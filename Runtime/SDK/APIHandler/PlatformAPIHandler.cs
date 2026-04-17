using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PixoVR.Apex.XAPI;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using UnityEngine;

namespace PixoVR.Apex
{
    public enum ResponseType
    {
        RT_NONE = 0,
        RT_FAILED_RESPONSE,
        RT_LOGIN,
        RT_GET_USER,
        RT_GET_USER_ACCESS,
        RT_GET_USER_MODULES,
        RT_GET_MODULES_LIST,
        RT_GEN_AUTH_LOGIN,
        RT_HEARTBEAT,
        RT_QUICK_ID_AUTH_GET_USERS,
        RT_QUICK_ID_AUTH_LOGIN,
        RT_GET_USER_METRICS_FOR_ORG,
    }

    public class PlatformAPIHandler : BaseAPIHandler
    {
        public delegate void APIResponse(ResponseType type, HttpResponseMessage message, object responseData);
        public APIResponse OnAPIResponse;

        protected string URL = "";
        protected HttpClient handlingClient = null;


        // Need to migrate to this in the future
        protected string apiURL = "";
        protected HttpClient apiHandlingClient = null;

        public PlatformAPIHandler()
            : this(PlatformEndpoints.NorthAmerica_ProductionEnvironment) { }

        public PlatformAPIHandler(string endpointUrl)
        {
            handlingClient = new HttpClient();
            SetEndpoint(endpointUrl);

            apiHandlingClient = new HttpClient();
        }

        HttpResponseMessage HandleException(Exception exception)
        {
            Debug.LogWarning("Exception has occurred: " + exception.Message);
            HttpResponseMessage badRequestResponse = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            OnAPIResponse.Invoke(ResponseType.RT_FAILED_RESPONSE, badRequestResponse, null);
            return badRequestResponse;
        }

        public override void SetEndpoint(string endpointUrl)
        {
            EnsureURLHasProtocol(ref endpointUrl);

            URL = endpointUrl;
            Debug.Log("[PlatformAPIHandler] Set Endpoint to " + URL);
            handlingClient.BaseAddress = new Uri(URL);
        }

        public override void SetPlatformEndpoint(string endpointUrl)
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

        public override async void Ping(Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
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
                failure?.Invoke(response, new FailureResponse { Error = "True", HttpCode = "400", Message = "Failed " });
            }

            success?.Invoke(response, null);
        }

        class GenerateAuthCodeInput
        {
            public int[] userIds;
        }

        public override async void GenerateAssistedLogin(string authToken, int userId, Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            apiHandlingClient.DefaultRequestHeaders.Clear();
            apiHandlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            apiHandlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Create the GraphQL request payload
            string jsonContent = "";

            if (userId >= 0)
            {
                var userIdArray = new int[1] { userId };
                var input = new { input = new GenerateAuthCodeInput { userIds = userIdArray } };
                // Create the GraphQL request payload
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

            HttpContent requestContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            object responseContent = null;
            try
            {
                response = await apiHandlingClient.PostAsync("/v2/query", requestContent);
                string body = await response.Content.ReadAsStringAsync();
                Debug.Log(body);

                // Parse the GraphQL response structure
                JObject jsonResponse = JObject.Parse(body);
                var failureResponse = GetGQLFailureResponse(jsonResponse, "generateAuthCode");
                if (failureResponse != null)
                {
                    failure?.Invoke(response, failureResponse);
                    return;
                }

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
            catch (Exception ex)
            {
                Debug.LogError($"Error generating assisted login: {ex.Message}");
                response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                failure?.Invoke(response, new FailureResponse { Error = "true", Message = ex.Message });
                return;
            }

            success?.Invoke(response, responseContent);
        }

        public override async void GetUserMetricsForOrg(string authToken, int orgID, int page, FilterParams filterParams, Action<UserMetricsResponse, object> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            apiHandlingClient.DefaultRequestHeaders.Clear();
            apiHandlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            apiHandlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
            HttpContent requestContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            object responseContent;
            try
            {
                response = await apiHandlingClient.PostAsync("/v2/query", requestContent);
                string body = await response.Content.ReadAsStringAsync();

                JObject jsonResponse = JObject.Parse(body);
                var failureResponse = GetGQLFailureResponse(jsonResponse, "userMetrics");
                if (failureResponse != null)
                {
                    OnAPIResponse.Invoke(ResponseType.RT_GET_USER_METRICS_FOR_ORG, response, failureResponse);
                    failure?.Invoke(response, failureResponse);
                    return;
                }

                var userMetricsJSON = jsonResponse["data"]["userMetrics"];
                var userMetricsResponse = JsonConvert.DeserializeObject<UserMetricsResponse>(userMetricsJSON.ToString());
                userMetricsResponse.result.ForEach(u => u.RefreshDisplayFields());
                responseContent = userMetricsResponse;

                success?.Invoke(userMetricsResponse, response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error retrieving users: {ex.Message}");
                response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                responseContent = new FailureResponse { Error = "true", Message = ex.Message };
                failure?.Invoke(response, responseContent as FailureResponse);
            }
        }

        public override async void GetDevicesForOrg(string authToken, int orgID, int page, FilterParams filterParams, Action<HttpResponseMessage, OrgDevicesResponse> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            apiHandlingClient.DefaultRequestHeaders.Clear();
            apiHandlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            apiHandlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
            HttpContent requestContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            OrgDevicesResponse responseContent = null;
            try
            {
                response = await apiHandlingClient.PostAsync("/v2/query", requestContent);
                string body = await response.Content.ReadAsStringAsync();

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
                response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                failure?.Invoke(response, new FailureResponse { Error = "true", Message = ex.Message });
                return;
            }

            success?.Invoke(response, responseContent);
        }

        public override async void GetSessionHistory(string authToken, int page, SessionFilters sessionFilters, FilterParams filterParams, Action<HttpResponseMessage, SessionHistoryResponse> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            apiHandlingClient.DefaultRequestHeaders.Clear();
            apiHandlingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            apiHandlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
            HttpContent requestContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response;
            SessionHistoryResponse responseContent;
            try
            {
                response = await apiHandlingClient.PostAsync("/v2/query", requestContent);
                string body = await response.Content.ReadAsStringAsync();
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
                response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                failure?.Invoke(response, new FailureResponse { Error = "true", Message = ex.Message });
                return;
            }

            success?.Invoke(response, responseContent);
        }

        public override async void LoginWithToken(string token, Action<HttpResponseMessage, ActiveUserInformation> success, Action<HttpResponseMessage, FailureResponse> failure)
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
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }

            Debug.Log($"[Platform API] Got a valid login response!");
            ActiveUserInformation userInformation = new ActiveUserInformation();
            userInformation.User = (responseContent as UserLoginResponseContent).User;
            success?.Invoke(response, userInformation);
        }

        public override async void Login(LoginData login, Action<HttpResponseMessage, ActiveUserInformation> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            Debug.Log("[Platform API] Calling Login.");
            handlingClient.DefaultRequestHeaders.Clear();

            HttpContent loginRequestContent = new StringContent(JsonUtility.ToJson(login));
            loginRequestContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

            Debug.Log("[Platform API] Call to post api login.");
            HttpResponseMessage response = await handlingClient.PostAsync("/login", loginRequestContent);
            string body = await response.Content.ReadAsStringAsync();
            Debug.Log("[Platform API] Got response body: " + body);
            object responseContent = JsonConvert.DeserializeObject<LoginResponseContent>(body);
            if ((responseContent as LoginResponseContent).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }

            Debug.Log("[Platform API] Response content deserialized.");
            ActiveUserInformation userInformation = new ActiveUserInformation();
            userInformation.User = responseContent as LoginResponseContent;
            success?.Invoke(response, userInformation);
        }

        public override async void GetUserData(string authToken, int userId)
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

        public override async void GetUserModules(string authToken, int userId)
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

        public override async void GetQuickIDAuthenticationUsers(string serialNumber)
        {
            apiHandlingClient.DefaultRequestHeaders.Clear();
            apiHandlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await apiHandlingClient.GetAsync(string.Format("/v2/auth/quick-id/get-users?serialNumber={0}", serialNumber));
            string body = await response.Content.ReadAsStringAsync();

            Debug.Log($"[Platform API] Body returned as {body}");

            object responseContent = JsonConvert.DeserializeObject<QuickIDAuthGetUsersResponse>(body);
            if ((responseContent as QuickIDAuthGetUsersResponse).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            }

            OnAPIResponse.Invoke(ResponseType.RT_QUICK_ID_AUTH_GET_USERS, response, responseContent);
        }

        public override async void QuickIDLogin(QuickIDLoginData login, Action<HttpResponseMessage, ActiveUserInformation> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            Debug.Log("[Platform API] Calling Quick ID login.");
            apiHandlingClient.DefaultRequestHeaders.Clear();

            HttpContent loginRequestContent = new StringContent(JsonUtility.ToJson(login));
            Debug.Log("[Platform API] Quick ID login request content: " + JsonUtility.ToJson(login));
            loginRequestContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

            Debug.Log("[Platform API] Call to post api Quick ID login.");
            HttpResponseMessage response = await apiHandlingClient.PostAsync("/v2/auth/quick-id/login", loginRequestContent);
            string body = await response.Content.ReadAsStringAsync();
            Debug.Log("[Platform API] Got response body: " + body);
            object responseContent = JsonConvert.DeserializeObject<PlatformLoginResponse>(body);

            var loginResponseContent = new LoginResponseContent();
            if ((responseContent as PlatformLoginResponse).HasErrored())
            {
                responseContent = JsonConvert.DeserializeObject<FailureResponse>(body);
            }
            else
            {
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
                    Debug.Log("[Platform API] Quick ID login response did not contain user data.");
                }

                ActiveUserInformation userInformation = new ActiveUserInformation();
                userInformation.User = loginResponseContent;
                success?.Invoke(response, userInformation);
                return;
            }

            Debug.Log("[Platform API] Response content deserialized and mapped.");
            failure?.Invoke(response, responseContent as FailureResponse);
        }

        public override async void GetModuleAccess(int moduleId, int userId, string serialNumber, Action<HttpResponseMessage, ActiveUserInformation> success, Action<HttpResponseMessage, FailureResponse> failure)
        {
            Debug.Log("[Platform API Handler] Get Module Access");
            handlingClient.DefaultRequestHeaders.Clear();
            handlingClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            string optionalParameters = "";
            Debug.Log($"Checking for a serial number: {serialNumber}");
            if (!string.IsNullOrEmpty(serialNumber))
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
                ActiveUserInformation userInformation = new ActiveUserInformation();
                userInformation.User = responseContent as LoginResponseContent;
                success?.Invoke(response, userInformation);
                return;
            }

            failure?.Invoke(response, responseContent as FailureResponse);
        }

        public override async void SendHeartbeat(string authToken, int sessionId, Action<HttpResponseMessage, object> success = null, Action<HttpResponseMessage, FailureResponse> failure = null)
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
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }

            success?.Invoke(response, null);
        }

        public override async void JoinSession(string authToken, JoinSessionData joinData, Action<HttpResponseMessage, JoinSessionResponse> success, Action<HttpResponseMessage, FailureResponse> failure)
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
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }
            else
            {
                JoinSessionResponse joinSessionResponse = (responseContent as JoinSessionResponse);
                joinSessionResponse.ParseData();
                responseContent = joinSessionResponse;
            }

            success?.Invoke(response, responseContent as JoinSessionResponse);
        }

        public override async void CompleteSession(string authToken, CompleteSessionData completionData, Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
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
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }

            success?.Invoke(response, null);
        }

        public override async void SendSessionEvent(string authToken, SessionEventData sessionEvent, Action<HttpResponseMessage, object> success, Action<HttpResponseMessage, FailureResponse> failure)
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
                failure?.Invoke(response, responseContent as FailureResponse);
                return;
            }

            success?.Invoke(response, null);
        }

        public override async void GetModuleList(string authToken, string platform)
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

        private FailureResponse GetGQLFailureResponse(JObject jsonResponse, string jsonDataObjectKey)
        {

            if (!String.IsNullOrEmpty(jsonResponse["errors"]?.ToString()))
            {
                string errorMessage = jsonResponse["errors"]?[0]?["message"]?.ToString() ?? "Unknown GraphQL error";
                return new FailureResponse { Error = "true", Message = errorMessage };
            }

            if (String.IsNullOrEmpty(jsonResponse["data"]?.ToString()) || String.IsNullOrEmpty(jsonResponse["data"][jsonDataObjectKey]?.ToString()))
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
