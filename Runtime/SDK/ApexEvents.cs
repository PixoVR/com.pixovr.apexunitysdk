using UnityEngine.Events;

namespace PixoVR.Apex.Events
{
    [System.Serializable]
    public class OnWebSocketConnectSuccessful : UnityEvent { };

    [System.Serializable]
    public class OnWebSocketConnectFailed : UnityEvent<string> { };

    [System.Serializable]
    public class OnWebSocketReceive : UnityEvent<string> { };

    [System.Serializable]
    public class OnWebSocketClosed : UnityEvent<System.Net.WebSockets.WebSocketCloseStatus> { };
}
