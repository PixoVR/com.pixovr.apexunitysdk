using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PixoVR.Apex.XAPI;
using UnityEngine;

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
        RT_HEARTBEAT,
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

        public APIHandler()
            : this(PlatformEndpoints.NorthAmerica_ProductionEnvironment) { }

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
            EnsureURLHasProtocol(ref endpointUrl);

            URL = endpointUrl;
            Debug.Log("[APIHandler] Set Endpoint to " + URL);
            handlingClient.BaseAddress = new Uri(URL);
        }

        public void SetWebEndpoint(string endpointUrl)
        {
            EnsureURLHasProtocol(ref endpointUrl);

            webURL = endpointUrl;
            webHandlingClient.BaseAddress = new Uri(webURL);
        }

        public void SetPlatformEndpoint(string endpointUrl)
        {
            EnsureURLHasProtocol(ref endpointUrl);

            apiURL = endpointUrl;
            apiHandlingClient.BaseAddress = new Uri(apiURL);
        }

        private void EnsureURLHasProtocol(ref string url)
        {
            if (!url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                if (url.StartsWith("http:", StringComparison.InvariantCultureIgnoreCase))
                {
#if UNITY_EDITOR
                    Debug.LogWarning("URL must be a secured http endpoint for production.");
#else
                    Debug.LogError("URL must be a securated http endpoint.");
#endif
                }
                else
                {
                    url.Insert(0, "https://");
                }
            }
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

        public async void GenerateAssistedLogin(string authToken)
        {
            apiHandlingClient.DefaultRequestHeaders.Clear();
            apiHandlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            apiHandlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Create the GraphQL request payload
            var graphqlRequest = new
            {
                operationName = "generateAuthCode",
                variables = new { input = new { } },
                query = "mutation generateAuthCode($input: AuthCodeInput!) { generateAuthCode(input: $input) { code expiresAt __typename }}",
            };

            string jsonContent = JsonConvert.SerializeObject(graphqlRequest);
            HttpContent requestContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            object responseContent;
            try
            {
                response = await apiHandlingClient.PostAsync("/v2/query", requestContent);
                string body = await response.Content.ReadAsStringAsync();
                Debug.Log(body);

                // Parse the GraphQL response structure
                JObject jsonResponse = JObject.Parse(body);

                if (jsonResponse["data"] != null && jsonResponse["data"]["generateAuthCode"] != null)
                {
                    // Extract the relevant data from the GraphQL response
                    string code = jsonResponse["data"]["generateAuthCode"]["code"]?.ToString();
                    string expiresAt = jsonResponse["data"]["generateAuthCode"]["expiresAt"]?.ToString();

                    // Create the GeneratedAssistedLogin object with the extracted data
                    GeneratedAssistedLogin assistedLogin = new GeneratedAssistedLogin
                    {
                        AssistedLogin = new AssistedLoginCode { AuthCode = code, Expires = expiresAt },
                    };

                    responseContent = assistedLogin;
                }
                else if (jsonResponse["errors"] != null)
                {
                    // Handle GraphQL errors
                    string errorMessage = jsonResponse["errors"]?[0]?["message"]?.ToString() ?? "Unknown GraphQL error";
                    responseContent = new FailureResponse { Error = "true", Message = errorMessage };
                }
                else
                {
                    // Fallback error handling
                    responseContent = new FailureResponse
                    {
                        Error = "true",
                        Message = "Invalid response format from server",
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating assisted login: {ex.Message}");
                response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                responseContent = new FailureResponse { Error = "true", Message = ex.Message };
            }

            OnAPIResponse.Invoke(ResponseType.RT_GEN_AUTH_LOGIN, response, responseContent);
        }

        public async void LoginWithToken(string token)
        {
            Debug.Log($"[Platform API] Logging in with token: {token}");
            apiHandlingClient.DefaultRequestHeaders.Clear();
            apiHandlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            apiHandlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Debug.Log($"[Platform API] Sending login with a token.");
            HttpResponseMessage response = await apiHandlingClient.GetAsync("/v2/auth/validate-signature");
            string body = await response.Content.ReadAsStringAsync();
            Debug.Log($"[Platform API] Body returned as {body}");
            object responseContent = JsonConvert.DeserializeObject<UserLoginResponseContent>(body);
            if ((responseContent as UserLoginResponseContent).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            }

            Debug.Log($"[Platform API] Got a valid login response!");
            object loginResponseContent = (responseContent as UserLoginResponseContent).User;

            OnAPIResponse.Invoke(ResponseType.RT_LOGIN, response, loginResponseContent);
        }

        public async void Login(LoginData login)
        {
            Debug.Log("[Platform API] Calling Login.");
            handlingClient.DefaultRequestHeaders.Clear();

            HttpContent loginRequestContent = new StringContent(JsonUtility.ToJson(login));
            loginRequestContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

            Debug.Log("[Platform API] Call to post api login.");
            HttpResponseMessage response = await handlingClient.PostAsync("/login", loginRequestContent);
            string body = await response.Content.ReadAsStringAsync();
            Debug.Log("[Platform API] Got response body.");
            object responseContent = JsonConvert.DeserializeObject<LoginResponseContent>(body);
            if ((responseContent as LoginResponseContent).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            }

            Debug.Log("[Platform API] Response content deserialized.");
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

        public async void GetModuleAccess(int moduleId, int userId, string serialNumber)
        {
            Debug.Log("[Platform API Handler] Get Module Access");
            handlingClient.DefaultRequestHeaders.Clear();
            handlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            string optionalParameters = "";
            if (serialNumber.Length > 0)
            {
                optionalParameters = "?serial=" + serialNumber;
            }

            Debug.Log(
                $"[{GetType().Name}] Checking module access at: "
                    + String.Format("/access/user/{0}/module/{1}{2}", userId, moduleId, optionalParameters)
            );

            HttpResponseMessage response = await handlingClient.GetAsync(
                String.Format("/access/user/{0}/module/{1}{2}", userId, moduleId, optionalParameters)
            );
            string body = await response.Content.ReadAsStringAsync();

            Debug.Log($"[{GetType().Name}] GetModuleAccess return body: {body}");
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

            HttpResponseMessage response = await apiHandlingClient.PostAsync(
                "/heartbeat/pulse",
                heartbeatRequestContent
            );
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

            string endpoint = "/modules";
            if (platform != null && platform.Length > 0)
            {
                endpoint += $"?platform={platform}";
            }

            Debug.Log($"GetModuleList built endpoint: {endpoint}");

            HttpResponseMessage response = await handlingClient.GetAsync(endpoint);
            string body = await response.Content.ReadAsStringAsync();

            try
            {
                var responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
                OnAPIResponse.Invoke(ResponseType.RT_GET_MODULES_LIST, response, responseContent);
                return;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }

            List<OrgModule> orgModules = new List<OrgModule>();
            JArray array = JArray.Parse(body);
            if (array != null)
            {
                var tokens = array.Children();
                foreach (JToken selectedToken in tokens)
                {
                    OrgModule orgModule = ScriptableObject.CreateInstance<OrgModule>();
                    orgModule.Parse(selectedToken);
                    orgModules.Add(orgModule);
                }
            }

            Debug.Log(orgModules.Count.ToString());
            OnAPIResponse.Invoke(ResponseType.RT_GET_MODULES_LIST, response, orgModules);
        }
    }
}
