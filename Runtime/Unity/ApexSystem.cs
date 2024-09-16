using System;
using System.Net.Http;
using UnityEngine;
using UnityEngine.XR;
using PixoVR.Apex.Events;
using PixoVR.Apex.XAPI;
using PixoVR.Apex.Utils;
using TinCan;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Collections.Generic;
#if MANAGE_XR
using MXR.SDK;
#endif

namespace PixoVR.Apex
{
    public delegate void PlatformResponse(ResponseType type, bool wasSuccessful, object responseData);

    [DefaultExecutionOrder(-50)]
    public class ApexSystem : ApexSingleton<ApexSystem>
    {
        private enum VersionParts : int
        {
            Major = 0,
            Minor,
            Patch
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
            set { }
        }

        public static string DeviceSerialNumber
        {
            get { return Instance.deviceSerialNumber; }
            set { }
        }

        public static string PassedLoginToken
        {
            get { return Instance.loginToken; }
            set { }
        }

        public static string OptionalData
        {
            get { return Instance.optionalParameter; }
            set { Instance.optionalParameter = value; }
        }

        public static string ReturnTarget
        {
            get { return Instance.returnTargetParameter; }
            set { Instance.returnTargetParameter = value; }
        }

        public static string TargetType
        {
            get { return Instance.targetTypeParameter; }
            set { Instance.targetTypeParameter = value; }
        }


        [SerializeField, EndpointDisplay]
        protected PlatformServer PlatformTargetServer;

        [SerializeField]
        protected string serverIP = "";

        [SerializeField]
        protected int moduleID = 0;
        [SerializeField]
        protected string moduleName = "Generic";
        [SerializeField]
        protected string moduleVersion = "0.00.00";
        [SerializeField]
        protected string scenarioID = "Generic";
        [SerializeField]
        public bool runSetupOnAwake = true;
        [SerializeField]
        public bool loginCheckModuleAccess = true;
        [SerializeField]
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

        protected string loginToken = "";
        protected string optionalParameter = "";
        protected string returnTargetParameter = "";
        protected string targetTypeParameter = "";

        protected LoginResponseContent currentActiveLogin = null;
        protected APIHandler apexAPIHandler;
        protected ApexWebsocket webSocket;
        protected Task<bool> socketConnectTask;
        protected Task socketDisconnectTask;

        public OnHttpResponseEvent OnPingSuccess = new OnHttpResponseEvent();
        public OnHttpResponseEvent OnPingFailed = new OnHttpResponseEvent();

        public OnModuleAccessSuccessEvent OnModuleAccessSuccess = new OnModuleAccessSuccessEvent();
        public OnApexFailureEvent OnModuleAccessFailed = new OnApexFailureEvent();

        public OnLoginSuccessEvent OnLoginSuccess = new OnLoginSuccessEvent();
        public OnApexFailureEvent OnLoginFailed = new OnApexFailureEvent();
        
        public OnGetUserSuccessEvent OnGetUserSuccess = new OnGetUserSuccessEvent();
        public OnApexFailureEvent OnGetUserFailed = new OnApexFailureEvent();

        public OnGetUserModulesSuccessEvent OnGetUserModulesSuccess = new OnGetUserModulesSuccessEvent();
        public OnApexFailureEvent OnGetUserModulesFailed = new OnApexFailureEvent();
        
        public OnHttpResponseEvent OnJoinSessionSuccess = new OnHttpResponseEvent();
        public OnApexFailureEvent OnJoinSessionFailed = new OnApexFailureEvent();
        
        public OnHttpResponseEvent OnCompleteSessionSuccess = new OnHttpResponseEvent();
        public OnApexFailureEvent OnCompleteSessionFailed = new OnApexFailureEvent();
        
        public OnHttpResponseEvent OnSendEventSuccess = new OnHttpResponseEvent();
        public OnApexFailureEvent OnSendEventFailed = new OnApexFailureEvent();

        public OnGetOrgModulesSuccessEvent OnGetOrganizationModulesSuccess = new OnGetOrgModulesSuccessEvent();
        public OnApexFailureEvent OnGetOrganizationModulesFailed = new OnApexFailureEvent();

        public PlatformResponse OnPlatformResponse = null;

        public OnAuthCodeReceived OnAuthorizationCodeReceived = new OnAuthCodeReceived();

        void Awake()
        {
            if(runSetupOnAwake)
            {
                Debug.Log("[ApexSystem] Running on awake!");
                SetupAPI();
            }

            DontDestroyOnLoad(gameObject);
#if MANAGE_XR
            InitMXRSDK();
#endif
        }

#if MANAGE_XR
        async void InitMXRSDK()
        {
            await MXRManager.InitAsync();
            MXRManager.System.OnDeviceStatusChange += OnDeviceStatusChanged;
            deviceSerialNumber = MXRManager.System.DeviceStatus.serial;
        }

        void OnDeviceStatusChanged(DeviceStatus newDeviceStatus)
        {
            deviceSerialNumber = newDeviceStatus.serial;
        }
#endif
        void SetupAPI()
        {
            Debug.Log("Mac Address: " + ApexUtils.GetMacAddress());
            if (serverIP.Length == 0)
            {
                serverIP = GetEndpointFromTarget(PlatformTargetServer);
            }

            apexAPIHandler = new APIHandler(serverIP);

            if(apexAPIHandler != null)
            {
                Debug.Log("[ApexSystem] Apex API Handler is not null!");
            }

            // TODO: Move to new plugin
            apexAPIHandler.SetWebEndpoint(GetWebEndpointFromPlatformTarget(PlatformTargetServer));
            apexAPIHandler.SetPlatformEndpoint(GetPlatformEndpointFromPlatformTarget(PlatformTargetServer));
            apexAPIHandler.OnAPIResponse += OnAPIResponse;

            if(webSocket != null)
            {
                if(webSocket.IsConnected())
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

            _ParsePassedData();
            loginToken = GetAuthenticationToken();
        }

        void _ExitApplication(string returnTarget, string returnTargetType)
        {
            Debug.Log("[ApexSystem] " + returnTarget + " " + returnTargetType);

            string parameters = "";

            if(CurrentActiveLogin != null)
            {
                parameters += "pixotoken=" + CurrentActiveLogin.Token;
            }

            if (optionalParameter.Length > 0)
            {
                if (parameters.Length > 0)
                    parameters += "&";
                parameters += "optional=" + optionalParameter;
            }

            if (returnTarget.Length > 0)
            {
                if (parameters.Length > 0)
                    parameters += "&";
                parameters += "returntarget=" + returnTarget;
            }

            if (returnTargetType.Length > 0)
            {
                if (parameters.Length > 0)
                    parameters += "&";
                parameters += "targettype=" + returnTargetType;
            }

            if (returnTargetParameter.Length > 0)
            {
                if (targetTypeParameter.Equals("url", StringComparison.OrdinalIgnoreCase))
                {
                    string returnURL = returnTargetParameter;
                    if (parameters.Length > 0)
                    {
                        returnURL += "?" + parameters;
                    }
                    Debug.Log("Custom Target: " + returnURL);
                    Application.OpenURL(returnURL);
                }
                else
                {
                    List<string> keys = new List<string>(), values = new List<string>();

                    if(CurrentActiveLogin != null)
                    {
                        keys.Add("pixotoken");
                        values.Add(CurrentActiveLogin.Token);
                    }

                    if (optionalParameter.Length > 0)
                    {
                        keys.Add("optional");
                        values.Add(optionalParameter);
                    }

                    if (returnTarget.Length > 0)
                    {
                        keys.Add("returntarget");
                        values.Add(returnTarget);
                    }

                    if (returnTargetType.Length > 0)
                    {
                        keys.Add("targettype");
                        values.Add(returnTargetType);
                    }

                    PixoAndroidUtils.LaunchApp(returnTargetParameter, keys.ToArray(), values.ToArray());
                }
            }

            string url = GetPlatformEndpointFromPlatformTarget(PlatformTargetServer);
            if (url.Contains("apexsa.") || url.Contains("saudi."))
            {
                string returnUrl = "pixovr://com.PixoVR.SA_TrainingAcademy";
                if(parameters.Length > 0)
                {
                    returnUrl += "?" + parameters;
                }
                Debug.Log("Training Hub: " + returnUrl);
                Application.OpenURL(returnUrl);
            }
            else
            {
                string returnUrl = "pixovr://com.PixoVR.SA_TrainingAcademy?" + parameters;
                if (parameters.Length > 0)
                {
                    returnUrl += "?" + parameters;
                }
                Debug.Log("Hub App: " + returnUrl);
                Application.OpenURL(returnUrl);
            }
        }

        string GetEndpointFromTarget(PlatformServer target)
        {
            return target.ToUrlString();
        }

        string GetWebEndpointFromPlatformTarget(PlatformServer target)
        {
            int targetValue = (int)target;
            WebPlatformServer webTarget = (WebPlatformServer)targetValue;

            return webTarget.ToUrlString();
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
                Debug.LogAssertion(moduleVersion + " is an invalid module version.");
            }
            deviceID = SystemInfo.deviceUniqueIdentifier;
            deviceModel = SystemInfo.deviceModel;
            platform = XRSettings.loadedDeviceName.Length > 0 ? XRSettings.loadedDeviceName : Application.platform.ToString();
            clientIP = Utils.ApexUtils.GetLocalIP();
        }

        private void FixedUpdate()
        {
            if(webSocket != null)
            {
                webSocket.Update();
            }

            if (sessionInProgress)
            {
                heartbeatTimer -= Time.fixedDeltaTime;

                if(heartbeatTimer <= 0.0f)
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
            Debug.Log("Websocket connected successfully.");
        }

        void OnWebSocketConnectFailed(string reason)
        {
            Debug.LogError("Websocket failed to connect with error: " + reason);
        }

        void OnWebSocketReceive(string data)
        {
            Debug.Log("Websocket received: " + data);
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
                Debug.Log(ex.Message);
            }
        }

        void OnWebSocketClosed(System.Net.WebSockets.WebSocketCloseStatus reason)
        {
            Debug.Log("Websocket closed with reason: " + reason);
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

        [Obsolete("ReturnToHub has been deprecated, please use ExitApplication.", true)]
        public static void ReturnToHub()
        {
            Instance._ReturnToHub();
        }

        public static void ExitApplication(string returnTarget, string returnTargetType)
        {
            Instance._ExitApplication(returnTarget, returnTargetType);
        }

        public static string GetAuthenticationToken()
        {
            return Instance._GetAuthenticationToken();
        }

        public static bool RequestAuthorizationCode()
        {
            return Instance._RequestAuthorizationCode();
        }

        public static void ChangePlatformServer(PlatformServer newServer)
        {
            Instance._ChangePlatformServer(newServer);
        }

        public static void Ping()
        {
            Instance._Ping();
        }

        public static bool LoginWithToken()
        {
            return LoginWithToken(Instance.loginToken);
        }

        public static bool LoginWithToken(string token)
        {
            return Instance._LoginWithToken(token);
        }

        public static bool Login(LoginData login)
        {
            return Instance._Login(login);
        }

        public static bool Login(string username, string password)
        {
            return Instance._Login(username, password);
        }

        public static bool CheckModuleAccess(int targetModuleID = -1)
        {
            return Instance._CheckModuleAccess(targetModuleID);
        }

        public static bool JoinSession(string scenarioID = null, Extension contextExtension = null)
        {
            return Instance._JoinSession(scenarioID, contextExtension);
        }

        public static bool CompleteSession(SessionData currentSessionData, Extension contextExtension = null, Extension resultExtension = null)
        {
            return Instance._CompleteSession(currentSessionData, contextExtension, resultExtension);
        }

        public static bool SendSimpleSessionEvent(string action, string targetObject, Extension contextExtension)
        {
            return Instance._SendSimpleSessionEvent(action, targetObject, contextExtension);
        }

        public static bool SendSessionEvent(Statement eventStatement)
        {
            return Instance._SendSessionEvent(eventStatement);
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

        public static bool GetModulesList()
        {
            return Instance._GetModuleList();
        }

        protected void _ChangePlatformServer(PlatformServer newServer)
        {
            PlatformTargetServer = newServer;

            SetupAPI();
        }

        protected void _Ping()
        {
            apexAPIHandler.Ping();
        }

        public bool _LoginWithToken(string token)
        {
            if(token.Length <= 0)
            {
                return false;
            }

            apexAPIHandler.LoginWithToken(token);

            return true;
        }

        protected bool _Login(LoginData login)
        {
            Debug.Log("[ApexSystem] _Login called.");
            if (login.Login.Length <= 0)
            {
                Debug.Log("[Login] No user name.");
                return false;
            }

            if(login.Password.Length <= 0)
            {
                Debug.Log("[Login] No password.");
                login.Password = "<empty>";
            }

            if(apexAPIHandler == null)
            {
                Debug.Log("[ApexSystem] API Handler is null.");
            }

            apexAPIHandler.Login(login);

            Debug.Log("[ApexSystem] Login called.");

            return true;
        }

        protected bool _Login(string username, string password)
        {
            return _Login(new LoginData(username, password));
        }

        public void _ParsePassedData()
        {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");

            string urlData = intent.Call<string>("getDataString");

            Debug.Log("[ApexSystem] Parsed Passed Data.");
            if(urlData != null && urlData.Length > 0)
            {
                Debug.Log("[ApexSystem] Parse from URL.");
                _ParseUrlData(urlData);
            }
            else
            {
                Debug.Log("[ApexSystem] Parsing from extras.");
                optionalParameter = intent.Call<string>("getStringExtra", "optional");
                returnTargetParameter = intent.Call<string>("getStringExtra", "returntarget");
                targetTypeParameter = intent.Call<string>("getStringExtra", "targettype");
            }
        }

        public void _ParseUrlData(string urlString)
        {
            string urlData = urlString.Substring(urlString.IndexOf('?') + 1);


            if (urlData.Length <= 0)
                return;

            string[] dataArray = urlData.Split('&');

            if (dataArray.Length <= 0)
                return;

            foreach(string dataElement in dataArray)
            {
                string[] dataParts = dataElement.Split('=');

                if (dataParts.Length <= 1)
                    continue;

                if(dataParts[0].Equals("optional", StringComparison.OrdinalIgnoreCase))
                {
                    optionalParameter = dataParts[1];
                }

                if (dataParts[0].Equals("returntarget", StringComparison.OrdinalIgnoreCase))
                {
                    returnTargetParameter = dataParts[1];
                }

                if (dataParts[0].Equals("targettype", StringComparison.OrdinalIgnoreCase))
                {
                    targetTypeParameter = dataParts[1];
                }
            }
        }

        public string _GetAuthenticationToken()
        {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");

            string ExtraString = intent.Call<string>("getStringExtra", "pixotoken");

            return ExtraString;
        }

        public bool _CheckModuleAccess(int targetModuleID = -1)
        {
            Debug.Log("[ApexSystem] _CheckModuleAccess called.");

            if (currentActiveLogin == null)
            {
                Debug.LogError("[ApexSystem] Cannot check user's module access with no active login.");
                return false;
            }

            if (targetModuleID <= -1)
            {
                targetModuleID = moduleID;
            }

            apexAPIHandler.GetModuleAccess(targetModuleID, currentActiveLogin.ID, deviceSerialNumber);

            return true;
        }

        protected bool _JoinSession(string newScenarioID = null, Extension contextExtension = null)
        {
            if (currentActiveLogin == null)
            {
                Debug.LogError("[ApexSystem] Cannot join session with no active login.");
                return false;
            }

            if(userAccessVerified == false)
            {
                return false;
            }

            if (newScenarioID != null)
            {
                scenarioID = newScenarioID;
            }

            if (sessionInProgress == true)
            {
                Debug.LogError("[ApexSystem] Session is already in progress." +
                    " The previous session didn't complete or a new session was started during an active session.");
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

            apexAPIHandler.JoinSession(currentActiveLogin.Token, sessionData);

            return true;
        }

        protected bool _SendSimpleSessionEvent(string verbName, string targetObject, Extension contextExtension)
        {
            if (userAccessVerified == false)
                return false;
            
            if (verbName == null)
                return false;

            if (verbName.Length == 0)
                return false;


            Statement sessionStatement = new Statement();
            Agent sessionActor = new Agent();
            sessionActor.mbox = currentActiveLogin.Email;

            Verb sessionVerb = new Verb();
            sessionVerb.id = new Uri("https://pixovr.com/xapi/verbs/" + verbName.Replace(' ', '_').ToLower());
            sessionVerb.display = new LanguageMap();
            sessionVerb.display.Add("en", verbName);

            Activity sessionActivity = new Activity();
            sessionActivity.id = string.Format("https://pixovr.com/xapi/objects/{0}/{1}/{2}", moduleID, scenarioID, targetObject.Replace(' ', '_').ToLower());

            Context sessionContext = new Context();
            sessionContext.registration = currentSessionID;
            sessionContext.revision = moduleVersion;
            sessionContext.platform = platform;

            sessionContext.extensions = AppendStandardContextExtension(contextExtension);

            sessionStatement.actor = sessionActor;
            sessionStatement.verb = sessionVerb;
            sessionStatement.target = sessionActivity;
            sessionStatement.context = sessionContext;

            SessionEventData sessionEvent = new SessionEventData();
            sessionEvent.DeviceId = deviceID;
            sessionEvent.ModuleId = ModuleID;
            sessionEvent.Uuid = currentSessionID.ToString();
            sessionEvent.EventType = ApexEventTypes.PIXOVR_SESSION_EVENT;
            sessionEvent.JsonData = sessionStatement;

            apexAPIHandler.SendSessionEvent(currentActiveLogin.Token, sessionEvent);

            return true;
        }

        protected bool _SendSessionEvent(Statement eventStatement)
        {
            if (userAccessVerified == false)
            {
                return false;
            }

            if (currentActiveLogin == null)
            {
                Debug.LogError("[ApexSystem] Cannot send a session event with no active login.");
                return false;
            }

            if (sessionInProgress == false)
            {
                Debug.LogError("[ApexSystem] No session in progress to send event for.");
                return false;
            }

            if(eventStatement == null)
            {
                Debug.LogError("[ApexSystem] No event data to send.");
                return false;
            }

            if(eventStatement.actor != null)
            {
                Debug.LogWarning("[ApexSystem] Actor data should not be filled out.");
            }

            if(eventStatement.verb == null)
            {
                Debug.LogError("[ApexSystem] Verb missing from eventStatement.");
                return false;
            }

            if(eventStatement.verb.id == null)
            {
                Debug.LogError("[ApexSystem] verb.id missing from eventStatement.");
                return false;
            }

            if(eventStatement.target == null)
            {
                Debug.LogError("[ApexSystem] Object (target) missing from eventStatement.");
                return false;
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

            apexAPIHandler.SendSessionEvent(currentActiveLogin.Token, sessionEvent);

            return true;
        }

        protected bool _CompleteSession(SessionData currentSessionData, Extension contextExtension, Extension resultExtension)
        {
            if (userAccessVerified == false)
            {
                return false;
            }

            if (currentActiveLogin == null)
            {
                Debug.LogError("[ApexSystem] Cannot complete session with no active login.");
                return false;
            }

            if (sessionInProgress == false)
            {
                Debug.LogError("[ApexSystem] No session in progress to complete.");
                return false;
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
            sessionResult.score.scaled = DetermineScaledScore(currentSessionData.ScaledScore, currentSessionData.Score, currentSessionData.MaximumScore);
            sessionResult.duration = TimeSpan.FromSeconds(currentSessionData.Duration);
            if(resultExtension != null)
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
            sessionData.ScoreScaled = DetermineScaledScore(currentSessionData.ScaledScore, currentSessionData.Score, currentSessionData.MaximumScore);

            apexAPIHandler.CompleteSession(currentActiveLogin.Token, sessionData);

            return true;
        }

        protected bool _SendHeartbeat()
        {
            Debug.Log("Sending heartbeat...");
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

        protected bool _GetModuleList()
        {
            if (currentActiveLogin == null)
                return false;

            apexAPIHandler.GetModuleList(currentActiveLogin.Token, "htcfocus3");
            return true;
        }

        private float DetermineScaledScore(float scaledScore, float score, float maxScore)
        {
            float determinedScaledScore = scaledScore;
            
            if(scaledScore < Mathf.Epsilon && score >= Mathf.Epsilon)
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
            contextExtension.AddSimple("sdk_version", "unity-" + Utils.ApexUtils.GetSDKVersion());

            return new Extensions(contextExtension.ToJObject());
        }

        protected void OnAPIResponse(ResponseType response, HttpResponseMessage message, object responseData)
        {
            Debug.Log("[ApexSystem] On API Response");
            bool success = message.IsSuccessStatusCode;
            if(responseData is FailureResponse)
            {
                success = success && (responseData is IFailure) && (!(responseData as FailureResponse).HasErrored());
            }

            switch (response)
            {
                case ResponseType.RT_PING:
                    {
                        if(success)
                        {
                            Debug.Log("[ApexSystem] Ping successful.");
                            OnPingSuccess.Invoke(message);
                        }
                        else
                        {
                            Debug.Log("[ApexSystem] Ping failed.");
                            OnPingFailed.Invoke(message);
                        }
                        break;
                    }
                case ResponseType.RT_LOGIN:
                    {
                        Debug.Log("[ApexSystem] Calling to handle login.");
                        HandleLogin(success, responseData);
                        break;
                    }
                case ResponseType.RT_GET_USER:
                    {
                        if(success)
                        {
                            OnGetUserSuccess.Invoke(responseData as GetUserResponseContent);
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.Log(string.Format("[ApexSystem] Failed to get user.\nError: {0}", failureData.Message));
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
                            Debug.Log(string.Format("[ApexSystem] Failed to get user.\nError: {0}", failureData.Message));
                            OnGetUserFailed.Invoke(responseData as FailureResponse);
                        }
                        break;
                    }
                case ResponseType.RT_SESSION_JOINED:
                    {
                        if(success)
                        {
                            JoinSessionResponse joinSessionResponse = responseData as JoinSessionResponse;
                            Debug.Log(string.Format("[ApexSystem] Session Id is {0}.", joinSessionResponse.SessionId));
                            heartbeatSessionID = joinSessionResponse.SessionId;
                            sessionInProgress = true;
                            OnJoinSessionSuccess.Invoke(message);
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.Log(string.Format("[ApexSystem] Failed to join session.\nError: {0}", failureData.Message));
                            currentSessionID = Guid.Empty;
                            sessionInProgress = false;
                            OnJoinSessionFailed.Invoke(responseData as FailureResponse);
                        }
                        break;
                    }
                case ResponseType.RT_SESSION_COMPLETE:
                    {
                        if (success)
                        {
                            sessionInProgress = false;
                            currentSessionID = Guid.Empty;
                            OnCompleteSessionSuccess.Invoke(message);
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.Log(string.Format("[ApexSystem] Failed to complete session.\nError: {0}", failureData.Message));
                            OnCompleteSessionFailed.Invoke(responseData as FailureResponse);
                        }
                        break;
                    }
                case ResponseType.RT_SESSION_EVENT:
                    {
                        if (success)
                        {
                            Debug.Log("[ApexSystem] Session event sent.");
                            OnSendEventSuccess.Invoke(message);
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.Log(string.Format("[ApexSystem] Failed to send session event.\nError: {0}", failureData.Message));
                            OnSendEventFailed.Invoke(responseData as FailureResponse);
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
                                OnModuleAccessFailed.Invoke(new FailureResponse()
                                {
                                    Error = "True",
                                    HttpCode = "401",
                                    Message = "User does not have access to module",
                                });
                            }
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.Log(string.Format("[ApexSystem] Failed to get users module access data.\nError: {0}", failureData.Message));

                            OnModuleAccessFailed.Invoke(responseData as FailureResponse);
                        }
                        break;
                    }
                case ResponseType.RT_GET_MODULES_LIST:
                    {
                        if(success)
                        {
                            OnGetOrganizationModulesSuccess.Invoke(responseData as List<OrgModule>);
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.Log(string.Format("[ApexSystem] Failed to get org modules.\nError: {0}", failureData.Message));

                            OnGetOrganizationModulesFailed.Invoke(responseData as FailureResponse);
                        }

                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            if(OnPlatformResponse != null)
            {
                OnPlatformResponse.Invoke(response, success, responseData);
            }
        }

        protected void HandleLogin(bool successful, object responseData)
        {
            Debug.Log("[ApexSystem] Handling Login");
            userAccessVerified = false;

            if (successful)
            {
                currentActiveLogin = responseData as LoginResponseContent;

                OnLoginSuccess.Invoke();

                if (loginCheckModuleAccess)
                {
                    apexAPIHandler.GetModuleAccess(moduleID, currentActiveLogin.ID, deviceSerialNumber);
                }
            }
            else
            {
                FailureResponse failureData = responseData as FailureResponse;
                Debug.Log(string.Format("[ApexSystem] Failed to log in.\nError: {0}", failureData.Message));
                OnLoginFailed.Invoke(responseData as FailureResponse);
            }
        }

        void _ReturnToHub()
        {
            var token = currentActiveLogin.Token;

            if(serverIP.Contains("apexsa.", StringComparison.CurrentCultureIgnoreCase) || serverIP.Contains("saudi.", StringComparison.CurrentCultureIgnoreCase))
            {
                Debug.Log($"pixovr://com.PixoVR.SA_TrainingAcademy?pixotoken={token}");
                Application.OpenURL($"pixovr://com.PixoVR.SA_TrainingAcademy?pixotoken={token}");
            }
            else
            {
                Debug.Log($"pixovr://com.PixoVR.PixoHub?pixotoken={token}");
                Application.OpenURL($"pixovr://com.PixoVR.PixoHub?pixotoken={token}");
            }
        }

        bool _RequestAuthorizationCode()
        {
            if(!webSocket.IsConnected())
            {
                ConnectWebsocket();
            }
            return webSocket.RequestAuthorizationCode();
        }

        // TODO: Move to new plugin
        public static bool GenerateOneTimeLoginForCurrentUser()
        {
            if (Instance.currentActiveLogin == null)
                return false;

            return Instance._GenerateOneTimeLoginForUser(Instance.currentActiveLogin.ID);
        }

        // TODO: Move to new plugin
        bool _GenerateOneTimeLoginForCurrentUser()
        {
            if (currentActiveLogin == null)
            {
                Debug.LogError("[ApexSystem] No current user logged in.");
                return false;
            }

            return _GenerateOneTimeLoginForUser(currentActiveLogin.ID);
        }

        // TODO: Move to new plugin
        bool _GenerateOneTimeLoginForUser(int userId)
        {
            if (currentActiveLogin == null)
            {
                Debug.LogError("[ApexSystem] No user logged in to generate code.");
                return false;
            }

            apexAPIHandler.GenerateAssistedLogin(currentActiveLogin.Token, userId);
            return true;
        }
    }
}
