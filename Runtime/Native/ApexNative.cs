using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PixoVR.Apex.Native
{
	/// <summary>
	/// P/Invoke declarations for the apex-sdk-core C interface (include/apex/Apex.h), one per
	/// function. Strings go in as NUL-terminated UTF-8 byte arrays (see <see cref="ToUtf8"/>);
	/// a null array passes NULL. Only <see cref="ApexCoreClient"/> calls these.
	/// </summary>
	internal static class ApexNative
	{
		/// <summary>apex_core.dll on Windows, libapex_core.so on Android.</summary>
		internal const string kLibraryName = "apex_core";

		/// <summary>The ABI this binding was written against: (major &lt;&lt; 16) | minor.</summary>
		internal const uint kExpectedAbiMajor = 0;
		internal const uint kExpectedAbiMinor = 7;

		// ---------------------------------------------------------------- Library

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint ApexAbiVersion();

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr ApexVersionString();

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr ApexBuiltinTransportName();

		// ---------------------------------------------------------------- Client

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexConfigInit(ref ApexConfigData config);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr ApexClientCreate(ref ApexConfigData config);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexClientDestroy(IntPtr client);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexClientPoll(IntPtr client, double nowSeconds);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexCancel(IntPtr client, ulong callId);

		// ---------------------------------------------------------------- Platform calls

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexPing(IntPtr client, ApexResultCallback onResult, IntPtr userData);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexLogin(IntPtr client, byte[] username, byte[] password,
			ApexResultCallback onResult, IntPtr userData);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexLoginWithToken(IntPtr client, byte[] userToken, ApexResultCallback onResult,
			IntPtr userData);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexQuickIdGetUsers(IntPtr client, byte[] serialNumber,
			ApexResultCallback onResult, IntPtr userData);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexQuickIdLogin(IntPtr client, byte[] serialNumber, byte[] username,
			ApexResultCallback onResult, IntPtr userData);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexGenerateLoginCode(IntPtr client, int userId, ApexResultCallback onResult,
			IntPtr userData);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexLogout(IntPtr client);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexGetUserModules(IntPtr client, int userId, ApexResultCallback onResult,
			IntPtr userData);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexCheckModuleAccess(IntPtr client, int moduleId, ApexResultCallback onResult,
			IntPtr userData);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexGetModuleList(IntPtr client, byte[] platformName, ApexResultCallback onResult,
			IntPtr userData);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexGetModulePage(IntPtr client, byte[] platformName, uint lifecycleMask,
			int sortOrder, int pageIndex, int pageSize, int refresh, ApexResultCallback onResult, IntPtr userData);

		// ---------------------------------------------------------------- Sessions

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexSessionJoin(IntPtr client, byte[] moduleData, UIntPtr moduleDataLength,
			ApexResultCallback onResult, IntPtr userData);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexSessionEvent(IntPtr client, byte[] moduleData, UIntPtr moduleDataLength,
			ApexResultCallback onResult, IntPtr userData);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong ApexSessionComplete(IntPtr client, byte[] moduleData, UIntPtr moduleDataLength,
			ApexResultCallback onResult, IntPtr userData);

		// ---------------------------------------------------------------- State

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int ApexIsLoggedIn(IntPtr client);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern UIntPtr ApexGetCurrentUser(IntPtr client, byte[] buffer, UIntPtr bufferSize);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern long ApexGetSessionId(IntPtr client);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexSetDeviceId(IntPtr client, byte[] deviceId);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexSetSessionFormat(IntPtr client, int sessionFormat);

		// ---------------------------------------------------------------- Launch arguments

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexLaunchParseArgs(IntPtr client, int argumentCount, IntPtr[] argumentValues);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexLaunchParseUrl(IntPtr client, byte[] launchUrl);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexLaunchSet(IntPtr client, byte[] key, byte[] value);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern UIntPtr ApexLaunchGet(IntPtr client, byte[] key, byte[] buffer, UIntPtr bufferSize);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern UIntPtr ApexLaunchExitTarget(IntPtr client, byte[] nextTarget, byte[] buffer,
			UIntPtr bufferSize);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int ApexModuleVersionIsValid(byte[] moduleVersion);

		// ---------------------------------------------------------------- Telemetry

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexTelemetryObjectRegistered(IntPtr client, byte[] trackedId, byte[] objectName,
			byte[] meshName, ref ApexPoseData pose);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexTelemetryObjectUnregistered(IntPtr client, byte[] trackedId,
			ref ApexPoseData pose);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexTelemetryEngagementBegin(IntPtr client, byte[] trackedId,
			byte[] engagementName);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexTelemetryEngagementEnd(IntPtr client, byte[] trackedId, byte[] engagementName);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexTelemetryInteraction(IntPtr client, byte[] trackedId, byte[] actionName);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexTelemetryStepBegin(IntPtr client, byte[] stepName);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexTelemetryStepEnd(IntPtr client, byte[] stepName, int succeeded, double score);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int ApexTelemetryPoseDue(IntPtr client);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexTelemetryPose(IntPtr client, byte[] trackedId, ref ApexPoseData pose);

		[DllImport(kLibraryName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void ApexTelemetryFlush(IntPtr client);

		// ---------------------------------------------------------------- UTF-8 helpers

		/// <summary>NUL-terminated UTF-8 bytes for a native string argument, or null for NULL.</summary>
		internal static byte[] ToUtf8(string managedText)
		{
			if (managedText == null)
			{
				return null;
			}

			int byteCount = Encoding.UTF8.GetByteCount(managedText);
			byte[] utf8Bytes = new byte[byteCount + 1];
			Encoding.UTF8.GetBytes(managedText, 0, managedText.Length, utf8Bytes, 0);
			return utf8Bytes;
		}

		/// <summary>Reads a NUL-terminated UTF-8 string the core owns. NULL reads as null.</summary>
		internal static string FromUtf8(IntPtr nativeText)
		{
			if (nativeText == IntPtr.Zero)
			{
				return null;
			}

			int byteCount = 0;
			while (Marshal.ReadByte(nativeText, byteCount) != 0)
			{
				byteCount++;
			}

			if (byteCount == 0)
			{
				return string.Empty;
			}

			byte[] utf8Bytes = new byte[byteCount];
			Marshal.Copy(nativeText, utf8Bytes, 0, byteCount);
			return Encoding.UTF8.GetString(utf8Bytes);
		}

		/// <summary>
		/// Calls a core function that fills a caller buffer and returns the full length, growing the
		/// buffer once when the first attempt was too small.
		/// </summary>
		internal static string ReadBufferedText(Func<byte[], UIntPtr, UIntPtr> readIntoBuffer)
		{
			byte[] textBuffer = new byte[1024];
			int requiredLength = (int)readIntoBuffer(textBuffer, (UIntPtr)textBuffer.Length).ToUInt64();
			if (requiredLength >= textBuffer.Length)
			{
				textBuffer = new byte[requiredLength + 1];
				requiredLength = (int)readIntoBuffer(textBuffer, (UIntPtr)textBuffer.Length).ToUInt64();
			}

			return Encoding.UTF8.GetString(textBuffer, 0, Math.Min(requiredLength, textBuffer.Length - 1));
		}
	}
}
