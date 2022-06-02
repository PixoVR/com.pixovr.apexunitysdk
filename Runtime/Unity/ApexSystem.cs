using System;
using System.Net.Http;
using UnityEngine;
using PixoVR.Apex.Events;
using PixoVR.Apex.XAPI;
using PixoVR.Apex.Utils;
using TinCan;

namespace PixoVR.Apex
{

    public class ApexSystem : ApexSingleton<ApexSystem>
    {
        public string ServerIP = SDK.ProductionEnvironmentEndpoint;
        public int ModuleID = 0;
        public string ModuleName = "Generic";
        public string ModuleVersion = "0.00.00";
        public string ScenarioID = "Generic";
        
        protected string deviceID;
        protected string deviceModel;
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

        private void Awake()
        {
            apexSDK = new SDK(ServerIP);
            apexSDK.OnAPIResponse += OnAPIResponse;

            DontDestroyOnLoad(gameObject);
        }

        // Start is called before the first frame update
        void Start()
        {
            deviceID = SystemInfo.deviceUniqueIdentifier;
            deviceModel = SystemInfo.deviceModel;
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

        public static bool JoinSession(string scenarioID = null)
        {
            return Instance._JoinSession(scenarioID);
        }

        public static bool CompleteSession(SessionData currentSessionData)
        {
            return Instance._CompleteSession(currentSessionData);
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

        protected bool _JoinSession(string scenarioID = null)
        {
            if (currentActiveLogin == null)
            {
                Debug.LogError("[ApexSystem] Cannot join session with no active login.");
                return false;
            }

            if(scenarioID != null)
            {
                ScenarioID = scenarioID;
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
            sessionActivity.id = string.Format("https://pixovr.com/xapi/objects/{0}/{1}", ModuleID, ScenarioID);

            Context sessionContext = new Context();
            sessionContext.registration = currentSessionID;
            sessionContext.revision = ModuleVersion;
            sessionContext.platform = deviceModel;

            string extensionString = string.Format("{{\"{0}\":{1}}}", ApexExtensionStrings.MODULE_ID, ModuleID);
            sessionContext.extensions = new Extensions(ApexUtils.ConvertStringToJObject(extensionString));

            sessionStatement.actor = sessionActor;
            sessionStatement.verb = sessionVerb;
            sessionStatement.target = sessionActivity;
            sessionStatement.context = sessionContext;

            JoinSessionData sessionData = new JoinSessionData();
            sessionData.DeviceId = deviceID;
            sessionData.IpAddress = clientIP;
            sessionData.ModuleId = ModuleID;
            sessionData.Uuid = currentSessionID.ToString();
            sessionData.EventType = ApexEventTypes.PIXOVR_SESSION_JOINED;
            sessionData.JsonData = sessionStatement;

            apexSDK.JoinSession(currentActiveLogin.Token, sessionData);

            return true;
        }

        protected bool _CompleteSession(SessionData currentSessionData)
        {
            if (currentActiveLogin == null)
            {
                Debug.LogError("[ApexSystem] Cannot complete session with no active login.");
                return false;
            }

            if (sessionInProgress == true)
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
            sessionActivity.id = string.Format("https://pixovr.com/xapi/objects/{0}/{1}", ModuleID, ScenarioID);

            // Create our context
            Context sessionContext = new Context();
            sessionContext.registration = currentSessionID;
            sessionContext.revision = ModuleVersion;
            sessionContext.platform = deviceModel;

            // Build an extension for the context
            string extensionString = string.Format("{{\"{0}\":{1}}}", ApexExtensionStrings.MODULE_ID, ModuleID);
            sessionContext.extensions = new Extensions(ApexUtils.ConvertStringToJObject(extensionString));

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

            // Create our statement and add the pieces
            Statement sessionStatement = new Statement();
            sessionStatement.actor = sessionActor;
            sessionStatement.verb = sessionVerb;
            sessionStatement.target = sessionActivity;
            sessionStatement.context = sessionContext;
            sessionStatement.result = sessionResult;

            CompleteSessionData sessionData = new CompleteSessionData();
            sessionData.DeviceId = deviceID;
            sessionData.ModuleId = ModuleID;
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
                            OnCompleteSessionFailed.Invoke(responseData as FailureResponse);
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
