using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PixoVR.Apex.Events;
using UnityEngine;

namespace PixoVR.Apex
{
	public enum EApexWebsocketRequestType
	{
		AUTHORIZATION_CODE = 0,
	}

	/// <summary>
	/// The Headset API's WebSocket (wss://host/ws), used to log in with an authorization code
	/// shown on the headset. Messages are received on a background task and handed to
	/// <see cref="OnReceive"/> from <see cref="Update"/>, on the main thread.
	/// </summary>
	public class ApexWebsocket
	{
		private const int kConnectTimeoutMilliseconds = 10000;
		private const int kSendTimeoutMilliseconds = 10000;
		private const int kRetryDelayMilliseconds = 5000;
		private const int kReceiveChunkSize = 2048;

		private readonly Queue<EApexWebsocketRequestType> m_pendingRequests = new Queue<EApexWebsocketRequestType>();
		private readonly ConcurrentQueue<string> m_receivedMessages = new ConcurrentQueue<string>();
		private ClientWebSocket m_webSocket;
		private Uri m_serverEndpoint;

		public OnWebSocketConnectSuccessful OnConnectSuccess { get; } = new OnWebSocketConnectSuccessful();
		public OnWebSocketConnectFailed OnConnectFailed { get; } = new OnWebSocketConnectFailed();
		public OnWebSocketReceive OnReceive { get; } = new OnWebSocketReceive();
		public OnWebSocketClosed OnClosed { get; } = new OnWebSocketClosed();

		public bool IsConnected()
		{
			return m_webSocket != null && m_webSocket.State == WebSocketState.Open;
		}

		/// <summary>Delivers received messages. Call from the main thread every frame.</summary>
		public void Update()
		{
			while (m_receivedMessages.TryDequeue(out string receivedMessage))
			{
				OnReceive.Invoke(receivedMessage);
			}
		}

		public async Task<bool> Connect(Uri serverEndpoint, int attemptCount = 3)
		{
			Debug.Log("Connecting websocket to endpoint " + serverEndpoint);
			m_serverEndpoint = serverEndpoint;
			string failureReason = string.Empty;
			for (int attemptIndex = 0; attemptIndex < attemptCount; attemptIndex++)
			{
				m_webSocket = new ClientWebSocket();
				m_webSocket.Options.AddSubProtocol("wss");
				try
				{
					using (CancellationTokenSource timeoutSource =
						new CancellationTokenSource(kConnectTimeoutMilliseconds))
					{
						await m_webSocket.ConnectAsync(m_serverEndpoint, timeoutSource.Token);
					}
				}
				catch (Exception exception)
				{
					failureReason = exception.Message;
					if (!(exception is WebSocketException) && !(exception is OperationCanceledException))
					{
						Debug.LogError(exception);
						break;
					}
				}

				if (m_webSocket.State == WebSocketState.Open)
				{
					break;
				}

				Debug.LogWarning("Failed to connect on attempt " + (attemptIndex + 1) + ".");
				await Task.Delay(kRetryDelayMilliseconds);
			}

			if (m_webSocket == null || m_webSocket.State != WebSocketState.Open)
			{
				OnConnectFailed.Invoke(failureReason);
				return false;
			}

			Debug.Log("Web socket connection was successful.");
			OnConnectSuccess.Invoke();
			StartReceiving(m_webSocket);
			ProcessNextRequest();
			return m_webSocket.State == WebSocketState.Open;
		}

		/// <summary>Asks the platform for an authorization code; sent once the socket is open.</summary>
		public bool RequestAuthorizationCode()
		{
			m_pendingRequests.Enqueue(EApexWebsocketRequestType.AUTHORIZATION_CODE);
			if (IsConnected())
			{
				ProcessNextRequest();
				return true;
			}

			return false;
		}

		public async Task CloseSocket()
		{
			if (m_webSocket == null)
			{
				return;
			}

			m_pendingRequests.Clear();
			ClientWebSocket closingSocket = m_webSocket;
			m_webSocket = null;
			try
			{
				if (closingSocket.State == WebSocketState.Open)
				{
					await closingSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutting down the websocket.",
						CancellationToken.None);
				}
			}
			catch (Exception exception)
			{
				Debug.LogWarning("Websocket close failed: " + exception.Message);
			}
			finally
			{
				closingSocket.Dispose();
			}

			OnClosed.Invoke(WebSocketCloseStatus.NormalClosure);
		}

		private void ProcessNextRequest()
		{
			if (m_pendingRequests.Count == 0)
			{
				return;
			}

			EApexWebsocketRequestType nextRequestType = m_pendingRequests.Dequeue();
			switch (nextRequestType)
			{
				case EApexWebsocketRequestType.AUTHORIZATION_CODE:
					Debug.Log("Sending Auth Code Request");
					SendText("{\"action\": \"authcode\" }");
					break;
				default:
					Debug.LogError("Invalid request type submitted of type " + nextRequestType);
					break;
			}
		}

		private void SendText(string messageText)
		{
			byte[] messageBytes = Encoding.UTF8.GetBytes(messageText);
			ClientWebSocket sendingSocket = m_webSocket;
			Task.Run(() => Send(sendingSocket, messageBytes));
		}

		private async Task Send(ClientWebSocket sendingSocket, byte[] messageBytes)
		{
			if (sendingSocket == null || sendingSocket.State != WebSocketState.Open)
			{
				return;
			}

			try
			{
				using (CancellationTokenSource timeoutSource = new CancellationTokenSource(kSendTimeoutMilliseconds))
				{
					await sendingSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
						timeoutSource.Token);
				}
			}
			catch (Exception exception)
			{
				Debug.LogWarning("Websocket send stopped: " + exception.Message);
				return;
			}

			ProcessNextRequest();
		}

		private void StartReceiving(ClientWebSocket receivingSocket)
		{
			Task.Run(() => Receive(receivingSocket));
		}

		private async Task Receive(ClientWebSocket receivingSocket)
		{
			byte[] receiveChunk = new byte[kReceiveChunkSize];
			using (MemoryStream messageBytes = new MemoryStream())
			{
				while (receivingSocket.State == WebSocketState.Open)
				{
					WebSocketReceiveResult receiveResult;
					try
					{
						// No cancellation token: cancelling a receive aborts the socket.
						receiveResult = await receivingSocket.ReceiveAsync(new ArraySegment<byte>(receiveChunk),
							CancellationToken.None);
					}
					catch (Exception exception)
					{
						Debug.LogWarning("Websocket receive stopped: " + exception.Message);
						return;
					}

					if (receiveResult.MessageType == WebSocketMessageType.Close)
					{
						return;
					}

					messageBytes.Write(receiveChunk, 0, receiveResult.Count);
					if (receiveResult.EndOfMessage)
					{
						m_receivedMessages.Enqueue(Encoding.UTF8.GetString(messageBytes.ToArray()));
						messageBytes.SetLength(0);
					}
				}
			}
		}
	}
}
