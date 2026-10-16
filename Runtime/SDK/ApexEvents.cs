using System;
using System.Net.WebSockets;
using UnityEngine.Events;

namespace PixoVR.Apex.Events
{
	[Serializable]
	public class OnWebSocketConnectSuccessful : UnityEvent
	{
	}

	[Serializable]
	public class OnWebSocketConnectFailed : UnityEvent<string>
	{
	}

	[Serializable]
	public class OnWebSocketReceive : UnityEvent<string>
	{
	}

	[Serializable]
	public class OnWebSocketClosed : UnityEvent<WebSocketCloseStatus>
	{
	}
}
