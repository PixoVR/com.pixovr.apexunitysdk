using System.Collections.Generic;
using System.Net.Http;
using UnityEngine.Events;

namespace PixoVR.Apex.Events
{
    [System.Serializable]
    public class OnHttpResponseEvent : UnityEvent<HttpResponseMessage> { };

    [System.Serializable]
    public class OnApexFailureEvent : UnityEvent<FailureResponse> { };

    [System.Serializable]
    public class OnModuleAccessSuccessEvent : UnityEvent<LoginResponseContent> { };

    [System.Serializable]
    public class OnWebSocketLoginSucceededEvent : UnityEvent<HttpResponseMessage, ActiveUserInformation> { };

    [System.Serializable]
    public class OnWebSocketLoginFailedEvent : UnityEvent<HttpResponseMessage, FailureResponse> { };

    [System.Serializable]
    public class OnGetUserSuccessEvent : UnityEvent<GetUserResponseContent> { };

    [System.Serializable]
    public class OnGetUserModulesSuccessEvent : UnityEvent<GetUserModulesResponse> { };

    [System.Serializable]
    public class OnWebSocketConnectSuccessful : UnityEvent { };

    [System.Serializable]
    public class OnWebSocketConnectFailed : UnityEvent<string> { };

    [System.Serializable]
    public class OnWebSocketReceive : UnityEvent<string> { };

    [System.Serializable]
    public class OnWebSocketClosed : UnityEvent<System.Net.WebSockets.WebSocketCloseStatus> { };

    [System.Serializable]
    public class OnAuthCodeReceived : UnityEvent<string> { };

    [System.Serializable]
    public class OnGetOrgModulesSuccessEvent : UnityEvent<List<OrgModule>> { };

    [System.Serializable]
    public class OnGeneratedAssistedLoginSuccessEvent : UnityEvent<GeneratedAssistedLogin> { };

    [System.Serializable]
    public class OnQuickIDAuthLoginSuccessEvent : UnityEvent<LoginResponseContent> { };

    [System.Serializable]
    public class OnGetQuickIDAuthUsersSuccessEvent : UnityEvent<QuickIDAuthGetUsersResponse> { };

    [System.Serializable]
    public class OnGetUserMetricsForOrgSuccessEvent : UnityEvent<UserMetricsResponse> { };

    [System.Serializable]
    public class OnGetSessionHistorySuccessEvent : UnityEvent<SessionHistoryResponse> { };
}