using System;
using System.Net.Http;
using UnityEngine;
using UnityEngine.XR;
using PixoVR.Apex.Events;
using PixoVR.Apex.XAPI;
using TinCan;

namespace PixoVR.Apex
{

    public class ApexSystem : ApexSingleton<ApexSystem>
    {
        public static string ServerIP
        {
            get { return Instance.serverIP; }
            set { }
        }

        public static int ModuleID
        {
            get { return Instance.moduleID; }
            set { }
        }

        public static string ModuleName
        {
            get { return Instance.moduleName; }
            set { }
        }

        public static string ModuleVersion
        {
            get { return Instance.moduleVersion; }
            set { }
        }

        public static string ScenarioID
        {
            get { return Instance.scenarioID; }
            set { }
        }

        [SerializeField]
        protected string serverIP = SDK.ProductionEnvironmentEndpoint;
        [SerializeField]
        protected int moduleID = 0;
        [SerializeField]
        protected string moduleName = "Generic";
        [SerializeField]
        protected string moduleVersion = "0.00.00";
        [SerializeField]
        protected string scenarioID = "Generic";
        
        protected string deviceID;
        protected string deviceModel;
        protected string platform;
        protected string clientIP;
        protected Guid currentSessionID;
        protected bool sessionInProgress;

        protected LoginResponseContent currentActiveLogin = null;
        protected SDK apexSDK;

        public OnHttpResponseEvent OnPingSuccess = new OnHttpResponseEvent();
        public OnHttpResponseEvent OnPingFailed = new OnHttpResponseEvent();
        public OnLoginSuccessEvent OnLoginSuccess = new OnLoginSuccessEvent();
        public OnApexFailureEvent OnLoginFailed = new OnApexFailureEvent();
        public OnGetUserSuccessEvent OnGetUserSuccess = new OnGetUserSuccessEvent();
        public OnApexFailureEvent OnGetUserFailed = new OnApexFailureEvent();
        public OnHttpResponseEvent OnJoinSessionSuccess = new OnHttpResponseEvent();
        public OnApexFailureEvent OnJoinSessionFailed = new OnApexFailureEvent();
        public OnHttpResponseEvent OnCompleteSessionSuccess = new OnHttpResponseEvent();
        public OnApexFailureEvent OnCompleteSessionFailed = new OnApexFailureEvent();
        public OnHttpResponseEvent OnSendEventSuccess = new OnHttpResponseEvent();
        public OnApexFailureEvent OnSendEventFailed = new OnApexFailureEvent();

        private void Awake()
        {
            apexSDK = new SDK(serverIP);
            apexSDK.OnAPIResponse += OnAPIResponse;

            DontDestroyOnLoad(gameObject);
        }

        // Start is called before the first frame update
        void Start()
        {
            deviceID = SystemInfo.deviceUniqueIdentifier;
            deviceModel = SystemInfo.deviceModel;
            platform = XRSettings.loadedDeviceName.Length > 0 ? XRSettings.loadedDeviceName : Application.platform.ToString();
            clientIP = Utils.ApexUtils.GetLocalIP();
        }

        public static void Ping()
        {
            Instance._Ping();
        }

        public static bool Login(LoginData login)
        {
            return Instance._Login(login);
        }

        public static bool Login(string username, string password)
        {
            return Instance._Login(username, password);
        }

        public static bool JoinSession(string scenarioID = null, Extension contextExtension = null)
        {
            return Instance._JoinSession(scenarioID, contextExtension);
        }

        public static bool CompleteSession(SessionData currentSessionData, Extension contextExtension = null, Extension resultExtension = null)
        {
            return Instance._CompleteSession(currentSessionData, contextExtension, resultExtension);
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

        protected void _Ping()
        {
            apexSDK.Ping();
        }

        protected bool _Login(LoginData login)
        {
            if (login.Password.Length <= 0 || login.Login.Length <= 0)
            {
                return false;
            }

            apexSDK.Login(login);
            return true;
        }

        protected bool _Login(string username, string password)
        {
            return _Login(new LoginData(username, password));
        }

        protected bool _JoinSession(string newScenarioID = null, Extension contextExtension = null)
        {
            if (currentActiveLogin == null)
            {
                Debug.LogError("[ApexSystem] Cannot join session with no active login.");
                return false;
            }

            if(newScenarioID != null)
            {
                scenarioID = newScenarioID;
            }

            if(sessionInProgress == true)
            {
                Debug.LogError("[ApexSystem] Session is already in progress." +
                    " The previous session didn't complete or a new session was started during an active session.");
            }

            currentSessionID = Guid.NewGuid();

            // Finish filling this out
            Statement sessionStatement = new Statement();
            Agent sessionActor = new Agent();
            sessionActor.mbox = currentActiveLogin.Email;

            Verb sessionVerb = new Verb();
            sessionVerb.id = ApexVerbs.JOINED_SESSION;
            sessionVerb.display = new LanguageMap();
            sessionVerb.display.Add("en","Joined Session");

            Activity sessionActivity = new Activity();
            sessionActivity.id = string.Format("https://pixovr.com/xapi/objects/{0}/{1}", moduleID, scenarioID);

            Context sessionContext = new Context();
            sessionContext.registration = currentSessionID;
            sessionContext.revision = moduleVersion;
            sessionContext.platform = platform;

            Extension currentContextExtension;
            if (contextExtension != null)
            {
                currentContextExtension = contextExtension;
            }
            else
            {
                currentContextExtension = new Extension();
            }

            currentContextExtension.Add(ApexExtensionStrings.MODULE_ID, moduleID.ToString());
            currentContextExtension.AddSimple("device_id", deviceID);
            currentContextExtension.AddSimple("device_model", deviceModel);
            sessionContext.extensions = new Extensions(currentContextExtension.ToJObject());

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

            apexSDK.JoinSession(currentActiveLogin.Token, sessionData);

            return true;
        }

        protected bool _SendSessionEvent(Statement eventStatement)
        {
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

            Extension contextExtension;
            if (eventStatement.context.extensions != null)
            {
                contextExtension = new Extension(eventStatement.context.extensions.ToJObject());
            }
            else
            {
                contextExtension = new Extension();
            }
            contextExtension.AddSimple("device_id", deviceID);
            contextExtension.AddSimple("device_model", deviceModel);

            SessionEventData sessionEvent = new SessionEventData();
            sessionEvent.DeviceId = deviceID;
            sessionEvent.ModuleId = ModuleID;
            sessionEvent.Uuid = currentSessionID.ToString();
            sessionEvent.EventType = ApexEventTypes.PIXOVR_SESSION_EVENT;
            sessionEvent.JsonData = eventStatement;

            apexSDK.SendSessionEvent(currentActiveLogin.Token, sessionEvent);

            return true;
        }

        protected bool _CompleteSession(SessionData currentSessionData, Extension contextExtension, Extension resultExtension)
        {
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

            // Build an extension for the context
            Extension currentContextExtension;
            if (contextExtension != null)
            {
                currentContextExtension = contextExtension;
            }
            else
            {
                currentContextExtension = new Extension();
            }

            currentContextExtension.Add(ApexExtensionStrings.MODULE_ID, moduleID.ToString());
            currentContextExtension.AddSimple("device_id", deviceID);
            currentContextExtension.AddSimple("device_model", deviceModel);
            sessionContext.extensions = new Extensions(currentContextExtension.ToJObject());

            // Create our results
            Result sessionResult = new Result();
            sessionResult.completion = currentSessionData.Complete;
            sessionResult.success = currentSessionData.Success;
            // Add score to the results
            sessionResult.score = new Score();
            sessionResult.score.min = currentSessionData.MinimumScore;
            sessionResult.score.max = currentSessionData.MaximumScore;
            sessionResult.score.raw = currentSessionData.Score;
            sessionResult.score.scaled = currentSessionData.ScaledScore;
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
            sessionData.ScoreScaled = currentSessionData.ScaledScore;

            apexSDK.CompleteSession(currentActiveLogin.Token, sessionData);

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

            apexSDK.GetUserData(currentActiveLogin.Token, userId);
            return true;
        }

        protected void OnAPIResponse(ResponseType response, HttpResponseMessage message, object responseData)
        {
            bool success = message.IsSuccessStatusCode && !(responseData is IFailure);

            switch(response)
            {
                case ResponseType.RT_PING:
                    {
                        if(success)
                        {
                            Debug.Log("Yay! Ping success!");
                            OnPingSuccess.Invoke(message);
                        }
                        else
                        {
                            Debug.Log("Boo! No ping succcess!");
                            OnPingFailed.Invoke(message);
                        }
                        break;
                    }
                case ResponseType.RT_LOGIN:
                    {
                        if (success)
                        {
                            currentActiveLogin = responseData as LoginResponseContent;
                            OnLoginSuccess.Invoke(currentActiveLogin);
                        }
                        else
                        {
                            FailureResponse failureData = responseData as FailureResponse;
                            Debug.Log(string.Format("[ApexSystem] Failed to log in.\nError: {0}", failureData.Message));
                            OnLoginFailed.Invoke(responseData as FailureResponse);
                        }
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
                case ResponseType.RT_SESSION_JOINED:
                    {
                        if(success)
                        {
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
                default:
                    {
                        break;
                    }
            }
        }
    }
}
