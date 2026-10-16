using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using PixoVR.Apex.Analytics;
using UnityEngine;

namespace PixoVR.Apex.Native
{
	/// <summary>Where the core sends telemetry packets.</summary>
	internal enum ETelemetryDestination
	{
		/// <summary>Records are dropped.</summary>
		OFF,
		/// <summary>The core POSTs each packet to the telemetry URL with the user's token.</summary>
		HTTP,
		/// <summary>Each packet is handed to the C# packet handler, which logs or saves it.</summary>
		CALLBACK,
	}

	/// <summary>Everything the core needs to create a client.</summary>
	internal sealed class ApexCoreSettings
	{
		public EPlatformServer PlatformServer { get; set; } = EPlatformServer.NA_PRODUCTION;
		/// <summary>Overrides the environment's Headset API host when set.</summary>
		public string HeadsetApiUrl { get; set; }
		/// <summary>Overrides the environment's Platform API host when set.</summary>
		public string PlatformApiUrl { get; set; }
		public int ModuleId { get; set; }
		public string ModuleVersion { get; set; }
		public string DeviceSerialNumber { get; set; }
		public string DeviceModel { get; set; }
		public string PlatformName { get; set; }
		public string ClientIp { get; set; }
		public double HeartbeatIntervalSeconds { get; set; } = 5.0;
		public ETelemetryDestination TelemetryDestination { get; set; } = ETelemetryDestination.OFF;
		public string TelemetryUrl { get; set; }
		public double TelemetryFlushIntervalSeconds { get; set; }
		public double TelemetryPoseSamplesPerSecond { get; set; }
		public int TelemetryMaximumRecordsPerPacket { get; set; }
		/// <summary>Receives each packet as JSON when the destination is CALLBACK.</summary>
		public Action<string> TelemetryPacketHandler { get; set; }
	}

	/// <summary>
	/// One client of the native core. Every call returns at once; its result arrives on a later
	/// <see cref="Poll"/>, on the thread that polls, as an <see cref="ApexResponse"/> plus the
	/// payload JSON (null on failure). Use it from the main thread only.
	/// </summary>
	internal sealed class ApexCoreClient : IDisposable, IApexTelemetryRecorder
	{
		private const string kLogTag = "ApexCore";

		// Native code keeps these delegates; static fields keep them alive for the whole process.
		private static readonly ApexResultCallback kResultCallback = OnNativeResult;
		private static readonly ApexLogCallback kLogCallback = OnNativeLog;
		private static readonly ApexTelemetryCallback kTelemetryCallback = OnNativeTelemetryPacket;
		private static bool m_libraryLoaded;

		private readonly Dictionary<ulong, Action<ApexResponse, string>> m_pendingCalls =
			new Dictionary<ulong, Action<ApexResponse, string>>();
		private readonly Action<string> m_telemetryPacketHandler;
		private IntPtr m_nativeClient;
		private GCHandle m_selfHandle;
		private bool m_polling;
		private bool m_releaseHandleAfterPoll;

		private ApexCoreClient(IntPtr nativeClient, GCHandle selfHandle, Action<string> telemetryPacketHandler)
		{
			m_nativeClient = nativeClient;
			m_selfHandle = selfHandle;
			m_telemetryPacketHandler = telemetryPacketHandler;
		}

		public bool IsValid => m_nativeClient != IntPtr.Zero;

		private IntPtr SelfPointer => GCHandle.ToIntPtr(m_selfHandle);

		// ---------------------------------------------------------------- Library

		/// <summary>The core library version, e.g. "0.1.0", or null when the library cannot be loaded.</summary>
		public static string GetLibraryVersion()
		{
			return TryLoadLibrary(out _) ? ApexNative.FromUtf8(ApexNative.ApexVersionString()) : null;
		}

		/// <summary>"winhttp", "android" or "none", or null when the library cannot be loaded.</summary>
		public static string GetBuiltinTransportName()
		{
			return TryLoadLibrary(out _) ? ApexNative.FromUtf8(ApexNative.ApexBuiltinTransportName()) : null;
		}

		/// <summary>
		/// Loads the core and checks its ABI. On Android the library is loaded through
		/// System.loadLibrary first, so its JNI_OnLoad receives the JavaVM that its HTTP backend needs.
		/// </summary>
		public static bool TryLoadLibrary(out string errorMessage)
		{
			errorMessage = null;
			if (m_libraryLoaded)
			{
				return true;
			}

			try
			{
#if UNITY_ANDROID && !UNITY_EDITOR
				using (AndroidJavaClass javaSystem = new AndroidJavaClass("java.lang.System"))
				{
					javaSystem.CallStatic("loadLibrary", ApexNative.kLibraryName);
				}

#endif
				uint abiVersion = ApexNative.ApexAbiVersion();
				uint abiMajor = abiVersion >> 16;
				uint abiMinor = abiVersion & 0xFFFF;
				// While the major version is 0, any minor version may break the ABI.
				bool compatible = abiMajor == ApexNative.kExpectedAbiMajor
					&& (abiMajor > 0 ? abiMinor >= ApexNative.kExpectedAbiMinor
						: abiMinor == ApexNative.kExpectedAbiMinor);
				if (!compatible)
				{
					errorMessage = "The apex_core library has ABI " + abiMajor + "." + abiMinor + "; this SDK needs "
						+ ApexNative.kExpectedAbiMajor + "." + ApexNative.kExpectedAbiMinor + ".";
					return false;
				}

				m_libraryLoaded = true;
				return true;
			}
			catch (DllNotFoundException exception)
			{
				errorMessage = "The apex_core native library is not available on this platform: " + exception.Message;
			}
			catch (EntryPointNotFoundException exception)
			{
				errorMessage = "The apex_core native library is out of date: " + exception.Message;
			}
			catch (Exception exception)
			{
				errorMessage = "The apex_core native library failed to load: " + exception.Message;
			}

			return false;
		}

		/// <summary>True when moduleVersion is "MAJOR.MINOR.PATCH" as the platform expects.</summary>
		public static bool IsModuleVersionValid(string moduleVersion)
		{
			if (string.IsNullOrEmpty(moduleVersion) || !TryLoadLibrary(out _))
			{
				return false;
			}

			return ApexNative.ApexModuleVersionIsValid(ApexNative.ToUtf8(moduleVersion)) != 0;
		}

		// ---------------------------------------------------------------- Lifetime

		/// <summary>Creates a client, or returns null and explains why in errorMessage.</summary>
		public static ApexCoreClient Create(ApexCoreSettings settings, out string errorMessage)
		{
			if (!TryLoadLibrary(out errorMessage))
			{
				return null;
			}

			List<IntPtr> nativeStrings = new List<IntPtr>();
			IntPtr AllocateText(string managedText)
			{
				if (string.IsNullOrEmpty(managedText))
				{
					return IntPtr.Zero;
				}

				byte[] utf8Bytes = ApexNative.ToUtf8(managedText);
				IntPtr nativeText = Marshal.AllocHGlobal(utf8Bytes.Length);
				Marshal.Copy(utf8Bytes, 0, nativeText, utf8Bytes.Length);
				nativeStrings.Add(nativeText);
				return nativeText;
			}

			GCHandle selfHandle = default;
			try
			{
				ApexConfigData config = default;
				ApexNative.ApexConfigInit(ref config);
				config.Environment = (int)ToNativeEnvironment(settings.PlatformServer);
				config.HeadsetApiUrl = AllocateText(settings.HeadsetApiUrl);
				config.PlatformApiUrl = AllocateText(settings.PlatformApiUrl);
				config.ModuleId = settings.ModuleId;
				config.ModuleVersion = AllocateText(settings.ModuleVersion);
				config.DeviceId = AllocateText(settings.DeviceSerialNumber);
				config.DeviceModel = AllocateText(settings.DeviceModel);
				config.Platform = AllocateText(settings.PlatformName);
				config.ClientIp = AllocateText(settings.ClientIp);
				if (settings.HeartbeatIntervalSeconds > 0.0)
				{
					config.HeartbeatIntervalSeconds = settings.HeartbeatIntervalSeconds;
				}

				config.LogCallback = Marshal.GetFunctionPointerForDelegate(kLogCallback);
				config.LogUserData = IntPtr.Zero;
				config.LogLevel = (int)EApexLogLevel.APEX_LOG_INFO;
				config.TelemetrySink = (int)ToNativeTelemetrySink(settings.TelemetryDestination);
				config.TelemetryUrl = AllocateText(settings.TelemetryUrl);
				config.TelemetryFlushIntervalSeconds = settings.TelemetryFlushIntervalSeconds;
				config.TelemetryPoseSamplesPerSecond = settings.TelemetryPoseSamplesPerSecond;
				config.TelemetryMaximumRecordsPerPacket = settings.TelemetryMaximumRecordsPerPacket;

				// The handle is created before the client so the telemetry callback can find it.
				ApexCoreClient coreClient = new ApexCoreClient(IntPtr.Zero, default, settings.TelemetryPacketHandler);
				selfHandle = GCHandle.Alloc(coreClient);
				coreClient.m_selfHandle = selfHandle;
				if (settings.TelemetryDestination == ETelemetryDestination.CALLBACK)
				{
					config.TelemetryCallback = Marshal.GetFunctionPointerForDelegate(kTelemetryCallback);
					config.TelemetryUserData = GCHandle.ToIntPtr(selfHandle);
				}

				// The core copies every string, so they are freed below.
				IntPtr nativeClient = ApexNative.ApexClientCreate(ref config);
				if (nativeClient == IntPtr.Zero)
				{
					errorMessage = "The core rejected the configuration; see the ApexCore log above.";
					selfHandle.Free();
					return null;
				}

				coreClient.m_nativeClient = nativeClient;
				return coreClient;
			}
			catch (Exception exception)
			{
				if (selfHandle.IsAllocated)
				{
					selfHandle.Free();
				}

				errorMessage = "Creating the core client failed: " + exception.Message;
				return null;
			}
			finally
			{
				foreach (IntPtr nativeText in nativeStrings)
				{
					Marshal.FreeHGlobal(nativeText);
				}
			}
		}

		/// <summary>Destroys the native client. Pending calls are dropped without a result.</summary>
		public void Dispose()
		{
			if (m_nativeClient == IntPtr.Zero)
			{
				return;
			}

			ApexNative.ApexClientDestroy(m_nativeClient);
			m_nativeClient = IntPtr.Zero;
			m_pendingCalls.Clear();
			// Destroyed from inside a result callback, the core finishes the poll first.
			if (m_polling)
			{
				m_releaseHandleAfterPoll = true;
			}
			else
			{
				ReleaseSelfHandle();
			}
		}

		private void ReleaseSelfHandle()
		{
			if (m_selfHandle.IsAllocated)
			{
				m_selfHandle.Free();
			}
		}

		/// <summary>Delivers finished results and drives the session heartbeat. Call once per frame.</summary>
		public void Poll(double nowSeconds)
		{
			if (m_nativeClient == IntPtr.Zero)
			{
				return;
			}

			m_polling = true;
			try
			{
				ApexNative.ApexClientPoll(m_nativeClient, nowSeconds);
			}
			finally
			{
				m_polling = false;
				if (m_releaseHandleAfterPoll)
				{
					m_releaseHandleAfterPoll = false;
					ReleaseSelfHandle();
				}
			}
		}

		/// <summary>Cancels a call; its callback runs on the next poll with APEX_ERR_CANCELLED.</summary>
		public void Cancel(ulong callId)
		{
			if (m_nativeClient != IntPtr.Zero)
			{
				ApexNative.ApexCancel(m_nativeClient, callId);
			}
		}

		// ---------------------------------------------------------------- Platform calls

		public ulong Ping(Action<ApexResponse, string> onResult)
		{
			return StartCall(() => ApexNative.ApexPing(m_nativeClient, kResultCallback, SelfPointer), onResult);
		}

		public ulong Login(string username, string password, Action<ApexResponse, string> onResult)
		{
			return StartCall(() => ApexNative.ApexLogin(m_nativeClient, ApexNative.ToUtf8(username),
				ApexNative.ToUtf8(password), kResultCallback, SelfPointer), onResult);
		}

		/// <summary>A null or empty token uses the "pixotoken" launch argument.</summary>
		public ulong LoginWithToken(string userToken, Action<ApexResponse, string> onResult)
		{
			return StartCall(() => ApexNative.ApexLoginWithToken(m_nativeClient,
				ApexNative.ToUtf8(userToken), kResultCallback, SelfPointer), onResult);
		}

		/// <summary>A null serial number uses the device serial number from the settings.</summary>
		public ulong QuickIdGetUsers(string serialNumber, Action<ApexResponse, string> onResult)
		{
			return StartCall(() => ApexNative.ApexQuickIdGetUsers(m_nativeClient,
				ApexNative.ToUtf8(serialNumber), kResultCallback, SelfPointer), onResult);
		}

		public ulong QuickIdLogin(string serialNumber, string username, Action<ApexResponse, string> onResult)
		{
			return StartCall(() => ApexNative.ApexQuickIdLogin(m_nativeClient,
				ApexNative.ToUtf8(serialNumber), ApexNative.ToUtf8(username), kResultCallback, SelfPointer), onResult);
		}

		/// <summary>A negative user ID generates the code for the current user.</summary>
		public ulong GenerateLoginCode(int userId, Action<ApexResponse, string> onResult)
		{
			return StartCall(() => ApexNative.ApexGenerateLoginCode(m_nativeClient, userId,
				kResultCallback, SelfPointer), onResult);
		}

		/// <summary>Forgets the current user and ends any session locally.</summary>
		public void Logout()
		{
			if (m_nativeClient != IntPtr.Zero)
			{
				ApexNative.ApexLogout(m_nativeClient);
			}
		}

		/// <summary>A negative user ID means the current user.</summary>
		public ulong GetUserModules(int userId, Action<ApexResponse, string> onResult)
		{
			return StartCall(() => ApexNative.ApexGetUserModules(m_nativeClient, userId,
				kResultCallback, SelfPointer), onResult);
		}

		/// <summary>A negative module ID means the module ID from the settings.</summary>
		public ulong CheckModuleAccess(int moduleId, Action<ApexResponse, string> onResult)
		{
			return StartCall(() => ApexNative.ApexCheckModuleAccess(m_nativeClient, moduleId,
				kResultCallback, SelfPointer), onResult);
		}

		public ulong GetModuleList(string platformName, Action<ApexResponse, string> onResult)
		{
			return StartCall(() => ApexNative.ApexGetModuleList(m_nativeClient,
				ApexNative.ToUtf8(platformName), kResultCallback, SelfPointer), onResult);
		}

		// ---------------------------------------------------------------- Sessions

		public ulong JoinSession(byte[] moduleData, Action<ApexResponse, string> onResult)
		{
			return StartCall(() => ApexNative.ApexSessionJoin(m_nativeClient, moduleData,
				ToLength(moduleData), kResultCallback, SelfPointer), onResult);
		}

		public ulong SendSessionEvent(byte[] moduleData, Action<ApexResponse, string> onResult)
		{
			return StartCall(() => ApexNative.ApexSessionEvent(m_nativeClient, moduleData,
				ToLength(moduleData), kResultCallback, SelfPointer), onResult);
		}

		public ulong CompleteSession(byte[] moduleData, Action<ApexResponse, string> onResult)
		{
			return StartCall(() => ApexNative.ApexSessionComplete(m_nativeClient, moduleData,
				ToLength(moduleData), kResultCallback, SelfPointer), onResult);
		}

		// ---------------------------------------------------------------- State

		public bool IsLoggedIn()
		{
			return m_nativeClient != IntPtr.Zero && ApexNative.ApexIsLoggedIn(m_nativeClient) != 0;
		}

		/// <summary>The current user as JSON, or an empty string when logged out.</summary>
		public string GetCurrentUserJson()
		{
			if (m_nativeClient == IntPtr.Zero)
			{
				return string.Empty;
			}

			return ApexNative.ReadBufferedText((textBuffer, bufferSize) => ApexNative.ApexGetCurrentUser(m_nativeClient,
				textBuffer, bufferSize));
		}

		/// <summary>The active session's ID, or 0 when no session is active.</summary>
		public long GetSessionId()
		{
			return m_nativeClient == IntPtr.Zero ? 0 : ApexNative.ApexGetSessionId(m_nativeClient);
		}

		/// <summary>Replaces the serial number Quick ID and module access use.</summary>
		public void SetDeviceSerialNumber(string serialNumber)
		{
			if (m_nativeClient != IntPtr.Zero)
			{
				ApexNative.ApexSetDeviceId(m_nativeClient, ApexNative.ToUtf8(serialNumber ?? string.Empty));
			}
		}

		// ---------------------------------------------------------------- Launch arguments

		/// <summary>Command-line arguments as the OS passes them; the first is the executable.</summary>
		public void ParseLaunchArguments(string[] commandLineArguments)
		{
			if (m_nativeClient == IntPtr.Zero || commandLineArguments == null)
			{
				return;
			}

			IntPtr[] nativeArguments = new IntPtr[commandLineArguments.Length];
			try
			{
				for (int argumentIndex = 0; argumentIndex < commandLineArguments.Length; argumentIndex++)
				{
					byte[] utf8Bytes = ApexNative.ToUtf8(commandLineArguments[argumentIndex] ?? string.Empty);
					nativeArguments[argumentIndex] = Marshal.AllocHGlobal(utf8Bytes.Length);
					Marshal.Copy(utf8Bytes, 0, nativeArguments[argumentIndex], utf8Bytes.Length);
				}

				ApexNative.ApexLaunchParseArgs(m_nativeClient, nativeArguments.Length, nativeArguments);
			}
			finally
			{
				foreach (IntPtr nativeArgument in nativeArguments)
				{
					if (nativeArgument != IntPtr.Zero)
					{
						Marshal.FreeHGlobal(nativeArgument);
					}
				}
			}
		}

		/// <summary>Launcher URL data, such as an Android intent's data string or a deep link.</summary>
		public void ParseLaunchUrl(string launchUrl)
		{
			if (m_nativeClient != IntPtr.Zero && !string.IsNullOrEmpty(launchUrl))
			{
				ApexNative.ApexLaunchParseUrl(m_nativeClient, ApexNative.ToUtf8(launchUrl));
			}
		}

		public void SetLaunchArgument(string argumentName, string argumentValue)
		{
			if (m_nativeClient != IntPtr.Zero && !string.IsNullOrEmpty(argumentName))
			{
				ApexNative.ApexLaunchSet(m_nativeClient, ApexNative.ToUtf8(argumentName),
					ApexNative.ToUtf8(argumentValue ?? string.Empty));
			}
		}

		/// <summary>The launch argument's value, or an empty string when it was not passed.</summary>
		public string GetLaunchArgument(string argumentName)
		{
			if (m_nativeClient == IntPtr.Zero || string.IsNullOrEmpty(argumentName))
			{
				return string.Empty;
			}

			byte[] nameBytes = ApexNative.ToUtf8(argumentName);
			return ApexNative.ReadBufferedText((textBuffer, bufferSize) => ApexNative.ApexLaunchGet(m_nativeClient,
				nameBytes, textBuffer, bufferSize));
		}

		/// <summary>How to hand control back to the launcher, as JSON (see Apex.h).</summary>
		public string GetExitTargetJson(string nextTarget)
		{
			if (m_nativeClient == IntPtr.Zero)
			{
				return "{\"action\":\"none\"}";
			}

			byte[] nextTargetBytes = ApexNative.ToUtf8(string.IsNullOrEmpty(nextTarget) ? null : nextTarget);
			return ApexNative.ReadBufferedText((textBuffer,
				bufferSize) => ApexNative.ApexLaunchExitTarget(m_nativeClient, nextTargetBytes, textBuffer,
					bufferSize));
		}

		// ---------------------------------------------------------------- Telemetry

		public void TelemetryObjectRegistered(string trackedId, string objectName, string meshName, Vector3 position,
			Vector3 rotation)
		{
			if (m_nativeClient != IntPtr.Zero)
			{
				ApexPoseData pose = ToPose(position, rotation);
				ApexNative.ApexTelemetryObjectRegistered(m_nativeClient, ApexNative.ToUtf8(trackedId),
					ApexNative.ToUtf8(objectName),
					ApexNative.ToUtf8(meshName), ref pose);
			}
		}

		public void TelemetryObjectUnregistered(string trackedId, Vector3 position, Vector3 rotation)
		{
			if (m_nativeClient != IntPtr.Zero)
			{
				ApexPoseData pose = ToPose(position, rotation);
				ApexNative.ApexTelemetryObjectUnregistered(m_nativeClient, ApexNative.ToUtf8(trackedId), ref pose);
			}
		}

		public void TelemetryEngagementBegin(string trackedId, string engagementName)
		{
			if (m_nativeClient != IntPtr.Zero)
			{
				ApexNative.ApexTelemetryEngagementBegin(m_nativeClient, ApexNative.ToUtf8(trackedId),
					ApexNative.ToUtf8(engagementName));
			}
		}

		public void TelemetryEngagementEnd(string trackedId, string engagementName)
		{
			if (m_nativeClient != IntPtr.Zero)
			{
				ApexNative.ApexTelemetryEngagementEnd(m_nativeClient, ApexNative.ToUtf8(trackedId),
					ApexNative.ToUtf8(engagementName));
			}
		}

		public void TelemetryInteraction(string trackedId, string actionName)
		{
			if (m_nativeClient != IntPtr.Zero)
			{
				ApexNative.ApexTelemetryInteraction(m_nativeClient, ApexNative.ToUtf8(trackedId),
					ApexNative.ToUtf8(actionName));
			}
		}

		public void TelemetryStepBegin(string stepName)
		{
			if (m_nativeClient != IntPtr.Zero)
			{
				ApexNative.ApexTelemetryStepBegin(m_nativeClient, ApexNative.ToUtf8(stepName));
			}
		}

		public void TelemetryStepEnd(string stepName, bool succeeded, double score)
		{
			if (m_nativeClient != IntPtr.Zero)
			{
				ApexNative.ApexTelemetryStepEnd(m_nativeClient, ApexNative.ToUtf8(stepName), succeeded ? 1 : 0, score);
			}
		}

		/// <summary>True when the head and tracked objects should report their poses this frame.</summary>
		public bool TelemetryPoseDue()
		{
			return m_nativeClient != IntPtr.Zero && ApexNative.ApexTelemetryPoseDue(m_nativeClient) != 0;
		}

		/// <summary>A null or empty tracked ID is the head. Unchanged poses are dropped by the core.</summary>
		public void TelemetryPose(string trackedId, Vector3 position, Vector3 rotation)
		{
			if (m_nativeClient != IntPtr.Zero)
			{
				ApexPoseData pose = ToPose(position, rotation);
				ApexNative.ApexTelemetryPose(m_nativeClient, ApexNative.ToUtf8(string.IsNullOrEmpty(trackedId) ? null
					: trackedId), ref pose);
			}
		}

		public void TelemetryFlush()
		{
			if (m_nativeClient != IntPtr.Zero)
			{
				ApexNative.ApexTelemetryFlush(m_nativeClient);
			}
		}

		// ---------------------------------------------------------------- Callbacks from the core

		/// <summary>Starts a native call and remembers its callback until the result arrives.</summary>
		private ulong StartCall(Func<ulong> nativeCall, Action<ApexResponse, string> onResult)
		{
			ulong callId = m_nativeClient == IntPtr.Zero ? 0 : nativeCall();
			if (callId == 0)
			{
				// Only a missing client returns no call ID.
				ApexResponse failure = ApexResponse.LocalFailure(EApexError.APEX_ERR_INVALID_STATE,
					"The Apex core client is not running.");
				InvokeSafely(onResult, failure, null);
				return 0;
			}

			m_pendingCalls[callId] = onResult;
			return callId;
		}

		[MonoPInvokeCallback(typeof(ApexResultCallback))]
		private static void OnNativeResult(IntPtr userData, ref ApexResultData result)
		{
			try
			{
				if (!(GCHandle.FromIntPtr(userData).Target is ApexCoreClient coreClient))
				{
					return;
				}

				if (!coreClient.m_pendingCalls.TryGetValue(result.CallId, out Action<ApexResponse, string> onResult))
				{
					return;
				}

				coreClient.m_pendingCalls.Remove(result.CallId);
				ApexResponse response = new ApexResponse((EApexError)result.Error, result.HttpStatus,
					ApexNative.FromUtf8(result.Message));
				string payloadJson = ApexNative.FromUtf8(result.Json);
				InvokeSafely(onResult, response, payloadJson);
			}
			catch (Exception exception)
			{
				// An exception must not unwind into native code.
				Debug.LogException(exception);
			}
		}

		private static void InvokeSafely(Action<ApexResponse, string> onResult, ApexResponse response,
			string payloadJson)
		{
			try
			{
				onResult?.Invoke(response, payloadJson);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		/// <summary>May run on a core worker thread; Unity's logger is thread-safe.</summary>
		[MonoPInvokeCallback(typeof(ApexLogCallback))]
		private static void OnNativeLog(IntPtr userData, int logLevel, IntPtr message)
		{
			try
			{
				string messageText = ApexNative.FromUtf8(message);
				switch ((EApexLogLevel)logLevel)
				{
					case EApexLogLevel.APEX_LOG_ERROR:
						Debug.unityLogger.Log(LogType.Error, kLogTag, messageText);
						break;
					case EApexLogLevel.APEX_LOG_WARNING:
						Debug.unityLogger.Log(LogType.Warning, kLogTag, messageText);
						break;
					default:
						Debug.unityLogger.Log(LogType.Log, kLogTag, messageText);
						break;
				}
			}
			catch (Exception)
			{
				// Logging must never throw into native code.
			}
		}

		[MonoPInvokeCallback(typeof(ApexTelemetryCallback))]
		private static void OnNativeTelemetryPacket(IntPtr userData, IntPtr packetJson)
		{
			try
			{
				if (GCHandle.FromIntPtr(userData).Target is ApexCoreClient coreClient)
				{
					coreClient.m_telemetryPacketHandler?.Invoke(ApexNative.FromUtf8(packetJson));
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		// ---------------------------------------------------------------- Conversions

		private static UIntPtr ToLength(byte[] moduleData)
		{
			return (UIntPtr)(moduleData == null ? 0 : moduleData.Length);
		}

		private static ApexPoseData ToPose(Vector3 position, Vector3 rotation)
		{
			ApexPoseData pose = new ApexPoseData();
			pose.PositionX = position.x;
			pose.PositionY = position.y;
			pose.PositionZ = position.z;
			pose.RotationX = rotation.x;
			pose.RotationY = rotation.y;
			pose.RotationZ = rotation.z;
			return pose;
		}

		internal static EApexEnvironment ToNativeEnvironment(EPlatformServer platformServer)
		{
			switch (platformServer)
			{
				case EPlatformServer.NA_STAGE:
					return EApexEnvironment.APEX_ENV_NA_STAGING;
				case EPlatformServer.NA_DEV:
					return EApexEnvironment.APEX_ENV_NA_DEVELOPMENT;
				case EPlatformServer.SA_PRODUCTION:
					return EApexEnvironment.APEX_ENV_SA_PRODUCTION;
				case EPlatformServer.LOCAL_DEV:
					return EApexEnvironment.APEX_ENV_LOCAL;
				default:
					return EApexEnvironment.APEX_ENV_NA_PRODUCTION;
			}
		}

		private static EApexTelemetrySink ToNativeTelemetrySink(ETelemetryDestination telemetryDestination)
		{
			switch (telemetryDestination)
			{
				case ETelemetryDestination.HTTP:
					return EApexTelemetrySink.APEX_TELEMETRY_HTTP;
				case ETelemetryDestination.CALLBACK:
					return EApexTelemetrySink.APEX_TELEMETRY_CALLBACK;
				default:
					return EApexTelemetrySink.APEX_TELEMETRY_OFF;
			}
		}
	}
}
