using UnityEngine.Events;
using System.Net.Http;

namespace PixoVR.Apex.Events
{
    [System.Serializable]
    public class OnHttpResponseEvent : UnityEvent<HttpResponseMessage> { };
    
    [System.Serializable]
    public class OnApexFailureEvent : UnityEvent<FailureResponse> { };
    
    [System.Serializable]
    public class OnModuleAccessSuccessEvent : UnityEvent<LoginResponseContent> { };

    [System.Serializable]
    public class OnLoginSuccessEvent : UnityEvent { };

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
}
