using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net.Http.Headers;
using UnityEngine;
using PixoVR.Apex.XAPI;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace PixoVR.Apex
{
    public enum ResponseType
    {
        RT_NONE = 0,
        RT_FAILED_RESPONSE,
        RT_PING,
        RT_LOGIN,
        RT_GET_USER,
        RT_SESSION_JOINED,
        RT_SESSION_COMPLETE,
        RT_SESSION_EVENT,
        RT_GET_USER_ACCESS,
        RT_GET_USER_MODULES,
        RT_GET_MODULES_LIST,
        RT_GEN_AUTH_LOGIN,
        RT_HEARTBEAT
    }

    public class APIHandler
    {
        public delegate void APIResponse(ResponseType type, HttpResponseMessage message, object responseData);
        public APIResponse OnAPIResponse;

        protected string URL = "";
        protected HttpClient handlingClient = null;

        // Move to a separate plugin
        protected string webURL = "";
        protected HttpClient webHandlingClient = null;

        // Need to migrate to this in the future
        protected string apiURL = "";
        protected HttpClient apiHandlingClient = null;


        public APIHandler() : this(PlatformEndpoints.NorthAmerica_ProductionEnvironment)
        {
        }

        public APIHandler(string endpointUrl)
        {
            handlingClient = new HttpClient();
            SetEndpoint(endpointUrl);

            webHandlingClient = new HttpClient();
            apiHandlingClient = new HttpClient();
        }

        HttpResponseMessage HandleException(Exception exception)
        {
            Debug.LogWarning("Exception has occurred: " + exception.Message);
            HttpResponseMessage badRequestResponse = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            OnAPIResponse.Invoke(ResponseType.RT_FAILED_RESPONSE, badRequestResponse, null);
            return badRequestResponse;
        }

        public void SetEndpoint(string endpointUrl)
        {
            if (!endpointUrl.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                if (endpointUrl.StartsWith("http:", StringComparison.InvariantCultureIgnoreCase))
                {
                    Debug.LogWarning("Endpoint must be a secured http endpoint.");
                    Regex expression = new Regex(Regex.Escape("http"));
                    endpointUrl = expression.Replace(endpointUrl, "https", 1);
                }
                else
                {
                    endpointUrl.Insert(0, "https://");
                }
            }

            URL = endpointUrl;
            handlingClient.BaseAddress = new Uri(URL);
        }

        public void SetWebEndpoint(string endpointUrl)
        {
            if (!endpointUrl.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                if (endpointUrl.StartsWith("http:", StringComparison.InvariantCultureIgnoreCase))
                {
                    Debug.LogWarning("Endpoint must be a secured http endpoint.");
                    Regex expression = new Regex(Regex.Escape("http"));
                    endpointUrl = expression.Replace(endpointUrl, "https", 1);
                }
                else
                {
                    endpointUrl.Insert(0, "https://");
                }
            }

            webURL = endpointUrl;
            webHandlingClient.BaseAddress = new Uri(webURL);
        }

        public void SetPlatformEndpoint(string endpointUrl)
        {
            if (!endpointUrl.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                if (endpointUrl.StartsWith("http:", StringComparison.InvariantCultureIgnoreCase))
                {
                    Debug.LogWarning("Endpoint must be a secured http endpoint.");
                    Regex expression = new Regex(Regex.Escape("http"));
                    endpointUrl = expression.Replace(endpointUrl, "https", 1);
                }
                else
                {
                    endpointUrl.Insert(0, "https://");
                }
            }

            apiURL = endpointUrl;
            apiHandlingClient.BaseAddress = new Uri(apiURL);
        }

        public async void Ping()
        {
            handlingClient.DefaultRequestHeaders.Clear();
            handlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            try
            {
                response = await handlingClient.GetAsync("/ping");
            }
            catch (Exception ex)
            {
                response = HandleException(ex);
            }

            OnAPIResponse.Invoke(ResponseType.RT_PING, response, null);
        }

        public async void GenerateAssistedLogin(string authToken, int userId)
        {
            webHandlingClient.DefaultRequestHeaders.Clear();
            webHandlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            webHandlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            webHandlingClient.DefaultRequestHeaders.Add("x-access-token", authToken);

            HttpResponseMessage response = await webHandlingClient.GetAsync(string.Format("api/user/{0}/assisted-login", userId));
            string body = await response.Content.ReadAsStringAsync();
            Debug.Log(body);
            object responseContent = JsonConvert.DeserializeObject<GeneratedAssistedLogin>(body);
            GeneratedAssistedLogin assistedLogin = responseContent as GeneratedAssistedLogin;
            if ((responseContent as GeneratedAssistedLogin).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            }

            OnAPIResponse.Invoke(ResponseType.RT_GEN_AUTH_LOGIN, response, responseContent);
        }

        public async void LoginWithToken(string token)
        {
            apiHandlingClient.DefaultRequestHeaders.Clear();
            apiHandlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            apiHandlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await apiHandlingClient.GetAsync("/v2/auth/validate-signature");
            string body = await response.Content.ReadAsStringAsync();
            object responseContent = JsonConvert.DeserializeObject<UserLoginResponseContent>(body);
            if ((responseContent as UserLoginResponseContent).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            }

            object loginResponseContent = (responseContent as UserLoginResponseContent).User;

            OnAPIResponse.Invoke(ResponseType.RT_LOGIN, response, loginResponseContent);

            //// Now we build our fun request here!
            //UVaRestRequestJSON* Request = VaRestSubsystem->ConstructVaRestRequestExt(EVaRestRequestVerb::POST, EVaRestRequestContentType::json);
            //FString RequestURL = URL + "/v2/auth/validate-signature";
            //Request->SetURL(RequestURL);
            //Request->SetHeader("Authorization: Bearer", LaunchToken);
            //
            //Request->OnStaticRequestComplete.AddLambda([&](UVaRestRequestJSON * Request)-> void { OnLoginComplete(Request); });
            //Request->OnStaticRequestFail.AddLambda([&](UVaRestRequestJSON * Request)-> void { OnLoginFail(Request); });
            //
            //Request->ExecuteProcessRequest();
        }

        public async void Login(LoginData login)
        {
            handlingClient.DefaultRequestHeaders.Clear();

            HttpContent loginRequestContent = new StringContent(JsonUtility.ToJson(login));
            loginRequestContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

            HttpResponseMessage response = await handlingClient.PostAsync("/login", loginRequestContent);
            string body = await response.Content.ReadAsStringAsync();
            object responseContent = JsonConvert.DeserializeObject<LoginResponseContent>(body);
            if ((responseContent as LoginResponseContent).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            }

            OnAPIResponse.Invoke(ResponseType.RT_LOGIN, response, responseContent);
        }

        public async void GetUserData(string authToken, int userId)
        {
            handlingClient.DefaultRequestHeaders.Clear();
            handlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            handlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await handlingClient.GetAsync(string.Format("/user/{0}", userId));
            string body = await response.Content.ReadAsStringAsync();
            object responseContent = JsonConvert.DeserializeObject<GetUserResponseContent>(body);
            GetUserResponseContent userInfo = responseContent as GetUserResponseContent;
            if ((responseContent as GetUserResponseContent).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            }

            OnAPIResponse.Invoke(ResponseType.RT_GET_USER, response, responseContent);
        }

        public async void GetUserModules(string authToken, int userId)
        {
            handlingClient.DefaultRequestHeaders.Clear();
            handlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            handlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            UserModulesRequestData usersModulesRequest = new UserModulesRequestData();
            usersModulesRequest.UserIds.Add(userId);
            HttpContent loginRequestContent = new StringContent(JsonUtility.ToJson(usersModulesRequest));
            loginRequestContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

            HttpResponseMessage response = await handlingClient.PostAsync("/access/users", loginRequestContent);
            string body = await response.Content.ReadAsStringAsync();
            object responseContent = JsonConvert.DeserializeObject<GetUserModulesResponse>(body);
            if ((responseContent as GetUserModulesResponse).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            }
            else
            {
                (responseContent as GetUserModulesResponse).ParseData();
            }

            OnAPIResponse.Invoke(ResponseType.RT_GET_USER_MODULES, response, responseContent);
        }

        public async void JoinSession(string authToken, JoinSessionData joinData)
        {
            handlingClient.DefaultRequestHeaders.Clear();
            handlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            handlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpContent joinSessionRequestContent = new StringContent(joinData.ToJSON());
            joinSessionRequestContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

            HttpResponseMessage response = await handlingClient.PostAsync("/event", joinSessionRequestContent);
            string body = await response.Content.ReadAsStringAsync();
            object responseContent = JsonConvert.DeserializeObject<JoinSessionResponse>(body);
            if ((responseContent as FailureResponse).HasErrored())
            {
                responseContent = null;
            }
            else
            {
                JoinSessionResponse joinSessionResponse = (responseContent as JoinSessionResponse);
                joinSessionResponse.ParseData();
                responseContent = joinSessionResponse;
            }

            OnAPIResponse.Invoke(ResponseType.RT_SESSION_JOINED, response, responseContent);
        }

        public async void GetModuleAccess(int moduleId, int userId)
        {
            handlingClient.DefaultRequestHeaders.Clear();
            handlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            HttpResponseMessage response = await handlingClient.GetAsync(String.Format("/access/user/{0}/module/{1}", userId, moduleId));
            string body = await response.Content.ReadAsStringAsync();


            object responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            if (!(responseContent as FailureResponse).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<UserAccessResponseContent>(body);

            }
            OnAPIResponse.Invoke(ResponseType.RT_GET_USER_ACCESS, response, responseContent);
        }

        public async void SendHeartbeat(string authToken, int sessionId)
        {
            apiHandlingClient.DefaultRequestHeaders.Clear();
            apiHandlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            apiHandlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HeartbeatData heartbeatData = new HeartbeatData(sessionId);

            HttpContent heartbeatRequestContent = new StringContent(heartbeatData.ToJSON());
            heartbeatRequestContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

            HttpResponseMessage response = await apiHandlingClient.PostAsync("/heartbeat/pulse", heartbeatRequestContent);
            string body = await response.Content.ReadAsStringAsync();
            object responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            if ((responseContent as FailureResponse).HasErrored())
            {
                responseContent = null;
            }

            OnAPIResponse.Invoke(ResponseType.RT_HEARTBEAT, response, responseContent);
        }

        public async void CompleteSession(string authToken, CompleteSessionData completionData)
        {
            handlingClient.DefaultRequestHeaders.Clear();
            handlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            handlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpContent completeSessionRequestContent = new StringContent(completionData.ToJSON());
            completeSessionRequestContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

            HttpResponseMessage response = await handlingClient.PostAsync("/event", completeSessionRequestContent);
            string body = await response.Content.ReadAsStringAsync();
            object responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            if ((responseContent as FailureResponse).HasErrored())
            {
                responseContent = null;
            }

            OnAPIResponse.Invoke(ResponseType.RT_SESSION_COMPLETE, response, responseContent);
        }

        public async void SendSessionEvent(string authToken, SessionEventData sessionEvent)
        {
            handlingClient.DefaultRequestHeaders.Clear();
            handlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            handlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpContent sessionEventRequestContent = new StringContent(sessionEvent.ToJSON());
            sessionEventRequestContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

            HttpResponseMessage response = await handlingClient.PostAsync("/event", sessionEventRequestContent);
            string body = await response.Content.ReadAsStringAsync();
            object responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            if ((responseContent as FailureResponse).HasErrored())
            {
                responseContent = null;
            }

            OnAPIResponse.Invoke(ResponseType.RT_SESSION_EVENT, response, responseContent);
        }

        public async void GetModuleList(string authToken, string platform)
        {
            handlingClient.DefaultRequestHeaders.Clear();
            handlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            handlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await handlingClient.GetAsync(string.Format("/modules?platform={0}", platform));
            string body = await response.Content.ReadAsStringAsync();
            object responseContent = null;
            List<OrgModule> orgModules = new List<OrgModule>();
            JArray array = JArray.Parse(body);
            if(array != null)
            {
                var tokens = array.Children();
                foreach(JToken selectedToken in tokens)
                {
                    OrgModule orgModule = new OrgModule(selectedToken);
                    orgModules.Add(orgModule);
                }
            }

            Debug.Log(orgModules.Count.ToString());

            if(orgModules.Count <= 0)
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            }
            else
            {
                responseContent = orgModules;
            }

            OnAPIResponse.Invoke(ResponseType.RT_GET_MODULES_LIST, response, responseContent);
        }
    }
}