using PixoVR.Apex.XAPI;
using System;
using System.Net.Http;

namespace PixoVR.Apex
{
    public class BaseAPIHandler
    {
        public BaseAPIHandler()
            : this(PlatformEndpoints.NorthAmerica_ProductionEnvironment) { }

        public BaseAPIHandler(string endpointUrl)
        {
        }

        public virtual void SetEndpoint(string endpointUrl)
        {
            throw new NotImplementedException();
        }

        public virtual void SetPlatformEndpoint(string endpointUrl)
        {
            throw new NotImplementedException();
        }

        public virtual async void Ping(Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void GenerateAssistedLogin(string authToken, int userId, Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void GetUserMetricsForOrg(string authToken, int orgID, int page, FilterParams filterParams, Action<UserMetricsResponse, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void GetDevicesForOrg(string authToken, int orgID, int page, FilterParams filterParams, Action<HttpResponseMessage, OrgDevicesResponse> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void GetSessionHistory(string authToken, int page, SessionFilters sessionFilters, FilterParams filterParams, Action<HttpResponseMessage, SessionHistoryResponse> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void LoginWithToken(string token, Action<HttpResponseMessage, ActiveUserInformation> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void Login(LoginData login, Action<HttpResponseMessage, ActiveUserInformation> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void GetUserData(string authToken, int userId)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void GetUserModules(string authToken, int userId)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void GetQuickIDAuthenticationUsers(string serialNumber)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void QuickIDLogin(QuickIDLoginData login, Action<HttpResponseMessage, ActiveUserInformation> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void GetModuleAccess(int moduleId, int userId, string serialNumber, Action<HttpResponseMessage, ActiveUserInformation> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void SendHeartbeat(string authToken, int sessionId, Action<HttpResponseMessage, object> success = null, Action<HttpResponseMessage, FailureResponse> failure = null)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void JoinSession(string authToken, JoinSessionData joinData, Action<HttpResponseMessage, JoinSessionResponse> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void CompleteSession(string authToken, CompleteSessionData completionData, Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void SendSessionEvent(string authToken, SessionEventData sessionEvent, Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }

        public virtual async void GetModuleList(string authToken, string platform)
        {
            throw new NotImplementedException();
            await System.Threading.Tasks.Task.FromResult(0);
        }
    }
}
