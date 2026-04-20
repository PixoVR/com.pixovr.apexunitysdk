using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PixoVR.Apex.XAPI;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PixoVR.Apex
{
    public class WebGLPlatformAPIHandler : BaseAPIHandler
    {
        public delegate void APIResponse(ResponseType type, HttpResponseMessage message, object responseData);
        public APIResponse OnAPIResponse;

        protected string URL = "";
        protected string apiURL = "";

        // ---------------------------------------------------------------------------
        // Helpers to bridge UnityWebRequest -> HttpResponseMessage (kept for
        // override signatures that return HttpResponseMessage to callers).
        // ---------------------------------------------------------------------------

        private static HttpResponseMessage ToHttpResponse(UnityWebRequest uwr)
        {
            var code = (System.Net.HttpStatusCode)(uwr.responseCode > 0 ? uwr.responseCode : 400);
            return new HttpResponseMessage(code);
        }

        private static HttpResponseMessage BadRequestResponse()
            => new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);

        private static HttpResponseMessage InternalErrorResponse()
            => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);

        // Awaitable wrapper so async methods can use a single await instead of coroutines.
        private static Task<UnityWebRequest> SendAsync(UnityWebRequest uwr)
        {
            var tcs = new TaskCompletionSource<UnityWebRequest>();
            var op = uwr.SendWebRequest();
            op.completed += _ => tcs.SetResult(uwr);
            return tcs.Task;
        }

        // ---------------------------------------------------------------------------
        // Request factory helpers
        // ---------------------------------------------------------------------------

        private UnityWebRequest MakeGet(string baseUrl, string path, string authToken = null)
        {
            string uri = baseUrl.TrimEnd('/') + path;
            var uwr = UnityWebRequest.Get(uri);
            uwr.SetRequestHeader("Accept", "application/json");
            if (!string.IsNullOrEmpty(authToken))
                uwr.SetRequestHeader("Authorization", "Bearer " + authToken);
            return uwr;
        }

        private UnityWebRequest MakePost(string baseUrl, string path, string jsonBody, string authToken = null, string contentType = "application/json")
        {
            string uri = baseUrl.TrimEnd('/') + path;
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            var uwr = new UnityWebRequest(uri, "POST");
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", contentType);
            uwr.SetRequestHeader("Accept", "application/json");
            if (!string.IsNullOrEmpty(authToken))
                uwr.SetRequestHeader("Authorization", "Bearer " + authToken);
            return uwr;
        }

        // ---------------------------------------------------------------------------
        // Constructor / endpoint setup
        // ---------------------------------------------------------------------------

        public WebGLPlatformAPIHandler()
            : this(PlatformEndpoints.NorthAmerica_ProductionEnvironment) { }

        public WebGLPlatformAPIHandler(string endpointUrl)
        {
            SetEndpoint(endpointUrl);
        }

        public override void SetEndpoint(string endpointUrl)
        {
            URL = endpointUrl;
            Debug.Log("[PlatformAPIHandler] Set Endpoint to " + URL);
        }

        public override void SetPlatformEndpoint(string endpointUrl)
        {
            apiURL = endpointUrl;
        }

        // ---------------------------------------------------------------------------
        // API methods
        // ---------------------------------------------------------------------------

        public override async void Ping(Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            UnityWebRequest uwr = MakeGet(URL, "/ping");
            try
            {
                await SendAsync(uwr);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Exception has occurred: " + ex.Message);
                var bad = BadRequestResponse();
                OnAPIResponse?.Invoke(ResponseType.RT_FAILED_RESPONSE, bad, null);
                failure?.Invoke(bad, new FailureResponse { Error = "True", HttpCode = "400", Message = "Failed " });
                return;
            }

            success?.Invoke(ToHttpResponse(uwr), null);
        }

        class GenerateAuthCodeInput
        {
            public int[] userIds;
        }

        public override async void GenerateAssistedLogin(string authToken, int userId, Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            string jsonContent;

            if (userId >= 0)
            {
                var userIdArray = new int[1] { userId };
                var input = new { input = new GenerateAuthCodeInput { userIds = userIdArray } };
                var graphqlRequest = new
                {
                    operationName = "generateAuthCode",
                    variables = input,
                    query = "mutation generateAuthCode($input: AuthCodeInput!) { generateAuthCode(input: $input) { code expiresAt __typename }}",
                };
                jsonContent = JsonConvert.SerializeObject(graphqlRequest);
            }
            else
            {
                var graphqlRequest = new
                {
                    operationName = "generateAuthCode",
                    variables = new { input = new { } },
                    query = "mutation generateAuthCode($input: AuthCodeInput!) { generateAuthCode(input: $input) { code expiresAt __typename }}",
                };
                jsonContent = JsonConvert.SerializeObject(graphqlRequest);
            }

            UnityWebRequest uwr = MakePost(apiURL, "/v2/query", jsonContent, authToken);
            object responseContent = null;
            HttpResponseMessage response;
            try
            {
                await SendAsync(uwr);
                response = ToHttpResponse(uwr);
                string body = uwr.downloadHandler.text;
                Debug.Log(body);

                JObject jsonResponse = JObject.Parse(body);
                var failureResponse = GetGQLFailureResponse(jsonResponse, "generateAuthCode");
                if (failureResponse != null)
                {
                    failure?.Invoke(response, failureResponse);
                    return;
                }

                string code = jsonResponse["data"]["generateAuthCode"]["code"]?.ToString();
                string expiresAt = jsonResponse["data"]["generateAuthCode"]["expiresAt"]?.ToString();

                GeneratedAssistedLogin assistedLogin = new GeneratedAssistedLogin
                {
                    AssistedLogin = new AssistedLoginCode { AuthCode = code, Expires = expiresAt },
                };
                responseContent = assistedLogin;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating assisted login: {ex.Message}");
                response = InternalErrorResponse();
                failure?.Invoke(response, new FailureResponse { Error = "true", Message = ex.Message });
                return;
            }

            success?.Invoke(response, responseContent);
        }

        public override async void GetUserMetricsForOrg(string authToken, int orgID, int page, FilterParams filterParams, Action<UserMetricsResponse, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            var paramsInput = new
            {
                search = filterParams.searchText,
                sortField = filterParams.sortField,
                sortOrder = filterParams.sortOrder == FilterParams.SortOrder.Ascending ? "ASC" : "DESC",
            };

            var graphqlRequest = new
            {
                operationName = "userMetrics",
                variables = new { orgId = orgID, limit = 10, page = page, @params = paramsInput },
                query = "query userMetrics($orgId: ID!, $limit: Int, $page: Int, $params: GenericQueryParamsInput) { userMetrics(orgId: $orgId, limit: $limit, page: $page, params: $params) { result { id firstName lastName username email role orgId org { id name } createdAt orgUnitId orgUnit { id name externalId } lastModuleId lastModule { id abbreviation description } sessionCount lastActiveAt isInModule } pageInfo { totalCount page offset pageSize previousPage nextPage } } }\r\n"
            };

            string jsonContent = JsonConvert.SerializeObject(graphqlRequest);
            UnityWebRequest uwr = MakePost(apiURL, "/v2/query", jsonContent, authToken);
            HttpResponseMessage response;
            try
            {
                await SendAsync(uwr);
                response = ToHttpResponse(uwr);
                string body = uwr.downloadHandler.text;

                JObject jsonResponse = JObject.Parse(body);
                var failureResponse = GetGQLFailureResponse(jsonResponse, "userMetrics");
                if (failureResponse != null)
                {
                    OnAPIResponse?.Invoke(ResponseType.RT_GET_USER_METRICS_FOR_ORG, response, failureResponse);
                    failure?.Invoke(response, failureResponse);
                    return;
                }

                var userMetricsJSON = jsonResponse["data"]["userMetrics"];
                var userMetricsResponse = JsonConvert.DeserializeObject<UserMetricsResponse>(userMetricsJSON.ToString());
                userMetricsResponse.result.ForEach(u => u.RefreshDisplayFields());

                success?.Invoke(userMetricsResponse, response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error retrieving users: {ex.Message}");
                response = InternalErrorResponse();
                failure?.Invoke(response, new FailureResponse { Error = "true", Message = ex.Message });
            }
        }

        public override async void GetDevicesForOrg(string authToken, int orgID, int page, FilterParams filterParams, Action<HttpResponseMessage, OrgDevicesResponse> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            var graphqlRequest = new
            {
                operationName = "OrgDeviceLicenses",
                variables = new
                {
                    orgId = orgID,
                    limit = 25,
                    page = page,
                    search = filterParams.searchText,
                    sortField = filterParams.sortField,
                    sortOrder = filterParams.sortOrder == FilterParams.SortOrder.Ascending ? "ASC" : "DESC",
                },
                query = "query OrgDeviceLicenses($orgId: ID!, $limit: Int!, $page: Int!, $search: String, $sortField: String, $sortOrder: SortOrder) { orgDeviceLicenses(orgId: $orgId, limit: $limit, page: $page, search: $search, deviceLicenseParams: { sortField: $sortField, sortOrder: $sortOrder }) { result { id name serial manufacturer macAddress model notes online batteryLevel lastSeen location { city region country continent timezone latitude longitude formatter } expiresAt org { id name } deviceType currentApp { id abbreviation description } user { fullname email username } } pageInfo { totalCount page offset pageSize previousPage nextPage } } }\r\n"
            };

            string jsonContent = JsonConvert.SerializeObject(graphqlRequest);
            UnityWebRequest uwr = MakePost(apiURL, "/v2/query", jsonContent, authToken);
            HttpResponseMessage response;
            OrgDevicesResponse responseContent = null;
            try
            {
                await SendAsync(uwr);
                response = ToHttpResponse(uwr);
                string body = uwr.downloadHandler.text;

                JObject jsonResponse = JObject.Parse(body);
                var failureResponse = GetGQLFailureResponse(jsonResponse, "orgDeviceLicenses");
                if (failureResponse != null)
                {
                    failure?.Invoke(response, failureResponse);
                    return;
                }

                var orgDevicesJSON = jsonResponse["data"]["orgDeviceLicenses"];
                var deviceResponse = JsonConvert.DeserializeObject<OrgDevicesResponse>(orgDevicesJSON.ToString());
                deviceResponse.result.ForEach(u => u.RefreshDisplayFields());
                responseContent = deviceResponse;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error retrieving devices: {ex.Message}");
                response = InternalErrorResponse();
                failure?.Invoke(response, new FailureResponse { Error = "true", Message = ex.Message });
                return;
            }

            success?.Invoke(response, responseContent);
        }

        public override async void GetSessionHistory(string authToken, int page, SessionFilters sessionFilters, FilterParams filterParams, Action<HttpResponseMessage, SessionHistoryResponse> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            var paramsInput = new
            {
                search = filterParams.searchText,
                sortField = filterParams.sortField,
                sortOrder = filterParams.sortOrder == FilterParams.SortOrder.Ascending ? "ASC" : "DESC",
            };

            var graphqlRequest = new
            {
                operationName = "userSessionHistory",
                variables = new
                {
                    userId = sessionFilters.userIDs[0],
                    limit = 10,
                    page = page,
                    @params = paramsInput,
                },
                query = "query userSessionHistory($userId: ID!, $limit: Int, $page: Int, $params: GenericQueryParamsInput ){ userSessionHistory(userId: $userId, limit: $limit, page: $page, params: $params) { result { id userId moduleId module { id abbreviation description } rawScore maxScore status result startedAt completedAt } pageInfo { totalCount page offset pageSize previousPage nextPage } } }\r\n"
            };

            string jsonContent = JsonConvert.SerializeObject(graphqlRequest);
            UnityWebRequest uwr = MakePost(apiURL, "/v2/query", jsonContent, authToken);
            HttpResponseMessage response;
            SessionHistoryResponse responseContent;
            try
            {
                await SendAsync(uwr);
                response = ToHttpResponse(uwr);
                string body = uwr.downloadHandler.text;

                JObject jsonResponse = JObject.Parse(body);
                var failureResponse = GetGQLFailureResponse(jsonResponse, "userSessionHistory");
                if (failureResponse != null)
                {
                    failure?.Invoke(response, failureResponse);
                    return;
                }

                var sessionHistoryJSON = jsonResponse["data"]["userSessionHistory"];
                var sessionHistoryResponse = JsonConvert.DeserializeObject<SessionHistoryResponse>(sessionHistoryJSON.ToString());
                sessionHistoryResponse.result.ForEach(u => u.RefreshDisplayFields());
                responseContent = sessionHistoryResponse;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error retrieving session history: {ex.Message}");
                response = InternalErrorResponse();
                failure?.Invoke(response, new FailureResponse { Error = "true", Message = ex.Message });
                return;
            }

            success?.Invoke(response, responseContent);
        }

        public override async void LoginWithToken(string token, Action<HttpResponseMessage, ActiveUserInformation> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            Debug.Log($"[WebGLPA] Logging in with token: {token}");
            UnityWebRequest uwr = MakeGet(apiURL, "/v2/auth/validate-signature", token);
            Debug.Log($"[WebGLPA] Sending login with a token.");

            await SendAsync(uwr);
            HttpResponseMessage response = ToHttpResponse(uwr);
            string body = uwr.downloadHandler.text;
            Debug.Log($"[WebGLPA] Body returned as {body}");

            object responseContent = JsonConvert.DeserializeObject<UserLoginResponseContent>(body);
            if ((responseContent as UserLoginResponseContent).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }

            Debug.Log($"[WebGLPA] Got a valid login response!");
            ActiveUserInformation userInformation = new ActiveUserInformation();
            userInformation.User = (responseContent as UserLoginResponseContent).User;
            success?.Invoke(response, userInformation);
        }

        public override async void Login(LoginData login, Action<HttpResponseMessage, ActiveUserInformation> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            Debug.Log("[WebGLPA] Calling Login.");
            string jsonBody = JsonUtility.ToJson(login);
            UnityWebRequest uwr = MakePost(URL, "/login", jsonBody);

            Debug.Log("[WebGLPA] Call to post api login.");
            await SendAsync(uwr);
            HttpResponseMessage response = ToHttpResponse(uwr);
            string body = uwr.downloadHandler.text;
            Debug.Log("[WebGLPA] Got response body: " + body);

            object responseContent = JsonConvert.DeserializeObject<LoginResponseContent>(body);
            if ((responseContent as LoginResponseContent).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }

            Debug.Log("[WebGLPA] Response content deserialized.");
            ActiveUserInformation userInformation = new ActiveUserInformation();
            userInformation.User = responseContent as LoginResponseContent;
            success?.Invoke(response, userInformation);
        }

        public override async void GetUserData(string authToken, int userId)
        {
            UnityWebRequest uwr = MakeGet(URL, string.Format("/user/{0}", userId), authToken);

            await SendAsync(uwr);
            HttpResponseMessage response = ToHttpResponse(uwr);
            string body = uwr.downloadHandler.text;

            object responseContent = JsonConvert.DeserializeObject<GetUserResponseContent>(body);
            if ((responseContent as GetUserResponseContent).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            }

            OnAPIResponse.Invoke(ResponseType.RT_GET_USER, response, responseContent);
        }

        public override async void GetUserModules(string authToken, int userId)
        {
            UserModulesRequestData usersModulesRequest = new UserModulesRequestData();
            usersModulesRequest.UserIds.Add(userId);
            string jsonBody = JsonUtility.ToJson(usersModulesRequest);
            UnityWebRequest uwr = MakePost(URL, "/access/users", jsonBody, authToken);

            await SendAsync(uwr);
            HttpResponseMessage response = ToHttpResponse(uwr);
            string body = uwr.downloadHandler.text;

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

        public override async void GetQuickIDAuthenticationUsers(string serialNumber)
        {
            UnityWebRequest uwr = MakeGet(apiURL, string.Format("/v2/auth/quick-id/get-users?serialNumber={0}", serialNumber));

            await SendAsync(uwr);
            HttpResponseMessage response = ToHttpResponse(uwr);
            string body = uwr.downloadHandler.text;
            Debug.Log($"[WebGLPA] Body returned as {body}");

            object responseContent = JsonConvert.DeserializeObject<QuickIDAuthGetUsersResponse>(body);
            if ((responseContent as QuickIDAuthGetUsersResponse).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            }

            OnAPIResponse.Invoke(ResponseType.RT_QUICK_ID_AUTH_GET_USERS, response, responseContent);
        }

        public override async void QuickIDLogin(QuickIDLoginData login, Action<HttpResponseMessage, ActiveUserInformation> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            Debug.Log("[WebGLPA] Calling Quick ID login.");
            string jsonBody = JsonUtility.ToJson(login);
            Debug.Log("[WebGLPA] Quick ID login request content: " + jsonBody);
            UnityWebRequest uwr = MakePost(apiURL, "/v2/auth/quick-id/login", jsonBody);

            Debug.Log("[WebGLPA] Call to post api Quick ID login.");
            await SendAsync(uwr);
            HttpResponseMessage response = ToHttpResponse(uwr);
            string body = uwr.downloadHandler.text;
            Debug.Log("[WebGLPA] Got response body: " + body);

            object responseContent = JsonConvert.DeserializeObject<PlatformLoginResponse>(body);
            var loginResponseContent = new LoginResponseContent();

            if ((responseContent as PlatformLoginResponse).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
                Debug.Log("[WebGLPA] Response content deserialized and mapped.");
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }

            var platformLoginResponse = responseContent as PlatformLoginResponse;
            loginResponseContent.Token = platformLoginResponse.Token;
            if (platformLoginResponse.User != null)
            {
                loginResponseContent.ID = platformLoginResponse.User.Id;
                loginResponseContent.OrgId = platformLoginResponse.User.OrgId;
                loginResponseContent.First = platformLoginResponse.User.FirstName;
                loginResponseContent.Last = platformLoginResponse.User.LastName;
                loginResponseContent.Email = platformLoginResponse.User.Email;
                loginResponseContent.Role = platformLoginResponse.User.Role;
                loginResponseContent.Org = platformLoginResponse.User.Org;
            }
            else
            {
                Debug.Log("[WebGLPA] Quick ID login response did not contain user data.");
            }

            ActiveUserInformation userInformation = new ActiveUserInformation();
            userInformation.User = loginResponseContent;
            success?.Invoke(response, userInformation);
        }

        public override async void GetModuleAccess(int moduleId, int userId, string serialNumber, Action<HttpResponseMessage, ActiveUserInformation> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            Debug.Log("[WebGLPA Handler] Get Module Access");

            string optionalParameters = "";
            Debug.Log($"Checking for a serial number: {serialNumber}");
            if (!string.IsNullOrEmpty(serialNumber))
            {
                optionalParameters = "?serial=" + serialNumber;
            }

            string path = String.Format("/access/user/{0}/module/{1}{2}", userId, moduleId, optionalParameters);
            Debug.Log($"[{GetType().Name}] Checking module access at: " + path);

            // Accept: */* — set manually after construction
            string uri = URL.TrimEnd('/') + path;
            var uwr = UnityWebRequest.Get(uri);
            uwr.SetRequestHeader("Accept", "*/*");

            await SendAsync(uwr);
            HttpResponseMessage response = ToHttpResponse(uwr);
            string body = uwr.downloadHandler.text;
            Debug.Log($"[{GetType().Name}] GetModuleAccess return body: {body}");

            object responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            if (!(responseContent as FailureResponse).HasErrored())
            {
                var userAccessContent = JsonConvert.DeserializeObject<UserAccessResponseContent>(body);
                ActiveUserInformation userInformation = new ActiveUserInformation();
                userInformation.ModuleUserInformation = userAccessContent;
                success?.Invoke(response, userInformation);
                return;
            }

            failure?.Invoke(response, responseContent as FailureResponse);
        }

        public override async void SendHeartbeat(string authToken, int sessionId, Action<HttpResponseMessage, object> success = null, Action<HttpResponseMessage, FailureResponse> failure = null)
        {
            HeartbeatData heartbeatData = new HeartbeatData(sessionId);
            UnityWebRequest uwr = MakePost(apiURL, "/heartbeat/pulse", heartbeatData.ToJSON(), authToken);

            await SendAsync(uwr);
            HttpResponseMessage response = ToHttpResponse(uwr);
            string body = uwr.downloadHandler.text;

            object responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            if ((responseContent as FailureResponse).HasErrored())
            {
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }

            success?.Invoke(response, null);
        }

        public override async void JoinSession(string authToken, JoinSessionData joinData, Action<HttpResponseMessage, JoinSessionResponse> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            UnityWebRequest uwr = MakePost(URL, "/event", joinData.ToJSON(), authToken);

            await SendAsync(uwr);
            HttpResponseMessage response = ToHttpResponse(uwr);
            string body = uwr.downloadHandler.text;

            object responseContent = JsonConvert.DeserializeObject<JoinSessionResponse>(body);
            if ((responseContent as FailureResponse).HasErrored())
            {
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }

            JoinSessionResponse joinSessionResponse = (responseContent as JoinSessionResponse);
            joinSessionResponse.ParseData();
            success?.Invoke(response, joinSessionResponse);
        }

        public override async void CompleteSession(string authToken, CompleteSessionData completionData, Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            UnityWebRequest uwr = MakePost(URL, "/event", completionData.ToJSON(), authToken);

            await SendAsync(uwr);
            HttpResponseMessage response = ToHttpResponse(uwr);
            string body = uwr.downloadHandler.text;

            object responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            if ((responseContent as FailureResponse).HasErrored())
            {
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }

            success?.Invoke(response, null);
        }

        public override async void SendSessionEvent(string authToken, SessionEventData sessionEvent, Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            UnityWebRequest uwr = MakePost(URL, "/event", sessionEvent.ToJSON(), authToken);

            await SendAsync(uwr);
            HttpResponseMessage response = ToHttpResponse(uwr);
            string body = uwr.downloadHandler.text;

            object responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            if ((responseContent as FailureResponse).HasErrored())
            {
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }

            success?.Invoke(response, null);
        }

        public override async void GetModuleList(string authToken, string platform)
        {
            string path = "/modules";
            if (!string.IsNullOrEmpty(platform))
                path += $"?platform={platform}";

            Debug.Log($"GetModuleList built endpoint: {path}");
            UnityWebRequest uwr = MakeGet(URL, path, authToken);

            await SendAsync(uwr);
            HttpResponseMessage response = ToHttpResponse(uwr);
            string body = uwr.downloadHandler.text;

            try
            {
                if (body.Contains("\"Error\":", StringComparison.CurrentCultureIgnoreCase))
                {
                    var responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
                    OnAPIResponse.Invoke(ResponseType.RT_GET_MODULES_LIST, response, responseContent);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }

            List<OrgModule> orgModules = new List<OrgModule>();
            JArray array = JArray.Parse(body);
            if (array != null)
            {
                foreach (JToken selectedToken in array.Children())
                {
                    OrgModule orgModule = ScriptableObject.CreateInstance<OrgModule>();
                    orgModule.Parse(selectedToken);
                    orgModules.Add(orgModule);
                }
            }

            Debug.Log(orgModules.Count.ToString());
            OnAPIResponse.Invoke(ResponseType.RT_GET_MODULES_LIST, response, orgModules);
        }

        // ---------------------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------------------

        private FailureResponse GetGQLFailureResponse(JObject jsonResponse, string jsonDataObjectKey)
        {
            if (!String.IsNullOrEmpty(jsonResponse["errors"]?.ToString()))
            {
                string errorMessage = jsonResponse["errors"]?[0]?["message"]?.ToString() ?? "Unknown GraphQL error";
                return new FailureResponse { Error = "true", Message = errorMessage };
            }

            if (String.IsNullOrEmpty(jsonResponse["data"]?.ToString()) ||
                String.IsNullOrEmpty(jsonResponse["data"][jsonDataObjectKey]?.ToString()))
            {
                return new FailureResponse
                {
                    Error = "true",
                    Message = "Invalid response format from server",
                };
            }

            return null;
        }
    }
}