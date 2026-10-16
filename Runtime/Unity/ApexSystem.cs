using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PixoVR.Apex.Analytics;
using PixoVR.Apex.Analytics.PixoVR;
using PixoVR.Apex.Native;
using PixoVR.Apex.Utils;
using UnityEngine;
using UnityEngine.XR;
#if MANAGE_XR
using MXR.SDK;
#endif

namespace PixoVR.Apex
{
	/// <summary>
	/// The Apex platform from Unity: login, module access, sessions and launcher hand-off. Every
	/// call runs in the native apex-sdk-core library; results arrive on a later frame through the
	/// success or failure callback, on the main thread. Each callback gets an
	/// <see cref="ApexResponse"/> (error, HTTP status, message) and the parsed result.
	/// </summary>
	[DefaultExecutionOrder(-50)]
	public class ApexSystem : ApexSingleton<ApexSystem>
	{
		private const string kTag = "ApexSystem";
		private const string kTokenArgument = "pixotoken";
		private const string kOptionalArgument = "optional";
		private const string kReturnTargetArgument = "returntarget";
		private const string kTargetTypeArgument = "targettype";

		private static ApexSystem m_activeSystem;

		protected EPlatformServer m_platformTargetServer = EPlatformServer.NA_PRODUCTION;
		protected int m_moduleId;
		protected string m_moduleName = "Generic";
		protected string m_moduleVersion = string.Empty;
		protected string m_scenarioId = "Generic";

#if PIXOVR_DEBUG
		[SerializeField]
#endif
		protected bool m_runSetupOnAwake = true;

#if PIXOVR_MODULEACCESS_BYPASS
		protected bool m_loginCheckModuleAccess = false;
#else
		protected bool m_loginCheckModuleAccess = true;
#endif

#if PIXOVR_DEBUG
		[SerializeField]
#endif
		protected float m_heartbeatIntervalSeconds = 5.0f;

		protected string m_deviceId = string.Empty;
		protected string m_deviceModel = string.Empty;
		protected string m_platformName = string.Empty;
		protected string m_clientIp = string.Empty;
		protected string m_deviceSerialNumber = string.Empty;
		protected bool m_sessionInProgress;
		protected long m_sessionId;
		protected ActiveUserInformation m_currentUserInformation = new ActiveUserInformation();

		/// <summary>Launch arguments (pixotoken, optional, returntarget, targettype), kept across clients.</summary>
		protected readonly Dictionary<string, string> m_launchArguments = new Dictionary<string, string>();
		private bool m_hasParsedLaunchArguments;

		private ApexCoreClient m_coreClient;
		private string m_coreErrorMessage = string.Empty;
		private ApexWebsocket m_webSocket;

		private Action<string> m_pendingAuthorizationCodeReceived;
		private Action<ApexResponse, ActiveUserInformation> m_pendingWebSocketLoginSuccess;
		private Action<ApexResponse, FailureResponse> m_pendingWebSocketLoginFailure;

		// ---------------------------------------------------------------- Properties

		public static int ModuleID
		{
			get
			{
				return Instance.m_moduleId;
			}
			set
			{
				Instance.m_moduleId = value;
			}
		}

		public static string ModuleName
		{
			get
			{
				return Instance.m_moduleName;
			}
			set
			{
				Instance.m_moduleName = value;
			}
		}

		public static string ModuleVersion
		{
			get
			{
				return Instance.m_moduleVersion;
			}
			set
			{
				Instance.m_moduleVersion = value;
			}
		}

		public static string ScenarioID
		{
			get
			{
				return Instance.m_scenarioId;
			}
			set
			{
				Instance.m_scenarioId = value;
			}
		}

		public static UserAccessResponseContent CurrentUserModuleInformation =>
			Instance.m_currentUserInformation.ModuleUserInformation;

		public static LoginResponseContent CurrentUser => Instance.m_currentUserInformation.User;

		public static bool RunSetupOnAwake
		{
			get
			{
				return Instance.m_runSetupOnAwake;
			}
			set
			{
				Instance.m_runSetupOnAwake = value;
			}
		}

		public static bool LoginCheckModuleAccess => Instance.m_loginCheckModuleAccess;

		/// <summary>The device's serial number, used by Quick ID and module access.</summary>
		public static string DeviceSerialNumber
		{
			get
			{
				return Instance.m_deviceSerialNumber;
			}
			set
			{
				Instance.SetDeviceSerialNumber(value);
			}
		}

		public static ApexAnalyticsContext AnalyticsContext => Instance.BuildAnalyticsContext();

		/// <summary>The login token the launcher passed ("pixotoken"), or an empty string.</summary>
		public static string PassedLoginToken => Instance.GetLaunchArgument(kTokenArgument);

		/// <summary>The launcher's "optional" data, passed on when the module exits.</summary>
		public static string OptionalData
		{
			get
			{
				return Instance.GetLaunchArgument(kOptionalArgument);
			}
			set
			{
				Instance.SetLaunchArgument(kOptionalArgument, value);
			}
		}

		/// <summary>Where the module returns when it exits: an app package or a URL.</summary>
		public static string CurrentExitTarget
		{
			get
			{
				return Instance.GetLaunchArgument(kReturnTargetArgument);
			}
			set
			{
				string exitTarget = value ?? string.Empty;
				Instance.SetLaunchArgument(kReturnTargetArgument, exitTarget);
				Instance.SetLaunchArgument(kTargetTypeArgument, exitTarget.Contains("://") ? "url" : "app");
			}
		}

		public static string TargetType => Instance.GetLaunchArgument(kTargetTypeArgument);

		public static bool IsSessionInProgress => Instance.m_sessionInProgress;

		/// <summary>The platform's ID for the active session, or 0.</summary>
		public static long SessionId => Instance.m_sessionInProgress ? Instance.m_sessionId : 0;

		/// <summary>The Platform API host of the selected environment.</summary>
		public static string APIEndpoint =>
			((EAPIPlatformServer)(int)Instance.m_platformTargetServer).ToUrlString();

		/// <summary>The Headset API host of the selected environment.</summary>
		public static string HeadsetAPIEndpoint => Instance.m_platformTargetServer.ToUrlString();

		public static EPlatformServer PlatformTargetServer => Instance.m_platformTargetServer;

		public static bool UserHasAccess => Instance.m_currentUserInformation.ModuleUserInformation?.Access ?? false;

		/// <summary>True when the native core is loaded and its client is running.</summary>
		public static bool IsCoreAvailable => Instance.m_coreClient != null && Instance.m_coreClient.IsValid;

		/// <summary>Why the core is not running, or an empty string.</summary>
		public static string CoreErrorMessage => Instance.m_coreErrorMessage;

		/// <summary>The apex-sdk-core library version, e.g. "0.1.0", or null when it cannot be loaded.</summary>
		public static string CoreVersion => ApexCoreClient.GetLibraryVersion();

		// ---------------------------------------------------------------- Unity lifecycle

		private void Awake()
		{
			Debug.unityLogger.Log(LogType.Log, kTag, $"ApexSystem found on {gameObject.name}");
			if (!InitializeInstance(this))
			{
				Debug.unityLogger.Log(LogType.Log, kTag, "Instance already initialized.");
				return;
			}

			m_activeSystem = this;
			Editor.PixoVRProjectSettings projectSettings =
				Resources.Load<Editor.PixoVRProjectSettings>("PixoVRProjectSettings");
			if (projectSettings != null)
			{
				if (string.IsNullOrEmpty(m_moduleVersion))
				{
					m_moduleVersion = projectSettings.ModuleVersion;
				}

				m_moduleName = projectSettings.ModuleName;
				m_moduleId = projectSettings.ModuleID;
				m_platformTargetServer = projectSettings.PlatformTargetServer;
			}

			ReadDeviceInformation();
			ReadPlatformConfiguration();
#if UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
			SetupDeepLinking();
#endif
			if (m_runSetupOnAwake)
			{
				SetupAPI();
			}

			DontDestroyOnLoad(gameObject);
#if MANAGE_XR && !UNITY_EDITOR && !UNITY_WEBGL
			Debug.unityLogger.Log(LogType.Log, kTag, "Using ManageXR");
			InitializeManageXR();
#endif
		}

		private void Start()
		{
			if (!ApexCoreClient.IsModuleVersionValid(m_moduleVersion))
			{
				Debug.unityLogger.Log(LogType.Warning, kTag, $"{m_moduleVersion} is an invalid module version.");
			}
		}

		private void Update()
		{
			m_coreClient?.Poll(Time.realtimeSinceStartupAsDouble);
			m_webSocket?.Update();
		}

		private void OnApplicationQuit()
		{
			ApexAnalytics.Dispatch(provider => provider.Flush());
			if (m_sessionInProgress)
			{
				CompleteSession(null);
			}
		}

		private void OnApplicationPause(bool paused)
		{
			if (paused)
			{
				ApexAnalytics.Dispatch(provider => provider.Flush());
			}
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			if (m_activeSystem == this)
			{
				m_activeSystem = null;
				m_coreClient?.Dispose();
				m_coreClient = null;
			}
		}

		// ---------------------------------------------------------------- Setup

		/// <summary>(Re)creates the core client for the selected environment. Logs out any user.</summary>
		protected void SetupAPI()
		{
			m_coreClient?.Dispose();
			m_coreClient = null;
			m_currentUserInformation = new ActiveUserInformation();
			m_sessionInProgress = false;
			m_sessionId = 0;

			m_coreClient = ApexCoreClient.Create(BuildCoreSettings(), out string errorMessage);
			m_coreErrorMessage = errorMessage ?? string.Empty;
			if (m_coreClient == null)
			{
				Debug.unityLogger.Log(LogType.Error, kTag, "The Apex core is not running: " + m_coreErrorMessage);
			}

			if (!m_hasParsedLaunchArguments)
			{
				Dictionary<string, string> applicationArguments = PixoPlatformUtilities.ParseApplicationArguments();
				ApplyLaunchArguments(applicationArguments);
				m_hasParsedLaunchArguments = true;
			}

			foreach (KeyValuePair<string, string> launchArgument in m_launchArguments)
			{
				m_coreClient?.SetLaunchArgument(launchArgument.Key, launchArgument.Value);
			}

			Debug.unityLogger.Log(LogType.Log, kTag, string.IsNullOrEmpty(PassedLoginToken)
				? "No login token was passed."
				: "A login token was passed.");
			m_webSocket?.CloseSocket();
			m_webSocket = new ApexWebsocket();
			m_webSocket.OnConnectFailed.AddListener(reason =>
				Debug.unityLogger.Log(LogType.Error, kTag, "Websocket failed to connect: " + reason));
			m_webSocket.OnReceive.AddListener(OnWebSocketReceive);
		}

		private ApexCoreSettings BuildCoreSettings()
		{
			ApexCoreSettings coreSettings = new ApexCoreSettings();
			coreSettings.PlatformServer = m_platformTargetServer;
			coreSettings.ModuleId = m_moduleId;
			coreSettings.ModuleVersion = m_moduleVersion;
			coreSettings.DeviceSerialNumber = m_deviceSerialNumber;
			coreSettings.DeviceModel = m_deviceModel;
			coreSettings.PlatformName = m_platformName;
			coreSettings.ClientIp = m_clientIp;
			coreSettings.HeartbeatIntervalSeconds = m_heartbeatIntervalSeconds;

			ApexAnalyticsSettings analyticsSettings = ApexAnalyticsSettings.Load();
			if (ApexAnalyticsSettings.IsPixoVRProviderEnabled(analyticsSettings))
			{
				ETelemetrySinkType sinkType = analyticsSettings == null
					? ETelemetrySinkType.LOG
					: analyticsSettings.TelemetrySink;
				bool hasTelemetryUrl = analyticsSettings != null
					&& !string.IsNullOrEmpty(analyticsSettings.TelemetryHttpUrl);
				if (sinkType == ETelemetrySinkType.HTTP && !hasTelemetryUrl)
				{
					Debug.unityLogger.Log(LogType.Warning, kTag,
						"The HTTP telemetry sink needs a URL; logging packets instead.");
					sinkType = ETelemetrySinkType.LOG;
				}

				if (sinkType == ETelemetrySinkType.HTTP)
				{
					coreSettings.TelemetryDestination = ETelemetryDestination.HTTP;
					coreSettings.TelemetryUrl = analyticsSettings.TelemetryHttpUrl;
				}
				else
				{
					coreSettings.TelemetryDestination = ETelemetryDestination.CALLBACK;
					coreSettings.TelemetryPacketHandler = TelemetryPacketWriters.Create(sinkType);
				}

				if (analyticsSettings != null)
				{
					coreSettings.TelemetryFlushIntervalSeconds = analyticsSettings.FlushIntervalSeconds;
					coreSettings.TelemetryPoseSamplesPerSecond = analyticsSettings.PoseSamplesPerSecond;
					coreSettings.TelemetryMaximumRecordsPerPacket = analyticsSettings.MaximumRecordsPerPacket;
				}
			}

			return coreSettings;
		}

		private void ReadDeviceInformation()
		{
			m_deviceId = SystemInfo.deviceUniqueIdentifier;
			m_deviceModel = SystemInfo.deviceModel;
			m_platformName = XRSettings.loadedDeviceName.Length > 0
				? XRSettings.loadedDeviceName
				: Application.platform.ToString();
			m_clientIp = ApexUtils.GetLocalIP();
		}

		/// <summary>On Android, pixoconfig.cnf in shared storage may select the environment.</summary>
		private void ReadPlatformConfiguration()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			if (!PixoAndroidUtils.DoesFileExistInSharedLocation("pixoconfig.cnf"))
			{
				return;
			}

			string configurationText = PixoAndroidUtils.ReadFileFromSharedStorage("pixoconfig.cnf");
			if (string.IsNullOrEmpty(configurationText))
			{
				return;
			}

			ConfigurationTypes configuration = JsonConvert.DeserializeObject<ConfigurationTypes>(configurationText);
			if (configuration?.Platform == null)
			{
				Debug.unityLogger.Log(LogType.Log, kTag, "Failed to read pixoconfig.cnf.");
				return;
			}

			if (configuration.Platform.Contains("NA", StringComparison.OrdinalIgnoreCase))
			{
				if (configuration.Platform.Contains("Production", StringComparison.OrdinalIgnoreCase))
				{
					m_platformTargetServer = EPlatformServer.NA_PRODUCTION;
				}

				if (configuration.Platform.Contains("Dev", StringComparison.OrdinalIgnoreCase))
				{
					m_platformTargetServer = EPlatformServer.NA_DEV;
				}

				if (configuration.Platform.Contains("Stage", StringComparison.OrdinalIgnoreCase))
				{
					m_platformTargetServer = EPlatformServer.NA_STAGE;
				}
			}
			else if (configuration.Platform.Contains("SA", StringComparison.OrdinalIgnoreCase))
			{
				m_platformTargetServer = EPlatformServer.SA_PRODUCTION;
			}

			Debug.unityLogger.Log(LogType.Log, kTag, $"pixoconfig.cnf selects {m_platformTargetServer}.");
#endif
		}

		private void SetupDeepLinking()
		{
			Application.deepLinkActivated += OnDeepLinkActivated;
			if (!string.IsNullOrEmpty(Application.absoluteURL))
			{
				// A cold start from a deep link.
				OnDeepLinkActivated(Application.absoluteURL);
			}
		}

		private void OnDeepLinkActivated(string deepLinkUrl)
		{
			ApplyLaunchArguments(PixoPlatformUtilities.ParseURLArguments(deepLinkUrl));
		}

		private void ApplyLaunchArguments(Dictionary<string, string> launchArguments)
		{
			if (launchArguments == null)
			{
				return;
			}

			foreach (KeyValuePair<string, string> launchArgument in launchArguments)
			{
				bool isToken = launchArgument.Key == kTokenArgument;
				if (isToken && !string.IsNullOrEmpty(GetLaunchArgument(kTokenArgument)))
				{
					// A token already passed is kept.
					continue;
				}

				SetLaunchArgument(launchArgument.Key, launchArgument.Value);
			}
		}

		private string GetLaunchArgument(string argumentName)
		{
			return m_launchArguments.TryGetValue(argumentName, out string argumentValue) ? argumentValue : string.Empty;
		}

		private void SetLaunchArgument(string argumentName, string argumentValue)
		{
			m_launchArguments[argumentName] = argumentValue ?? string.Empty;
			m_coreClient?.SetLaunchArgument(argumentName, argumentValue ?? string.Empty);
		}

		private void SetDeviceSerialNumber(string serialNumber)
		{
			m_deviceSerialNumber = serialNumber ?? string.Empty;
			m_coreClient?.SetDeviceSerialNumber(m_deviceSerialNumber);
		}

#if MANAGE_XR
		private async void InitializeManageXR()
		{
			Debug.unityLogger.Log(LogType.Log, kTag, "Initializing the ManageXR SDK");
			await MXRManager.InitAsync();
			MXRManager.System.OnDeviceStatusChange += OnDeviceStatusChanged;
			SetDeviceSerialNumber(MXRManager.System.DeviceStatus.serial);
			Debug.unityLogger.Log(LogType.Log, kTag, $"Device serial set to {m_deviceSerialNumber}");
		}

		private void OnDeviceStatusChanged(DeviceStatus newDeviceStatus)
		{
			SetDeviceSerialNumber(newDeviceStatus.serial);
			Debug.unityLogger.Log(LogType.Log, kTag, $"Device serial number changed to {m_deviceSerialNumber}");
		}
#endif

		/// <summary>Switches environment. The new client starts logged out.</summary>
		public static void ChangePlatformServer(EPlatformServer newServer)
		{
			Instance.m_platformTargetServer = newServer;
			Instance.SetupAPI();
		}

		public void SetModuleId(int newModuleId)
		{
			m_moduleId = newModuleId;
		}

		// ---------------------------------------------------------------- Platform calls

		public static void Ping(Action<ApexResponse, object> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			Instance.RequireCore(failure)?.Ping((response, payloadJson) =>
				Deliver(response, success, failure, () => null));
		}

		/// <summary>Logs in with the token the launcher passed ("pixotoken").</summary>
		public static void LoginWithToken(Action<ApexResponse, ActiveUserInformation> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			LoginWithToken(PassedLoginToken, success, failure);
		}

		public static void LoginWithToken(string token, Action<ApexResponse, ActiveUserInformation> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			if (string.IsNullOrEmpty(token))
			{
				Fail(failure, EApexError.APEX_ERR_INVALID_ARGUMENT, "No token passed to login with.");
				return;
			}

			Instance.RequireCore(failure)?.LoginWithToken(token, (response, payloadJson) =>
				Instance.OnLoginResult(response, payloadJson, success, failure));
		}

		public static void Login(LoginData login, Action<ApexResponse, ActiveUserInformation> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			if (login == null || string.IsNullOrEmpty(login.Login))
			{
				Fail(failure, EApexError.APEX_ERR_INVALID_ARGUMENT, "No username or email entered.");
				return;
			}

			string password = string.IsNullOrEmpty(login.Password) ? "<empty>" : login.Password;
			Instance.RequireCore(failure)?.Login(login.Login, password, (response, payloadJson) =>
				Instance.OnLoginResult(response, payloadJson, success, failure));
		}

		public static void Login(string username, string password,
			Action<ApexResponse, ActiveUserInformation> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			Login(new LoginData(username, password), success, failure);
		}

		/// <summary>Logs in a user enrolled for Quick ID on this device.</summary>
		public static bool QuickIDLogin(string serialNumber, string username,
			Action<ApexResponse, ActiveUserInformation> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			if (string.IsNullOrEmpty(serialNumber) || string.IsNullOrEmpty(username))
			{
				return false;
			}

			Instance.RequireCore(failure)?.QuickIdLogin(serialNumber, username, (response, payloadJson) =>
				Instance.OnLoginResult(response, payloadJson, success, failure));
			return true;
		}

		/// <summary>The users enrolled for Quick ID on a device.</summary>
		public static bool GetQuickIDAuthUsers(string serialNumber,
			Action<ApexResponse, QuickIDAuthGetUsersResponse> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			if (string.IsNullOrEmpty(serialNumber))
			{
				Fail(failure, EApexError.APEX_ERR_INVALID_ARGUMENT, "No serial number provided.");
				return false;
			}

			Instance.RequireCore(failure)?.QuickIdGetUsers(serialNumber, (response, payloadJson) =>
				Deliver(response, success, failure,
					() => QuickIDAuthGetUsersResponse.FromJson(JsonReading.ParseObject(payloadJson))));
			return true;
		}

		/// <summary>Forgets the current user and ends any session locally.</summary>
		public static void Logout()
		{
			Instance.m_coreClient?.Logout();
			Instance.m_currentUserInformation = new ActiveUserInformation();
			Instance.m_sessionInProgress = false;
			Instance.m_sessionId = 0;
		}

		/// <summary>
		/// Gets a login code from the platform and waits for a user to sign in with it on another
		/// device. codeReceived gets the code to show; success runs once the user has signed in.
		/// </summary>
		public static bool RequestAuthorizationCode(Action<string> codeReceived = null,
			Action<ApexResponse, ActiveUserInformation> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			return Instance.StartAuthorizationCodeLogin(codeReceived, success, failure);
		}

		/// <summary>Checks the current user's access to a module (this module when moduleId &lt; 0).</summary>
		public static bool CheckModuleAccess(int targetModuleId = -1,
			Action<ApexResponse, ActiveUserInformation> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			if (CurrentUser == null)
			{
				Debug.unityLogger.Log(LogType.Error, kTag, "Cannot check module access with no active login.");
				Fail(failure, EApexError.APEX_ERR_NOT_LOGGED_IN, "Cannot check module access with no active login.");
				return false;
			}

			int checkedModuleId = targetModuleId < 0 ? Instance.m_moduleId : targetModuleId;
			Instance.RequireCore(failure)?.CheckModuleAccess(checkedModuleId, (response, payloadJson) =>
			{
				if (!response.Succeeded)
				{
					Fail(failure, response);
					return;
				}

				Instance.ApplyModuleAccess(UserAccessResponseContent.FromJson(JsonReading.ParseObject(payloadJson)));
				InvokeSafely(() => success?.Invoke(response, Instance.m_currentUserInformation));
			});
			return true;
		}

		public static bool GetCurrentUserModules(Action<ApexResponse, GetUserModulesResponse> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			return GetUserModules(-1, success, failure);
		}

		/// <summary>The modules a user can access (the current user when userId &lt; 0).</summary>
		public static bool GetUserModules(int userId = -1, Action<ApexResponse, GetUserModulesResponse> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			if (CurrentUser == null)
			{
				Fail(failure, EApexError.APEX_ERR_NOT_LOGGED_IN, "No user logged in.");
				return false;
			}

			Instance.RequireCore(failure)?.GetUserModules(userId, (response, payloadJson) =>
				Deliver(response, success, failure,
					() => GetUserModulesResponse.FromJson(JsonReading.ParseObject(payloadJson))));
			return true;
		}

		/// <summary>
		/// The modules available to the current user, keeping only versions built for platformName.
		/// success gets a <see cref="UserModulesResponse"/>; ToOrgModules converts its Modules.
		/// </summary>
		public static bool GetModulesList(string platformName, Action<ApexResponse, object> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			if (CurrentUser == null)
			{
				Fail(failure, EApexError.APEX_ERR_NOT_LOGGED_IN, "No user logged in.");
				return false;
			}

			Instance.RequireCore(failure)?.GetModuleList(platformName, (response, payloadJson) =>
				Deliver(response, success, failure, () => UserModulesResponse.FromJson(payloadJson)));
			return true;
		}

		/// <summary>A one-time login code for the current user. success gets a GeneratedAssistedLogin.</summary>
		public static bool GenerateOneTimeLoginForCurrentUser(Action<ApexResponse, object> success,
			Action<ApexResponse, FailureResponse> failure)
		{
			if (CurrentUser == null)
			{
				Fail(failure, EApexError.APEX_ERR_NOT_LOGGED_IN, "No user logged in to generate code.");
				return false;
			}

			GenerateLoginCode(-1, success, failure);
			return true;
		}

		/// <summary>A one-time login code for another user. success gets a GeneratedAssistedLogin.</summary>
		public static void GenerateOneTimeLoginForUser(int userId, Action<ApexResponse, object> success,
			Action<ApexResponse, FailureResponse> failure)
		{
			if (CurrentUser == null)
			{
				Fail(failure, EApexError.APEX_ERR_NOT_LOGGED_IN, "No current user logged in.");
				return;
			}

			if (userId < 0)
			{
				Fail(failure, EApexError.APEX_ERR_INVALID_ARGUMENT, "User id is invalid.");
				return;
			}

			GenerateLoginCode(userId, success, failure);
		}

		private static void GenerateLoginCode(int userId, Action<ApexResponse, object> success,
			Action<ApexResponse, FailureResponse> failure)
		{
			Instance.RequireCore(failure)?.GenerateLoginCode(userId, (response, payloadJson) =>
				Deliver(response, success, failure,
					() => GeneratedAssistedLogin.FromJson(JsonReading.ParseObject(payloadJson))));
		}

		// ---------------------------------------------------------------- Sessions

		/// <summary>
		/// Starts a session for the current user. The scenario ID, module and device details and
		/// sessionData travel to the platform as the session's join data (JSON); see the README.
		/// </summary>
		public static void JoinSession(string scenarioId = null, JObject sessionData = null,
			Action<ApexResponse, JoinSessionResponse> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			if (!string.IsNullOrEmpty(scenarioId))
			{
				Instance.m_scenarioId = scenarioId;
			}

			JObject joinData = Instance.BuildJoinData(sessionData);
			JoinSessionWithRawData(Encoding.UTF8.GetBytes(joinData.ToString(Formatting.None)), success, failure);
		}

		/// <summary>Starts a session, sending moduleData as the join data exactly as given.</summary>
		public static void JoinSessionWithRawData(byte[] moduleData,
			Action<ApexResponse, JoinSessionResponse> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			if (!Instance.CanSendSessionCalls(failure, false))
			{
				return;
			}

			if (Instance.m_sessionInProgress)
			{
				Debug.unityLogger.Log(LogType.Error, kTag,
					"Session is already in progress. The previous session didn't complete or a new session was started"
						+ " during an active session.");
			}

			Instance.RequireCore(failure)?.JoinSession(moduleData, (response, payloadJson) =>
			{
				if (!response.Succeeded)
				{
					Instance.m_sessionInProgress = false;
					Fail(failure, response);
					return;
				}

				JoinSessionResponse joinResponse = new JoinSessionResponse();
				JObject joinPayload = JsonReading.ParseObject(payloadJson);
				joinResponse.SessionId = (long)(JsonReading.GetNumber(joinPayload, "sessionId") ?? 0);
				Instance.m_sessionId = joinResponse.SessionId;
				Instance.m_sessionInProgress = true;
				Debug.unityLogger.Log(LogType.Log, kTag, $"Session Id is {joinResponse.SessionId}.");
				ApexAnalyticsContext analyticsContext = Instance.BuildAnalyticsContext();
				ApexAnalytics.Dispatch(provider => provider.OnSessionJoined(analyticsContext, joinResponse));
				InvokeSafely(() => success?.Invoke(response, joinResponse));
			});
		}

		/// <summary>Sends an event during the session: an action, optionally on a target object, with data.</summary>
		public static void SendSessionEvent(string action, string targetObject = null, JObject eventData = null,
			Action<ApexResponse, object> success = null, Action<ApexResponse, FailureResponse> failure = null)
		{
			if (string.IsNullOrEmpty(action))
			{
				Fail(failure, EApexError.APEX_ERR_INVALID_ARGUMENT, "The event needs an action.");
				return;
			}

			JObject eventBody = new JObject();
			eventBody["action"] = action;
			eventBody["target"] = targetObject;
			eventBody["scenarioId"] = Instance.m_scenarioId;
			eventBody["data"] = eventData;
			ApexAnalyticsContext analyticsContext = Instance.BuildAnalyticsContext();
			JObject analyticsData = eventData == null ? null : (JObject)eventData.DeepClone();
			if (Instance.CanSendSessionCalls(null, true))
			{
				ApexAnalytics.Dispatch(provider =>
					provider.OnSessionEvent(analyticsContext, action, targetObject, analyticsData));
			}

			SendSessionEventWithRawData(Encoding.UTF8.GetBytes(eventBody.ToString(Formatting.None)), success, failure);
		}

		/// <summary>Sends moduleData as a session event's data exactly as given.</summary>
		public static void SendSessionEventWithRawData(byte[] moduleData, Action<ApexResponse, object> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			if (!Instance.CanSendSessionCalls(failure, true))
			{
				return;
			}

			Instance.RequireCore(failure)?.SendSessionEvent(moduleData, (response, payloadJson) =>
				Deliver(response, success, failure, () => null));
		}

		/// <summary>
		/// Completes the session with its result. With no sessionData the session is reported
		/// complete and unsuccessful with no score. resultData travels with the result.
		/// </summary>
		public static void CompleteSession(SessionData sessionData, JObject resultData = null,
			Action<ApexResponse, object> success = null, Action<ApexResponse, FailureResponse> failure = null)
		{
			SessionData completedSession = sessionData ?? new SessionData(0f, 0f, 0f, 0f, 0, true, false);
			JObject completeBody = new JObject();
			completeBody["scenarioId"] = Instance.m_scenarioId;
			completeBody["score"] = completedSession.Score;
			completeBody["scaledScore"] = DetermineScaledScore(completedSession);
			completeBody["minimumScore"] = completedSession.MinimumScore;
			completeBody["maximumScore"] = completedSession.MaximumScore;
			completeBody["durationSeconds"] = completedSession.Duration;
			completeBody["complete"] = completedSession.Complete;
			completeBody["success"] = completedSession.Success;
			completeBody["data"] = resultData;

			if (Instance.CanSendSessionCalls(null, true))
			{
				ApexAnalyticsContext analyticsContext = Instance.BuildAnalyticsContext();
				SessionData analyticsSession = CloneJson(completedSession);
				ApexAnalytics.Dispatch(provider => provider.OnSessionCompleted(analyticsContext, analyticsSession));
			}

			byte[] completeData = Encoding.UTF8.GetBytes(completeBody.ToString(Formatting.None));
			CompleteSessionWithRawData(completeData, success, failure);
		}

		/// <summary>Completes the session, sending moduleData as its data exactly as given.</summary>
		public static void CompleteSessionWithRawData(byte[] moduleData, Action<ApexResponse, object> success = null,
			Action<ApexResponse, FailureResponse> failure = null)
		{
			if (!Instance.CanSendSessionCalls(failure, true))
			{
				return;
			}

			Instance.RequireCore(failure)?.CompleteSession(moduleData, (response, payloadJson) =>
			{
				if (!response.Succeeded)
				{
					// The session stays active so the module can retry.
					Fail(failure, response);
					return;
				}

				Instance.m_sessionInProgress = false;
				Instance.m_sessionId = 0;
				if (!ApplicationIsQuitting)
				{
					InvokeSafely(() => success?.Invoke(response, null));
				}
			});
		}

		private bool CanSendSessionCalls(Action<ApexResponse, FailureResponse> failure, bool needsActiveSession)
		{
			if (CurrentUser == null)
			{
				Fail(failure, EApexError.APEX_ERR_NOT_LOGGED_IN, "Session calls need an active login.");
				return false;
			}

#if PIXOVR_MODULEACCESS_BYPASS
			Debug.unityLogger.Log(LogType.Warning, kTag, "Bypassing the user access check.");
#else
			if (!UserHasAccess)
			{
				Fail(failure, EApexError.APEX_ERR_INVALID_STATE, "User access not verified.");
				return false;
			}

#endif

			if (needsActiveSession && !m_sessionInProgress)
			{
				Fail(failure, EApexError.APEX_ERR_INVALID_STATE, "No session in progress.");
				return false;
			}

			return true;
		}

		private JObject BuildJoinData(JObject sessionData)
		{
			JObject joinData = new JObject();
			joinData["scenarioId"] = m_scenarioId;
			joinData["moduleName"] = m_moduleName;
			joinData["moduleVersion"] = m_moduleVersion;
			joinData["platform"] = m_platformName;
			joinData["deviceId"] = m_deviceId;
			joinData["deviceModel"] = m_deviceModel;
			joinData["deviceSerial"] = string.IsNullOrEmpty(m_deviceSerialNumber) ? null : m_deviceSerialNumber;
			joinData["sdkVersion"] = "unity-" + ApexUtils.SDKVersion;
			joinData["sdkSourceHash"] = ApexUtils.SDKSourceHash;
			joinData["coreVersion"] = ApexCoreClient.GetLibraryVersion();
#if PIXOVR_MODULEACCESS_BYPASS
			joinData["moduleAccessChecked"] = false;
#else
			joinData["moduleAccessChecked"] = true;
#endif
			joinData["data"] = sessionData;
			return joinData;
		}

		private static float DetermineScaledScore(SessionData sessionData)
		{
			bool computeScaledScore = sessionData.ScaledScore < Mathf.Epsilon && sessionData.Score >= Mathf.Epsilon
				&& sessionData.MaximumScore > Mathf.Epsilon;
			return computeScaledScore ? sessionData.Score / sessionData.MaximumScore * 100f : sessionData.ScaledScore;
		}

		// ---------------------------------------------------------------- Exiting to the launcher

		/// <summary>Returns to the launcher (or the app it named), passing the login on, then quits.</summary>
		public static void ExitApplication(string nextExitTarget = "")
		{
			Instance.ExitToLauncher(nextExitTarget);
		}

		private void ExitToLauncher(string nextExitTarget)
		{
			string exitTargetJson = m_coreClient?.GetExitTargetJson(nextExitTarget) ?? "{\"action\":\"none\"}";
			JObject exitTarget = JsonReading.ParseObject(exitTargetJson);
			string exitAction = JsonReading.GetString(exitTarget, "action");
			Debug.unityLogger.Log(LogType.Log, kTag, "Exit target: " + exitTargetJson);
			if (exitAction == "open_url")
			{
				PixoPlatformUtilities.OpenURL(JsonReading.GetString(exitTarget, "url"));
			}
			else if (exitAction == "open_app")
			{
				List<string> extraNames = new List<string>();
				List<string> extraValues = new List<string>();
				JObject extras = JsonReading.GetObject(exitTarget, "extras") ?? new JObject();
				foreach (JProperty extra in extras.Properties())
				{
					extraNames.Add(extra.Name);
					extraValues.Add(extra.Value.ToString());
				}

				string packageName = JsonReading.GetString(exitTarget, "package");
				PixoPlatformUtilities.OpenApplication(packageName, extraNames.ToArray(), extraValues.ToArray());
			}

			PixoPlatformUtilities.CloseCurrentApplication();
		}

		// ---------------------------------------------------------------- Login results

		private void OnLoginResult(ApexResponse response, string payloadJson,
			Action<ApexResponse, ActiveUserInformation> success, Action<ApexResponse, FailureResponse> failure)
		{
			if (!response.Succeeded)
			{
				Debug.unityLogger.Log(LogType.Log, kTag, "Failed to log in: " + response.Message);
				m_currentUserInformation = new ActiveUserInformation();
				Fail(failure, response);
				return;
			}

			m_currentUserInformation = new ActiveUserInformation();
			m_currentUserInformation.User = LoginResponseContent.FromJson(JsonReading.ParseObject(payloadJson));
			ApexAnalyticsContext analyticsContext = BuildAnalyticsContext();
			LoginResponseContent analyticsUser = CloneJson(m_currentUserInformation.User);
			ApexAnalytics.Dispatch(provider => provider.OnUserIdentified(analyticsContext, analyticsUser));

			if (m_loginCheckModuleAccess)
			{
				// success runs once, after the access check.
				CheckModuleAccess(m_moduleId, success, failure);
				return;
			}

			InvokeSafely(() => success?.Invoke(response, m_currentUserInformation));
		}

		private void ApplyModuleAccess(UserAccessResponseContent moduleAccess)
		{
			m_currentUserInformation.ModuleUserInformation = moduleAccess;
			if (moduleAccess.PassingScore.HasValue && m_currentUserInformation.User != null)
			{
				m_currentUserInformation.User.MinimumPassingScore = moduleAccess.PassingScore.Value;
			}
		}

		// ---------------------------------------------------------------- Login by authorization code

		private bool StartAuthorizationCodeLogin(Action<string> codeReceived,
			Action<ApexResponse, ActiveUserInformation> success, Action<ApexResponse, FailureResponse> failure)
		{
			if (m_webSocket == null)
			{
				Fail(failure, EApexError.APEX_ERR_INVALID_STATE, "The Apex system is not set up.");
				return false;
			}

			m_pendingAuthorizationCodeReceived = codeReceived;
			m_pendingWebSocketLoginSuccess = success;
			m_pendingWebSocketLoginFailure = failure;
			if (!m_webSocket.IsConnected())
			{
				string webSocketUrl = BuildWebSocketUrl(HeadsetAPIEndpoint);
				System.Threading.Tasks.Task.Run(() => m_webSocket.Connect(new Uri(webSocketUrl)));
			}

			return m_webSocket.RequestAuthorizationCode();
		}

		private static string BuildWebSocketUrl(string headsetApiUrl)
		{
			string hostAndPath = headsetApiUrl;
			int schemeEnd = hostAndPath.IndexOf("://", StringComparison.Ordinal);
			if (schemeEnd >= 0)
			{
				hostAndPath = hostAndPath.Substring(schemeEnd + 3);
			}

			int pathStart = hostAndPath.IndexOf('/');
			string host = pathStart >= 0 ? hostAndPath.Substring(0, pathStart) : hostAndPath;
			return "wss://" + host + "/ws";
		}

		private void OnWebSocketReceive(string receivedText)
		{
			try
			{
				JObject message = JsonReading.ParseObject(receivedText);
				string authorizationCode = JsonReading.GetString(message, "auth_code");
				if (authorizationCode != null)
				{
					m_pendingAuthorizationCodeReceived?.Invoke(authorizationCode);
				}

				string userToken = JsonReading.GetString(message, "Token") ?? JsonReading.GetString(message, "token");
				if (!string.IsNullOrEmpty(userToken))
				{
					// The socket hands over a token; log in with it so the core knows the user.
					Action<ApexResponse, ActiveUserInformation> loginSuccess = m_pendingWebSocketLoginSuccess;
					Action<ApexResponse, FailureResponse> loginFailure = m_pendingWebSocketLoginFailure;
					m_pendingAuthorizationCodeReceived = null;
					m_pendingWebSocketLoginSuccess = null;
					m_pendingWebSocketLoginFailure = null;
					LoginWithToken(userToken, loginSuccess, loginFailure);
				}
			}
			catch (Exception exception)
			{
				Debug.unityLogger.Log(LogType.Log, kTag, exception.Message);
			}
		}

		// ---------------------------------------------------------------- Helpers

		/// <summary>The running core client, or null after reporting why to failure.</summary>
		private ApexCoreClient RequireCore(Action<ApexResponse, FailureResponse> failure)
		{
			if (m_coreClient != null && m_coreClient.IsValid)
			{
				return m_coreClient;
			}

			string reason = string.IsNullOrEmpty(m_coreErrorMessage)
				? "The Apex core is not set up."
				: m_coreErrorMessage;
			Fail(failure, EApexError.APEX_ERR_INVALID_STATE, reason);
			return null;
		}

		/// <summary>The running core as a telemetry recorder, without creating an ApexSystem.</summary>
		internal static IApexTelemetryRecorder FindTelemetryRecorder()
		{
			ApexCoreClient coreClient = m_activeSystem != null ? m_activeSystem.m_coreClient : null;
			return coreClient != null && coreClient.IsValid ? coreClient : null;
		}

		protected ApexAnalyticsContext BuildAnalyticsContext()
		{
			LoginResponseContent user = m_currentUserInformation?.User;
			ApexAnalyticsContext analyticsContext = new ApexAnalyticsContext();
			analyticsContext.UserId = user?.ID ?? 0;
			analyticsContext.OrgId = user?.OrgId ?? 0;
			analyticsContext.UserEmail = user?.Email;
			analyticsContext.ModuleId = m_moduleId;
			analyticsContext.ModuleName = m_moduleName;
			analyticsContext.ModuleVersion = m_moduleVersion;
			analyticsContext.ScenarioId = m_scenarioId;
			analyticsContext.SessionId = m_sessionInProgress ? m_sessionId : 0;
			analyticsContext.DeviceId = m_deviceId;
			analyticsContext.DeviceModel = m_deviceModel;
			analyticsContext.DeviceSerial = m_deviceSerialNumber;
			analyticsContext.Platform = m_platformName;
			analyticsContext.SdkVersion = ApexUtils.SDKVersion;
			return analyticsContext;
		}

		private static void Deliver<TResult>(ApexResponse response, Action<ApexResponse, TResult> success,
			Action<ApexResponse, FailureResponse> failure, Func<TResult> parseResult)
		{
			if (!response.Succeeded)
			{
				Fail(failure, response);
				return;
			}

			InvokeSafely(() => success?.Invoke(response, parseResult()));
		}

		private static void Fail(Action<ApexResponse, FailureResponse> failure, EApexError error, string message)
		{
			Fail(failure, ApexResponse.LocalFailure(error, message));
		}

		private static void Fail(Action<ApexResponse, FailureResponse> failure, ApexResponse response)
		{
			InvokeSafely(() => failure?.Invoke(response, response.ToFailureResponse()));
		}

		private static void InvokeSafely(Action callback)
		{
			try
			{
				callback();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private static TValue CloneJson<TValue>(TValue originalValue) where TValue : class
		{
			if (originalValue == null)
			{
				return null;
			}

			return JsonConvert.DeserializeObject<TValue>(JsonConvert.SerializeObject(originalValue));
		}
	}
}
