using Newtonsoft.Json;
using PixoVR.Apex.Events;
using PixoVR.Apex.Utils;
using PixoVR.Apex.XAPI;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TinCan;
using UnityEngine;
using UnityEngine.XR;

#if MANAGE_XR
using MXR.SDK;
#endif

namespace PixoVR.Apex
{
    public delegate void PlatformResponse(ResponseType type, bool wasSuccessful, object responseData);

    [DefaultExecutionOrder(-50)]
    public class ApexSystem : ApexSingleton<ApexSystem>
    {
        private static readonly string TAG = "ApexSystem";
        private enum VersionParts : int
        {
            Major = 0,
            Minor,
            Patch,
        }

        public static string ServerIP
        {
            get { return Instance.serverIP; }
            set { }
        }

        public static int ModuleID
        {
            get { return Instance.moduleID; }
            set { Instance.moduleID = value; }
        }

        public static string ModuleName
        {
            get { return Instance.moduleName; }
            set { Instance.moduleName = value; }
        }

        public static string ModuleVersion
        {
            get { return Instance.moduleVersion; }
            set { Instance.moduleVersion = value; }
        }

        public static string ScenarioID
        {
            get { return Instance.scenarioID; }
            set { Instance.scenarioID = value; }
        }

        public static LoginResponseContent CurrentActiveLogin
        {
            get { return Instance.currentActiveLogin; }
            set { }
        }

        public static bool RunSetupOnAwake
        {
            get { return Instance.runSetupOnAwake; }
            set { Instance.runSetupOnAwake = value; }
        }

        public static bool LoginCheckModuleAccess
        {
            get { return Instance.loginCheckModuleAccess; }
        }

        public static string DeviceSerialNumber
        {
            get { return Instance.deviceSerialNumber; }
        }

        public static string PassedLoginToken
        {
            get
            {
                Debug.unityLogger.Log(LogType.Log, TAG, $"Getting passed login token as {Instance.loginToken} in instance {Instance.gameObject.name}");
                return Instance.loginToken;
            }

            protected set
            {
                Debug.unityLogger.Log(LogType.Error, TAG, $"Setting passed login token to {value}");
                Instance.loginToken = value;
            }
        }

        public static string OptionalData
        {
            get { return Instance.optionalParameter; }
            set { Instance.optionalParameter = value; }
        }

        public static string CurrentExitTarget
        {
            get { return Instance.currentExitTargetParameter; }
            set
            {
                if (value != null)
                {
                    Instance.currentExitTargetParameter = value;
                }
                else
                {
                    Instance.currentExitTargetParameter = "";
                }

                if (Instance.currentExitTargetParameter.Contains("://"))
                {
                    TargetType = "url";
                }
                else
                {
                    TargetType = "app";
                }
            }
        }

        public static bool IsSessionInProgress
        {
            get { return Instance.sessionInProgress; }
        }

        public static string TargetType
        {
            get { return Instance.targetTypeParameter; }
            private set { Instance.targetTypeParameter = value; }
        }

        public static string APIEndpoint
        {
            get { return ((APIPlatformServer)Instance.PlatformTargetServer).ToUrlString(); }
        }

        public static APIHandler ApexAPIHandler
        {
            get { return Instance.apexAPIHandler; }
        }

        [SerializeField, EndpointDisplay]
        protected PlatformServer PlatformTargetServer;

#if PIXOVR_DEBUG
        [SerializeField]
#endif
        protected string serverIP = "";

        [SerializeField]
        protected int moduleID = 0;

        [SerializeField]
        protected string moduleName = "Generic";

#if PIXOVR_DEBUG
        [SerializeField]
#endif
        protected string moduleVersion = "";

        [SerializeField]
        protected string scenarioID = "Generic";

#if PIXOVR_DEBUG
        [SerializeField]
#endif
        protected bool runSetupOnAwake = true;

#if PIXOVR_DEBUG
        [SerializeField]
#endif
        protected bool loginCheckModuleAccess = true;

#if PIXOVR_DEBUG
        [SerializeField]
#endif
        protected float heartbeatTime = 5.0f;

        protected string webSocketUrl;
        protected string deviceID;
        protected string deviceModel;
        protected string platform;
        protected string clientIP;
        protected Guid currentSessionID;
        protected int heartbeatSessionID;
        protected float heartbeatTimer;
        protected bool sessionInProgress;
        protected bool userAccessVerified = false;
        protected string deviceSerialNumber = "";
        protected bool hasParsedArguments = false;

        protected string loginToken = "";
        protected string optionalParameter = "";
        protected string currentExitTargetParameter = "";
        protected string targetTypeParameter = "";

        protected LoginResponseContent currentActiveLogin = null;
        protected APIHandler apexAPIHandler;
        protected ApexWebsocket webSocket;
        protected Task<bool> socketConnectTask;
        protected Task socketDisconnectTask;

        public OnModuleAccessSuccessEvent OnModuleAccessSuccess = new OnModuleAccessSuccessEvent();
        public OnApexFailureEvent OnModuleAccessFailed = new OnApexFailureEvent();

        public OnLoginSuccessEvent OnLoginSuccess = new OnLoginSuccessEvent();
        public OnApexFailureEvent OnLoginFailed = new OnApexFailureEvent();

        public OnGetUserSuccessEvent OnGetUserSuccess = new OnGetUserSuccessEvent();
        public OnApexFailureEvent OnGetUserFailed = new OnApexFailureEvent();

        public OnGetUserModulesSuccessEvent OnGetUserModulesSuccess = new OnGetUserModulesSuccessEvent();
        public OnApexFailureEvent OnGetUserModulesFailed = new OnApexFailureEvent();

        public OnGetOrgModulesSuccessEvent OnGetOrganizationModulesSuccess = new OnGetOrgModulesSuccessEvent();
        public OnApexFailureEvent OnGetOrganizationModulesFailed = new OnApexFailureEvent();

        public PlatformResponse OnPlatformResponse = null;

        public OnAuthCodeReceived OnAuthorizationCodeReceived = new OnAuthCodeReceived();

        public OnGeneratedAssistedLoginSuccessEvent OnGeneratedAssistedLoginSuccess = new();
        public OnApexFailureEvent OnGeneratedAssistedLoginFailed = new();

        public OnGetQuickIDAuthUsersSuccessEvent OnGetQuickIDAuthGetUsersSuccess = new();
        public OnApexFailureEvent OnGetQuickIDAuthGetUsersFailed = new();

        public OnQuickIDAuthLoginSuccessEvent OnQuickIDAuthLoginSuccess = new();
        public OnApexFailureEvent OnQuickIDAuthLoginFailed = new();

        void Awake()
        {
            Debug.unityLogger.Log(LogType.Log, TAG, $"ApexSystem found on {gameObject.name}");
            if (!InitializeInstance(this))
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "Instance already initialized.");
            }

            var projectSettings = Resources.Load<Editor.PixoVRProjectSettings>("PixoVRProjectSettings");
            if (projectSettings != null)
            {
                if (string.IsNullOrEmpty(moduleVersion))
                {
                    moduleVersion = projectSettings.ModuleVersion;
                }
            }

            SetupPlatformConfiguration();

#if UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            SetupDeepLinking();
#endif
            if (runSetupOnAwake)
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "Running on awake!");
                SetupAPI();
            }

            DontDestroyOnLoad(gameObject);
#if MANAGE_XR && !UNITY_EDITOR
            Debug.unityLogger.Log(LogType.Log, TAG, "Using ManageXR");
            InitMXRSDK();
#endif
        }

#if MANAGE_XR
        async void InitMXRSDK()
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "Initializing the ManageXR SDK");
            await MXRManager.InitAsync();
            MXRManager.System.OnDeviceStatusChange += OnDeviceStatusChanged;
            deviceSerialNumber = MXRManager.System.DeviceStatus.serial;
            Debug.unityLogger.Log(LogType.Log, TAG, $"Device serial set to {deviceSerialNumber}");
        }

        void OnDeviceStatusChanged(DeviceStatus newDeviceStatus)
        {
            deviceSerialNumber = newDeviceStatus.serial;
            Debug.unityLogger.Log(LogType.Log, TAG, $"Device serial number changed to {deviceSerialNumber}");
        }
#endif

        void SetupDeepLinking()
        {
            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                // Cold start and Application.absoluteURL not null so process Deep Link.
                OnDeepLinkActivated(Application.absoluteURL);
            }
        }

        void OnDeepLinkActivated(string url)
        {
            // Update DeepLink Manager global variable, so URL can be accessed from anywhere.
            var urlArguments = PixoPlatformUtilities.ParseURLArguments(url);
            _ParsePassedData(urlArguments);
        }

        void SetupPlatformConfiguration()
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "SetupPlatformConfiguration");

#if UNITY_ANDROID
            if (PixoAndroidUtils.DoesFileExistInSharedLocation("pixoconfig.cnf"))
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "Found pixoconfig.cnf");

                string configContent = PixoAndroidUtils.ReadFileFromSharedStorage("pixoconfig.cnf");

                if (configContent.Length > 0)
                {
                    Debug.unityLogger.Log(LogType.Log, TAG, "Configuration is not empty.");
                    ConfigurationTypes configData = JsonConvert.DeserializeObject<ConfigurationTypes>(configContent);
                    if (configData == null)
                    {
                        Debug.unityLogger.Log(LogType.Log, TAG, "Failed to deserialize the config.");
                        return;
                    }

                    // Parse out the platform target to utilize the unity built in config values
                    if (configData.Platform.Contains("NA", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (configData.Platform.Contains("Production", StringComparison.CurrentCultureIgnoreCase))
                        {
                            Debug.unityLogger.Log(LogType.Log, TAG, "NA Production platform target.");
                            PlatformTargetServer = PlatformServer.NA_PRODUCTION;
                        }

                        if (configData.Platform.Contains("Dev", StringComparison.CurrentCultureIgnoreCase))
                        {
                            Debug.unityLogger.Log(LogType.Log, TAG, "NA Dev platform target.");
                            PlatformTargetServer = PlatformServer.NA_DEV;
                        }

                        if (configData.Platform.Contains("Stage", StringComparison.CurrentCultureIgnoreCase))
                        {
                            Debug.unityLogger.Log(LogType.Log, TAG, "NA Stage platform target.");
                            PlatformTargetServer = PlatformServer.NA_STAGE;
                        }
                    }
                    else if (configData.Platform.Contains("SA", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Debug.unityLogger.Log(LogType.Log, TAG, "SA Production platform target.");
                        PlatformTargetServer = PlatformServer.SA_PRODUCTION;
                    }

                    // TODO (MGruber): Add a custom value, but this requires multiple configuration values to be saved.
                    // Need to save the normal headset api endpoint, web api endpoint and platform api endpoint. 3 VALUES! D:
                }
            }
#endif
        }

        void SetupAPI()
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "Mac Address: " + ApexUtils.GetMacAddress());
            if (serverIP.Length == 0)
            {
                serverIP = GetEndpointFromTarget(PlatformTargetServer);
            }

            apexAPIHandler = new APIHandler(serverIP);

            if (apexAPIHandler != null)
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "Apex API Handler is not null!");
            }

            // TODO: Move to new plugin
            apexAPIHandler.SetPlatformEndpoint(GetPlatformEndpointFromPlatformTarget(PlatformTargetServer));
            apexAPIHandler.OnAPIResponse += OnAPIResponse;

            if (webSocket != null)
            {
                if (webSocket.IsConnected())
                {
                    DisconnectWebsocket();
                }
            }

            webSocket = new ApexWebsocket();
            webSocket.OnConnectSuccess.AddListener(() => OnWebSocketConnected());
            webSocket.OnConnectFailed.AddListener((reason) => OnWebSocketConnectFailed(reason));
            webSocket.OnReceive.AddListener((data) => OnWebSocketReceive(data));
            webSocket.OnClosed.AddListener((reason) => OnWebSocketClosed(reason));

            PopulateWebSocketURL();

            if (!hasParsedArguments)
            {
                var applicationArugments = PixoPlatformUtilities.ParseApplicationArguments();
                _ParsePassedData(applicationArugments);
                Debug.unityLogger.Log(LogType.Log, TAG, $"Login Token: {(string.IsNullOrEmpty(PassedLoginToken) ? "<Null>" : PassedLoginToken)}");
                hasParsedArguments = true;
            }
        }

        void _ExitApplication(string nextExitApplication)
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "ApexSystem::_ExitApplication");
            if (nextExitApplication == null)
            {
                nextExitApplication = "";
            }

            string returnTargetType = "app";

            if (nextExitApplication.Contains("://"))
            {
                returnTargetType = "url";
            }

            Debug.unityLogger.Log(LogType.Log, TAG, "" + nextExitApplication + " " + returnTargetType);

            string parameters = "";

            Debug.unityLogger.Log(LogType.Log, TAG, "Building parameters for url.");

            if (CurrentActiveLogin != null)
            {
                parameters += "pixotoken=" + CurrentActiveLogin.Token;
            }
            else if (!string.IsNullOrEmpty(PassedLoginToken))
            {
                parameters += "pixotoken=" + PassedLoginToken;
            }

            if (optionalParameter != null)
            {
                if (optionalParameter.Length > 0)
                {
                    if (parameters.Length > 0)
                        parameters += "&";
                    parameters += "optional=" + optionalParameter;
                }
            }

            if (nextExitApplication.Length > 0)
            {
                if (parameters.Length > 0)
                    parameters += "&";
                parameters += "returntarget=" + nextExitApplication;
            }

            if (returnTargetType.Length > 0)
            {
                if (parameters.Length > 0)
                    parameters += "&";
                parameters += "targettype=" + returnTargetType;
            }

            Debug.unityLogger.Log(LogType.Log, TAG, "Checking the return target parameter.");

            if (!string.IsNullOrEmpty(CurrentExitTarget))
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "Had a valid return target parameter.");
                if (targetTypeParameter.Equals("url", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.unityLogger.Log(LogType.Log, TAG, "Return Target is a URL.");

                    string returnURL = CurrentExitTarget;
                    if (!string.IsNullOrEmpty(parameters))
                    {
                        if (!returnURL.Contains('?'))
                            returnURL += "?";
                        else
                            returnURL += "&";

                        returnURL += parameters;
                    }
                    Debug.unityLogger.Log(LogType.Log, TAG, "Custom Target: " + returnURL);
                    PixoPlatformUtilities.OpenURL(returnURL);
                    PixoPlatformUtilities.CloseCurrentApplication();
                    return;
                }
                else
                {
                    Debug.unityLogger.Log(LogType.Log, TAG, $"Return Target is a package name. {CurrentExitTarget}");

                    List<string> keys = new List<string>(),
                        values = new List<string>();

                    Debug.unityLogger.Log(LogType.Log, TAG, "Adding pixo token.");

                    if (CurrentActiveLogin != null)
                    {
                        keys.Add("pixotoken");
                        values.Add(CurrentActiveLogin.Token);
                    }
                    else if (!string.IsNullOrEmpty(PassedLoginToken))
                    {
                        keys.Add("pixotoken");
                        values.Add(PassedLoginToken);
                    }

                    Debug.unityLogger.Log(LogType.Log, TAG, "Adding optional.");

                    if (!string.IsNullOrEmpty(optionalParameter))
                    {
                        keys.Add("optional");
                        values.Add(optionalParameter);
                    }

                    Debug.unityLogger.Log(LogType.Log, TAG, "Adding return target.");

                    if (!string.IsNullOrEmpty(nextExitApplication))
                    {
                        keys.Add("returntarget");
                        values.Add(nextExitApplication);
                    }

                    Debug.unityLogger.Log(LogType.Log, TAG, "Adding return target type.");

                    if (!string.IsNullOrEmpty(returnTargetType))
                    {
                        keys.Add("targettype");
                        values.Add(returnTargetType);
                    }

                    PixoPlatformUtilities.OpenApplication(CurrentExitTarget, keys.ToArray(), values.ToArray());
                    PixoPlatformUtilities.CloseCurrentApplication();
                    return;
                }
            }

            PixoPlatformUtilities.CloseCurrentApplication();
        }

        string GetEndpointFromTarget(PlatformServer target)
        {
            return target.ToUrlString();
        }

        string GetPlatformEndpointFromPlatformTarget(PlatformServer target)
        {
            int targetValue = (int)target;
            APIPlatformServer apiTarget = (APIPlatformServer)targetValue;

            return apiTarget.ToUrlString();
        }

        void PopulateWebSocketURL()
        {
            webSocketUrl = serverIP;

            if (webSocketUrl.Contains("://"))
            {
                webSocketUrl = webSocketUrl.Split(new string[] { "://" }, 2, StringSplitOptions.RemoveEmptyEntries)[1];
            }

            if (webSocketUrl.Contains("/"))
            {
                webSocketUrl = webSocketUrl.Split(new string[] { "/" }, 2, StringSplitOptions.RemoveEmptyEntries)[0];
            }

            webSocketUrl = "wss://" + webSocketUrl + "/ws";
        }

        void Start()
        {
            if (!IsModuleVersionValid())
            {
                Debug.unityLogger.Log(LogType.Warning, TAG, $"{moduleVersion} is an invalid module version.");
            }
            deviceID = SystemInfo.deviceUniqueIdentifier;
            deviceModel = SystemInfo.deviceModel;
            platform =
                XRSettings.loadedDeviceName.Length > 0 ? XRSettings.loadedDeviceName : Application.platform.ToString();
            clientIP = Utils.ApexUtils.GetLocalIP();
        }

        private void FixedUpdate()
        {
            if (webSocket != null)
            {
                webSocket.Update();
            }

            if (sessionInProgress)
            {
                heartbeatTimer -= Time.fixedDeltaTime;

                if (heartbeatTimer <= 0.0f)
                {
                    _SendHeartbeat();
                    heartbeatTimer += heartbeatTime;
                }
            }
        }

        void ConnectWebsocket()
        {
            socketConnectTask = Task.Run(() => webSocket.Connect(new Uri(webSocketUrl)));
        }

        void DisconnectWebsocket()
        {
            socketDisconnectTask = Task.Run(() => webSocket.CloseSocket());
        }

        void OnWebSocketConnected()
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "Websocket connected successfully.");
        }

        void OnWebSocketConnectFailed(string reason)
        {
            Debug.unityLogger.Log(LogType.Error, TAG, "Websocket failed to connect with error: " + reason);
        }

        void OnWebSocketReceive(string data)
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "Websocket received: " + data);
            try
            {
                if (data.Contains("auth_code"))
                {
                    var authCode = JsonConvert.DeserializeObject<AuthorizationCode>(data);
                    OnAuthorizationCodeReceived.Invoke(authCode.Code);
                }

                if (data.Contains("Token", StringComparison.OrdinalIgnoreCase))
                {
                    object loginResponse = JsonConvert.DeserializeObject<LoginResponseContent>(data);
                    HandleLogin(true, loginResponse);
                }
            }
            catch (Exception ex)
            {
                Debug.unityLogger.Log(LogType.Log, TAG, ex.Message);
            }
        }

        void OnWebSocketClosed(System.Net.WebSockets.WebSocketCloseStatus reason)
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "Websocket closed with reason: " + reason);
        }

        bool IsModuleVersionValid()
        {
            if (IsModuleVersionOnlyNumerical() == false)
                return false;

            string[] moduleVersionParts = moduleVersion.Split('.');

            if (moduleVersionParts.Length != 3)
                return false;

            if (!IsModuleMajorVersionPartValid(moduleVersionParts[(int)VersionParts.Major]))
                return false;

            if (!IsModuleNonMajorVersionPartValid(moduleVersionParts[(int)VersionParts.Minor]))
                return false;

            if (!IsModuleNonMajorVersionPartValid(moduleVersionParts[(int)VersionParts.Patch]))
                return false;

            return true;
        }

        static readonly Regex VersionValidator = new Regex(@"^[0123456789.]+$");

        bool IsModuleVersionOnlyNumerical()
        {
            return VersionValidator.IsMatch(moduleVersion);
        }

        bool IsModuleNonMajorVersionPartValid(string modulePart)
        {
            if (modulePart.Length <= 0)
                return false;

            if (modulePart.Length > 2)
                return false;

            return true;
        }

        bool IsModuleMajorVersionPartValid(string modulePart)
        {
            if (modulePart.Length <= 0)
                return false;

            if (modulePart.StartsWith("0"))
                return false;

            return true;
        }

        public static void ExitApplication(string nextExitTarget = "")
        {
            Instance._ExitApplication(nextExitTarget);
        }

        public static bool RequestAuthorizationCode()
        {
            return Instance._RequestAuthorizationCode();
        }

        public static void ChangePlatformServer(PlatformServer newServer)
        {
            Instance._ChangePlatformServer(newServer);
        }

        public static void Ping(Action<HttpResponseMessage, object> success = null, Action<HttpResponseMessage, FailureResponse> failure = null)
        {
            ApexAPIHandler.Ping(success, failure);
        }

        public static bool LoginWithToken()
        {
            Debug.unityLogger.Log(LogType.Log, TAG, $"Login with token of token {(string.IsNullOrEmpty(PassedLoginToken) ? "<none>" : PassedLoginToken)}");
            return LoginWithToken(PassedLoginToken);
        }

        public static bool LoginWithToken(string token)
        {
            Debug.unityLogger.Log(LogType.Log, TAG, $"Calling LoginWithToken ({token})");
            if (token.Length <= 0)
            {
                return false;
            }

            Debug.unityLogger.Log(LogType.Log, TAG, $"Logging in with token: {token}");
            ApexAPIHandler.LoginWithToken(token);

            return true;
        }

        public static bool Login(LoginData login)
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "_Login called.");
            if (ApexAPIHandler == null)
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "API Handler is null.");
                Instance.OnLoginFailed.Invoke(
                    Instance.GenerateFailureResponse(
                        "There was an error reaching the platform, please contact your administrator."
                    )
                );
                return false;
            }

            if (login.Login.Length <= 0)
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "[Login] No user name.");
                Instance.OnLoginFailed.Invoke(Instance.GenerateFailureResponse("No username or email entered."));
                return false;
            }

            if (login.Password.Length <= 0)
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "[Login] No password.");
                login.Password = "<empty>";
            }

            ApexAPIHandler.Login(login);

            Debug.unityLogger.Log(LogType.Log, TAG, "Login called.");

            return true;
        }

        public static bool Login(string username, string password)
        {
            return Login(new LoginData(username, password));
        }

        public static bool CheckModuleAccess(int targetModuleID = -1)
        {
            return Instance._CheckModuleAccess(targetModuleID);
        }

        public static void JoinSession(string scenarioID = null, Extension contextExtension = null, Action<HttpResponseMessage, JoinSessionResponse> success = null, Action<HttpResponseMessage, FailureResponse> failure = null)
        {
            Instance._JoinSession(scenarioID, contextExtension, (response, formattedResponse) =>
            {
                Debug.unityLogger.Log(LogType.Log, TAG, string.Format("[ApexSystem] Session Id is {0}.", formattedResponse.SessionId));
                Instance.heartbeatSessionID = formattedResponse.SessionId;
                Instance.sessionInProgress = true;
                success?.Invoke(response, formattedResponse);
            }, (response, formattedResponse) =>
            {
                Instance.sessionInProgress = false;
                Instance.currentSessionID = Guid.Empty;
                failure?.Invoke(response, formattedResponse);
            });
        }

        public static void CompleteSession(SessionData currentSessionData, Extension contextExtension = null, Extension resultExtension = null, Action<HttpResponseMessage, object> success = null, Action<HttpResponseMessage, FailureResponse> failure = null)
        {
            Instance._CompleteSession(currentSessionData, contextExtension, resultExtension, (response, formattedResponse) =>
            {
                Instance.sessionInProgress = false;
                Instance.currentSessionID = Guid.Empty;
                success?.Invoke(response, formattedResponse);
            }, failure);
        }

        public static void SendSimpleSessionEvent(string action, string targetObject, Extension contextExtension, Action<HttpResponseMessage, object> success = null, Action<HttpResponseMessage, FailureResponse> failure = null)
        {
            Instance._SendSimpleSessionEvent(action, targetObject, contextExtension, success, failure);
        }

        public static void SendSessionEvent(Statement eventStatement, Action<HttpResponseMessage, object> success = null, Action<HttpResponseMessage, FailureResponse> failure = null)
        {
            Instance._SendSessionEvent(eventStatement, success, failure);
        }

        public static bool GetCurrentUser()
        {
            return GetUser();
        }

        public static bool GetUser(int userId = -1)
        {
            return Instance._GetUser(userId);
        }

        public static bool GetCurrentUserModules()
        {
            return GetUserModules();
        }

        public static bool GetUserModules(int userId = -1)
        {
            return Instance._GetUserModules(userId);
        }

        public static bool GetModulesList(string platformName)
        {
            return Instance._GetModuleList(platformName);
        }

        public static bool GetQuickIDAuthUsers(string serialNumber)
        {
            return Instance._GetQuickIDAuthUsers(serialNumber);
        }

        public static bool QuickIDLogin(string serialNumber, string username)
        {
            return Instance._QuickIDLogin(serialNumber, username);
        }

        protected void _ChangePlatformServer(PlatformServer newServer)
        {
            PlatformTargetServer = newServer;

            SetupAPI();
        }

        protected bool _LoginWithToken(string token)
        {
            Debug.unityLogger.Log(LogType.Log, TAG, $"Calling LoginWithToken ({token})");
            if (token.Length <= 0)
            {
                return false;
            }

            Debug.unityLogger.Log(LogType.Log, TAG, $"Logging in with token: {token}");
            apexAPIHandler.LoginWithToken(token);

            return true;
        }

        protected FailureResponse GenerateFailureResponse(string message)
        {
            FailureResponse failureResponse = new FailureResponse()
            {
                Error = "True",
                HttpCode = "400",
                Message = message,
            };

            return failureResponse;
        }

        protected void _ParsePassedData(Dictionary<string, string> arguments)
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "Parsing passed data.");
            if (arguments == null)
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "No arguments found for the application.");
                return;
            }

            if (arguments.ContainsKey("optional"))
                optionalParameter = arguments["optional"];

            if (arguments.ContainsKey("returntarget"))
                CurrentExitTarget = arguments["returntarget"];

            if (arguments.ContainsKey("targettype"))
                TargetType = arguments["targettype"];

            Debug.unityLogger.Log(LogType.Log, TAG, "Is the token already set?");
            if (string.IsNullOrEmpty(PassedLoginToken))
            {
                Debug.unityLogger.Log(LogType.Log, TAG, "Token is not set, but does the arguments contain a token?");
                if (arguments.ContainsKey("pixotoken"))
                {
                    Debug.unityLogger.Log(LogType.Log, TAG, "Token was found in the arguments.");
                    string token = arguments["pixotoken"];
                    Debug.unityLogger.Log(LogType.Log, TAG, $"Found pixotoken {token}.");
                    if (!string.IsNullOrEmpty(token))
                    {
                        PassedLoginToken = string.Copy(token);
                    }
                }
            }
        }

        protected bool _CheckModuleAccess(int targetModuleID = -1)
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "_CheckModuleAccess called.");

            if (currentActiveLogin == null)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "Cannot check user's module access with no active login.");
                return false;
            }

            if (targetModuleID <= -1)
            {
                targetModuleID = moduleID;
            }

            Debug.unityLogger.Log(LogType.Log, TAG, $"Checking module access of module {targetModuleID} from user {currentActiveLogin.ID} and device serial number {(string.IsNullOrEmpty(deviceSerialNumber) == true ? "---" : deviceSerialNumber)}");
            apexAPIHandler.GetModuleAccess(targetModuleID, currentActiveLogin.ID, deviceSerialNumber);

            return true;
        }

        protected void _JoinSession(string newScenarioID, Extension contextExtension, Action<HttpResponseMessage, JoinSessionResponse> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            if (currentActiveLogin == null)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "Cannot join session with no active login.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "Cannot join session with no active login." });
                return;
            }

            if (userAccessVerified == false)
            {
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "User access not verified." });
                return;
            }

            if (newScenarioID != null)
            {
                scenarioID = newScenarioID;
            }

            if (sessionInProgress == true)
            {
                Debug.unityLogger.Log(LogType.Error, TAG,
                    "Session is already in progress."
                        + " The previous session didn't complete or a new session was started during an active session."
                );
            }

            currentSessionID = Guid.NewGuid();

            Statement sessionStatement = new Statement();
            Agent sessionActor = new Agent();
            sessionActor.mbox = currentActiveLogin.Email;

            Verb sessionVerb = new Verb();
            sessionVerb.id = ApexVerbs.JOINED_SESSION;
            sessionVerb.display = new LanguageMap();
            sessionVerb.display.Add("en", "Joined Session");

            Activity sessionActivity = new Activity();
            sessionActivity.id = string.Format("https://pixovr.com/xapi/objects/{0}/{1}", moduleID, scenarioID);

            Context sessionContext = new Context();
            sessionContext.registration = currentSessionID;
            sessionContext.revision = moduleVersion;
            sessionContext.platform = platform;

            sessionContext.extensions = AppendStandardContextExtension(contextExtension);

            sessionStatement.actor = sessionActor;
            sessionStatement.verb = sessionVerb;
            sessionStatement.target = sessionActivity;
            sessionStatement.context = sessionContext;

            JoinSessionData sessionData = new JoinSessionData();
            sessionData.DeviceId = deviceID;
            sessionData.IpAddress = clientIP;
            sessionData.ModuleId = moduleID;
            sessionData.Uuid = currentSessionID.ToString();
            sessionData.EventType = ApexEventTypes.PIXOVR_SESSION_JOINED;
            sessionData.JsonData = sessionStatement;

            apexAPIHandler.JoinSession(currentActiveLogin.Token, sessionData, success, failure);
        }

        protected void _SendSimpleSessionEvent(string verbName, string targetObject, Extension contextExtension, Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            if (string.IsNullOrEmpty(verbName))
            {
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "Verb name is invalid." });
                return;
            }

            Statement sessionStatement = new Statement();

            Verb sessionVerb = new Verb();
            sessionVerb.id = new Uri("https://pixovr.com/xapi/verbs/" + verbName.Replace(' ', '_').ToLower());
            sessionVerb.display = new LanguageMap();
            sessionVerb.display.Add("en", verbName);

            Activity sessionActivity = new Activity();
            sessionActivity.id = string.Format(
                "https://pixovr.com/xapi/objects/{0}/{1}/{2}",
                moduleID,
                scenarioID,
                targetObject.Replace(' ', '_').ToLower()
            );

            Context sessionContext = new Context();
            sessionContext.registration = currentSessionID;
            sessionContext.revision = moduleVersion;
            sessionContext.platform = platform;

            sessionContext.extensions = AppendStandardContextExtension(contextExtension);

            sessionStatement.actor = null;
            sessionStatement.verb = sessionVerb;
            sessionStatement.target = sessionActivity;
            sessionStatement.context = sessionContext;

            SessionEventData sessionEvent = new SessionEventData();
            sessionEvent.DeviceId = deviceID;
            sessionEvent.ModuleId = ModuleID;
            sessionEvent.Uuid = currentSessionID.ToString();
            sessionEvent.EventType = ApexEventTypes.PIXOVR_SESSION_EVENT;
            sessionEvent.JsonData = sessionStatement;

            _SendSessionEvent(sessionStatement, success, failure);
        }

        protected void _SendSessionEvent(Statement eventStatement, Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            if (userAccessVerified == false)
            {
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "User access not verified." });
                return;
            }

            if (currentActiveLogin == null)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "Cannot send a session event with no active login.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "Cannot send a session event with no active login." });
                return;
            }

            if (sessionInProgress == false)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "No session in progress to send event for.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "No session in progress to send event for." });
                return;
            }

            if (eventStatement == null)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "No event data to send.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "No event data to send." });
                return;
            }

            if (eventStatement.actor != null)
            {
                Debug.unityLogger.Log(LogType.Warning, TAG, "Actor data should not be filled out.");
            }

            if (eventStatement.verb == null)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "Verb missing from eventStatement.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "Verb missing from eventStatement." });
                return;
            }

            if (eventStatement.verb.id == null)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "verb.id missing from eventStatement.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "verb.id missing from eventStatement." });
                return;
            }

            if (eventStatement.target == null)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "Object (target) missing from eventStatement.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "Object (target) missing from eventStatement." });
                return;
            }

            eventStatement.actor = new Agent();
            eventStatement.actor.mbox = currentActiveLogin.Email;

            if (eventStatement.context == null)
            {
                eventStatement.context = new Context();
            }

            eventStatement.context.registration = currentSessionID;
            eventStatement.context.revision = ModuleVersion;
            eventStatement.context.platform = platform;

            eventStatement.context.extensions = AppendStandardContextExtension(eventStatement.context.extensions);

            SessionEventData sessionEvent = new SessionEventData();
            sessionEvent.DeviceId = deviceID;
            sessionEvent.ModuleId = ModuleID;
            sessionEvent.Uuid = currentSessionID.ToString();
            sessionEvent.EventType = ApexEventTypes.PIXOVR_SESSION_EVENT;
            sessionEvent.JsonData = eventStatement;

            apexAPIHandler.SendSessionEvent(currentActiveLogin.Token, sessionEvent, success, failure);
        }

        protected void _CompleteSession(SessionData currentSessionData, Extension contextExtension, Extension resultExtension, Action<HttpResponseMessage, object> success = null, Action<HttpResponseMessage, FailureResponse> failure = null)
        {
            if (userAccessVerified == false)
            {
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "User access not verified." });
                return;
            }

            if (currentActiveLogin == null)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "Cannot complete session with no active login.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "Cannot complete session with no active login." });
                return;
            }

            if (sessionInProgress == false)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "No session in progress to complete.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "No session in progress to complete." });
                return;
            }

            // Create our actor
            Agent sessionActor = new Agent();
            sessionActor.mbox = currentActiveLogin.Email;

            // Create our verb
            Verb sessionVerb = new Verb();
            sessionVerb.id = ApexVerbs.COMPLETED_SESSION;
            sessionVerb.display = new LanguageMap();
            sessionVerb.display.Add("en", "Completed Session");

            // Create the session activity
            Activity sessionActivity = new Activity();
            sessionActivity.id = string.Format("https://pixovr.com/xapi/objects/{0}/{1}", moduleID, scenarioID);

            // Create our context
            Context sessionContext = new Context();
            sessionContext.registration = currentSessionID;
            sessionContext.revision = moduleVersion;
            sessionContext.platform = platform;

            sessionContext.extensions = AppendStandardContextExtension(contextExtension);

            // Create our results
            Result sessionResult = new Result();
            sessionResult.completion = currentSessionData.Complete;
            sessionResult.success = currentSessionData.Success;
            // Add score to the results
            sessionResult.score = new Score();
            sessionResult.score.min = currentSessionData.MinimumScore;
            sessionResult.score.max = currentSessionData.MaximumScore;
            sessionResult.score.raw = currentSessionData.Score;
            sessionResult.score.scaled = DetermineScaledScore(
                currentSessionData.ScaledScore,
                currentSessionData.Score,
                currentSessionData.MaximumScore
            );
            sessionResult.duration = TimeSpan.FromSeconds(currentSessionData.Duration);
            if (resultExtension != null)
            {
                sessionResult.extensions = new Extensions(resultExtension.ToJObject());
            }

            // Create our statement and add the pieces
            Statement sessionStatement = new Statement();
            sessionStatement.actor = sessionActor;
            sessionStatement.verb = sessionVerb;
            sessionStatement.target = sessionActivity;
            sessionStatement.context = sessionContext;
            sessionStatement.result = sessionResult;

            CompleteSessionData sessionData = new CompleteSessionData();
            sessionData.DeviceId = deviceID;
            sessionData.ModuleId = moduleID;
            sessionData.Uuid = currentSessionID.ToString();
            sessionData.EventType = ApexEventTypes.PIXOVR_SESSION_COMPLETE;
            sessionData.JsonData = sessionStatement;
            sessionData.SessionDuration = currentSessionData.Duration;
            sessionData.Score = currentSessionData.Score;
            sessionData.ScoreMin = currentSessionData.MinimumScore;
            sessionData.ScoreMax = currentSessionData.MaximumScore;
            sessionData.ScoreScaled = DetermineScaledScore(
                currentSessionData.ScaledScore,
                currentSessionData.Score,
                currentSessionData.MaximumScore
            );

            apexAPIHandler.CompleteSession(currentActiveLogin.Token, sessionData, success, failure);
        }

        protected bool _SendHeartbeat()
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "Sending heartbeat...");
            if (!sessionInProgress)
                return false;

            if (currentActiveLogin == null)
                return false;

            apexAPIHandler.SendHeartbeat(currentActiveLogin.Token, heartbeatSessionID);

            return true;
        }

        protected bool _GetUser(int userId = -1)
        {
            if (currentActiveLogin == null)
                return false;

            if (userId < 0)
            {
                userId = currentActiveLogin.ID;
            }

            apexAPIHandler.GetUserData(currentActiveLogin.Token, userId);
            return true;
        }

        protected bool _GetUserModules(int userId = -1)
        {
            if (currentActiveLogin == null)
                return false;

            if (userId < 0)
            {
                userId = currentActiveLogin.ID;
            }

            apexAPIHandler.GetUserModules(currentActiveLogin.Token, userId);
            return true;
        }

        protected bool _GetModuleList(string platformName)
        {
            if (currentActiveLogin == null)
                return false;

            apexAPIHandler.GetModuleList(currentActiveLogin.Token, platformName);
            return true;
        }

        protected bool _GetQuickIDAuthUsers(string serialNumber)
        {
            if (String.IsNullOrEmpty(serialNumber)) return false;
            apexAPIHandler.GetQuickIDAuthenticationUsers(serialNumber);
            return true;
        }


        protected bool _QuickIDLogin(string serialNumber, string username)
        {
            if (String.IsNullOrEmpty(serialNumber) || string.IsNullOrEmpty(username)) return false;
            var loginData = new QuickIDLoginData(serialNumber, username);
            apexAPIHandler.QuickIDLogin(loginData);
            return true;
        }

        private float DetermineScaledScore(float scaledScore, float score, float maxScore)
        {
            float determinedScaledScore = scaledScore;

            if (scaledScore < Mathf.Epsilon && score >= Mathf.Epsilon)
            {
                determinedScaledScore = (score / maxScore) * 100f;
            }

            return determinedScaledScore;
        }

        private Extensions AppendStandardContextExtension(Extensions currentContextExtensions)
        {
            return AppendStandardContextExtension(new Extension(currentContextExtensions.ToJObject()));
        }

        private Extensions AppendStandardContextExtension(Extension currentContextExtension)
        {
            Extension contextExtension;
            if (currentContextExtension != null)
            {
                contextExtension = currentContextExtension;
            }
            else
            {
                contextExtension = new Extension();
            }

            contextExtension.Add(ApexExtensionStrings.MODULE_ID, moduleID.ToString());
            contextExtension.AddSimple("device_id", deviceID);
            contextExtension.AddSimple("device_model", deviceModel);
            contextExtension.AddSimple("sdk_version", "unity-" + ApexUtils.SDKVersion);

            if (string.IsNullOrEmpty(deviceSerialNumber))
            {
                contextExtension.AddSimple("device_serial", deviceSerialNumber.ToString());
            }

            return new Extensions(contextExtension.ToJObject());
        }

        protected void OnAPIResponse(ResponseType response, HttpResponseMessage message, object responseData)
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "On API Response");
            bool success = message.IsSuccessStatusCode;
            if (responseData is FailureResponse)
            {
                success = success && (responseData is IFailure) && (!(responseData as FailureResponse).HasErrored());
            }

            switch (response)
            {
                case ResponseType.RT_LOGIN:
                    {
                        Debug.unityLogger.Log(LogType.Log, TAG, "Calling to handle login.");
                        HandleLogin(success, responseData);
                        break;
                    }
                case ResponseType.RT_GET_USER:
                    {
                        if (success)
                        {
                            OnGetUserSuccess.Invoke(responseData as GetUserResponseContent);
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.unityLogger.Log(LogType.Log, TAG, string.Format("Failed to get user.\nError: {0}", failureData.Message));
                            OnGetUserFailed.Invoke(responseData as FailureResponse);
                        }
                        break;
                    }
                case ResponseType.RT_GET_USER_MODULES:
                    {
                        if (success)
                        {
                            OnGetUserModulesSuccess.Invoke(responseData as GetUserModulesResponse);
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.unityLogger.Log(LogType.Log, TAG, string.Format("Failed to get user.\nError: {0}", failureData.Message));
                            OnGetUserFailed.Invoke(responseData as FailureResponse);
                        }
                        break;
                    }
                case ResponseType.RT_GET_USER_ACCESS:
                    {
                        if (success)
                        {
                            var userAccessResponseContent = responseData as UserAccessResponseContent;
                            if (userAccessResponseContent.Access)
                            {
                                if (userAccessResponseContent.PassingScore.HasValue)
                                {
                                    currentActiveLogin.MinimumPassingScore = userAccessResponseContent.PassingScore.Value;
                                }

                                userAccessVerified = true;
                                OnModuleAccessSuccess.Invoke(currentActiveLogin);
                            }
                            else
                            {
                                currentActiveLogin = null;
                                userAccessVerified = false;
                                OnModuleAccessFailed.Invoke(
                                    new FailureResponse()
                                    {
                                        Error = "True",
                                        HttpCode = "401",
                                        Message = "User does not have access to module",
                                    }
                                );
                            }
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.unityLogger.Log(LogType.Log, TAG,
                                string.Format(
                                    "Failed to get users module access data.\nError: {0}",
                                    failureData.Message
                                )
                            );

                            OnModuleAccessFailed.Invoke(responseData as FailureResponse);
                        }
                        break;
                    }
                case ResponseType.RT_GET_MODULES_LIST:
                    {
                        if (success)
                        {
                            OnGetOrganizationModulesSuccess.Invoke(responseData as List<OrgModule>);
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.unityLogger.Log(LogType.Log, TAG,
                                string.Format("Failed to get org modules.\nError: {0}", failureData.Message)
                            );

                            OnGetOrganizationModulesFailed.Invoke(responseData as FailureResponse);
                        }

                        break;
                    }
                case ResponseType.RT_QUICK_ID_AUTH_GET_USERS:
                    {
                        if (success)
                        {
                            OnGetQuickIDAuthGetUsersSuccess.Invoke(responseData as QuickIDAuthGetUsersResponse);
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.unityLogger.Log(LogType.Log, TAG, string.Format("Failed to get Quick ID Authentication users.\nError: {0}", failureData.Message));
                            OnGetQuickIDAuthGetUsersFailed.Invoke(responseData as FailureResponse);
                        }
                        break;
                    }

                case ResponseType.RT_QUICK_ID_AUTH_LOGIN:
                    {
                        HandleLogin(success, responseData);
                        if (success)
                        {
                            OnQuickIDAuthLoginSuccess.Invoke(responseData as LoginResponseContent);
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.unityLogger.Log(LogType.Log, TAG, string.Format("Failed to authenticate with Quick ID Authentication.\nError: {0}", failureData.Message));
                            OnQuickIDAuthLoginFailed.Invoke(responseData as FailureResponse);
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            if (OnPlatformResponse != null)
            {
                OnPlatformResponse.Invoke(response, success, responseData);
            }
        }

        protected void HandleLogin(bool successful, object responseData)
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "Handling Login");
            userAccessVerified = false;

            if (successful)
            {
                currentActiveLogin = responseData as LoginResponseContent;

                OnLoginSuccess.Invoke();

                if (loginCheckModuleAccess)
                {
                    CheckModuleAccess(moduleID);
                }
            }
            else
            {
                FailureResponse failureData = responseData as FailureResponse;
                Debug.unityLogger.Log(LogType.Log, TAG, string.Format("Failed to log in.\nError: {0}", failureData.Message));
                OnLoginFailed.Invoke(responseData as FailureResponse);
            }
        }

        bool _RequestAuthorizationCode()
        {
            if (!webSocket.IsConnected())
            {
                ConnectWebsocket();
            }
            return webSocket.RequestAuthorizationCode();
        }

        public static bool GenerateOneTimeLoginForCurrentUser(Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            if (CurrentActiveLogin == null)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "No user logged in to generate code.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "No user logged in to generate code." });
                return false;
            }

            ApexAPIHandler.GenerateAssistedLogin(CurrentActiveLogin.Token, -1, success, failure);
            return true;
        }

        public static void GenerateOneTimeLoginForUser(int userId, Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            if (CurrentActiveLogin == null)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "No current user logged in.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "No current user logged in." });
                return;
            }

            if (userId < 0)
            {
                Debug.unityLogger.Log(LogType.Error, TAG, "User id is invalid.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "User id is invalid." });
                return;
            }

            ApexAPIHandler.GenerateAssistedLogin(CurrentActiveLogin.Token, userId, success, failure);
        }

        public static void GetUserMetricsForCurrentUsersOrg(int page, FilterParams filterParams, Action<UserMetricsResponse, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            if (CurrentActiveLogin == null)
            {
                Debug.LogError("[ApexSystem] No user logged in to get the user metrics for the current user org.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "No user logged in to retrieve org devices." });
                return;
            }
            
            ApexAPIHandler.GetUserMetricsForOrg(CurrentActiveLogin.Token, CurrentActiveLogin.OrgId, page, filterParams, success, failure);
        }

        public static void GetDevicesForOrg(int page, FilterParams filterParams, Action<HttpResponseMessage, OrgDevicesResponse> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            if (CurrentActiveLogin == null)
            {
                Debug.LogError("[ApexSystem] No user logged in to retrieve org devices.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "No user logged in to retrieve org devices." });
                return;
            }

            ApexAPIHandler.GetDevicesForOrg(CurrentActiveLogin.Token, CurrentActiveLogin.OrgId, page, filterParams, success, failure);
        }

        public static void GetSesssionHistory(int page, SessionFilters sessionFilters, FilterParams filterParams, Action<HttpResponseMessage, SessionHistoryResponse> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            if (CurrentActiveLogin == null)
            {
                Debug.LogError("[ApexSystem] No user logged in to retrieve session history.");
                failure?.Invoke(null, new FailureResponse { Error = "true", Message = "No user logged in to retrieve session history." });
                return;
            }

            ApexAPIHandler.GetSessionHistory(CurrentActiveLogin.Token, page, sessionFilters, filterParams, success, failure);
        }
    }
}
