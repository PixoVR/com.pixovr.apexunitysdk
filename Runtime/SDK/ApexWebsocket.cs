using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Text;
using PixoVR.Apex.Events;

namespace PixoVR.Apex
{
    public enum ApexWebsocketRequestType : uint
    {
        AuthorizationCode = 0
    }

    public class ApexWebsocket
    {
        public OnWebSocketConnectSuccessful OnConnectSuccess = new OnWebSocketConnectSuccessful();
        public OnWebSocketConnectFailed OnConnectFailed = new OnWebSocketConnectFailed();
        public OnWebSocketReceive OnReceive = new OnWebSocketReceive();
        public OnWebSocketClosed OnClosed = new OnWebSocketClosed();

        ClientWebSocket WebSocket;
        Uri ServerEndpoint;
        Queue<ApexWebsocketRequestType> PendingWebsocketRequests = new Queue<ApexWebsocketRequestType>();
        Queue<string> PendingReceiveResults = new Queue<string>();

        public bool IsConnected()
        {
            if (WebSocket == null)
                return false;

            return WebSocket.State == WebSocketState.Open;
        }

        public void Update()
        {
            while(PendingReceiveResults.Count > 0)
            {
                OnReceive.Invoke(PendingReceiveResults.Dequeue());
            }
        }

        public async Task<bool> Connect(Uri endpoint, int attemptTries = 3)
        {
            Debug.Log("Connecting websocket to endpoint " + endpoint.ToString());
            if (WebSocket == null)
            {
                WebSocket = new ClientWebSocket();
                WebSocket.Options.AddSubProtocol("wss");
            }

            ServerEndpoint = endpoint;
            
            var tokenSource = new System.Threading.CancellationTokenSource();
            tokenSource.CancelAfter(10000);

            string connectionMessage = "";

            int currentAttempt = 0;
            while(currentAttempt < attemptTries)
            {
                try
                {
                    await WebSocket.ConnectAsync(ServerEndpoint, tokenSource.Token);
                }
                catch(Exception ex)
                {
                    if(!(ex is WebSocketException))
                    {
                        // Silently give up here because of an awful exception happening.
                        Debug.LogError(ex);
                        currentAttempt = attemptTries;
                    }

                    connectionMessage = ex.Message;
                    WebSocket = new ClientWebSocket();
                }

                if (WebSocket.State == WebSocketState.Open)
                {
                    break;
                }

                currentAttempt++;
                Debug.LogWarning("Failed to connect on attempt " + currentAttempt + ".");
                await Task.Delay(5000);
            }

            Debug.Log("Web socket connection attempt has occured.");
            if (WebSocket.State != WebSocketState.Open)
            {
                OnConnectFailed.Invoke(connectionMessage);
                return false;
            }

            Debug.Log("Web socket connection was successful.");
            OnConnectSuccess.Invoke();

            StartReceiving();
            ProcessNextRequest();

            // Return based on websocket state because ReceiveAsync can trigger a WebSocket to abort in a weird scenario.
            return WebSocket.State == WebSocketState.Open;
        }

        void HandleOnMessageSent()
        {
            Debug.Log("Message Sent");
            ProcessNextRequest();
        }

        public bool RequestAuthorizationCode()
        {
            PendingWebsocketRequests.Enqueue(ApexWebsocketRequestType.AuthorizationCode);
            
            if (WebSocket != null && WebSocket.State == WebSocketState.Open)
            {
                ProcessNextRequest();
                return true;
            }

            return false;
        }

        void ProcessNextRequest()
        {
            if (PendingWebsocketRequests.Count > 0)
            {
                var NextRequestType = PendingWebsocketRequests.Dequeue();

                switch (NextRequestType)
                {
                    case ApexWebsocketRequestType.AuthorizationCode:
                        {
                            Debug.Log("Sending Auth Code Request");
                            SendString("{\"action\": \"authcode\" }");
                        }
                        break;
                    default:
                        {
                            Debug.LogError("Invalid request type submitted of type " + NextRequestType);
                        }
                        break;
                }
            }
        }

        void StartReceiving()
        {
            Task.Run(() => Receive());
        }

        void SendString(string message)
        {
            Debug.Log("Sending string: " + message);
            byte[] messageArray = Encoding.UTF8.GetBytes(message);
            Task.Run(() => Send(messageArray));
        }

        async void Send(byte[] message)
        {
            Debug.Log("Sending data of length " + message.Length);
            bool socketClosed = (WebSocket.State != WebSocketState.Open);
            bool messageSent = true;

            if(!socketClosed)
            {
                try
                {
                    var buffer = new ArraySegment<Byte>(message, 0, message.Length);
                    var cancelTokenSource = new System.Threading.CancellationTokenSource();
                    cancelTokenSource.CancelAfter(10000);
                    await WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancelTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Websocket send stopped with exception of type " + ex.GetType().Name);
                    // WebSocketException means likely that the socket was aborted.
                    if (ex is OperationCanceledException || WebSocket.CloseStatus.HasValue)
                    {
                        Debug.LogWarning("Websocket closed because " + ex.Message);
                    }
                    else if (ex is WebSocketException)
                    {
                        await CloseSocket();
                    }

                    messageSent = false;
                }
            }

            if(messageSent)
            {
                HandleOnMessageSent();
            }
        }

        async void Receive()
        {
            bool socketClosed = (WebSocket.State != WebSocketState.Open);

            byte[] receiveBuffer = new byte[2048];
            int bufferOffset = 0;
            int maxPacketSize = 256;
            while (!socketClosed)
            {
                bool finishedReceiving = false;
                while (!finishedReceiving)
                {
                    try
                    {
                        ArraySegment<byte> bytesReceived = new ArraySegment<byte>(receiveBuffer, bufferOffset, maxPacketSize);

                        // No cancellation token because cancelling destroys the socket.
                        WebSocketReceiveResult result = await WebSocket.ReceiveAsync(bytesReceived, System.Threading.CancellationToken.None);

                        //Partial data received
                        Debug.Log("Data: " + Encoding.UTF8.GetString(receiveBuffer, bufferOffset, result.Count));

                        bufferOffset += result.Count;
                        if (result.EndOfMessage)
                        {
                            finishedReceiving = true;
                            PendingReceiveResults.Enqueue(Encoding.UTF8.GetString(receiveBuffer, 0, bufferOffset));
                            Array.Clear(receiveBuffer, 0, 2048);
                            bufferOffset = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("Websocket receive stopped with exception of type " + ex.GetType().Name);
                        // WebSocketException means likely that the socket was aborted.
                        if (ex is OperationCanceledException || WebSocket.CloseStatus.HasValue)
                        {
                            Debug.LogWarning("Websocket closed.");
                            finishedReceiving = socketClosed = true;
                        }
                        else if(ex is WebSocketException)
                        {
                            Debug.LogWarning("Websocket closed because " + ex.Message);
                            await CloseSocket();
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                socketClosed = WebSocket.CloseStatus.HasValue;
            }
        }

        public async Task CloseSocket()
        {
            if(WebSocket != null)
            {
                PendingWebsocketRequests.Clear();
                var oldWebSocket = WebSocket;
                WebSocket = null;
                await oldWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutting down the websocket.", System.Threading.CancellationToken.None);
                OnClosed.Invoke(WebSocketCloseStatus.NormalClosure);
            }
        }

        ~ApexWebsocket()
        {
            var socketClose = CloseSocket();
        }
    }
}
