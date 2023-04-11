using UnityEngine;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Text;
using PixoVR.Apex.Events;

namespace PixoVR.Apex
{
    public class ApexWebsocket
    {
        public OnWebSocketConnectSuccessful OnConnectSuccess = new OnWebSocketConnectSuccessful();
        public OnWebSocketConnectFailed OnConnectFailed = new OnWebSocketConnectFailed();
        public OnWebSocketReceive OnReceive = new OnWebSocketReceive();
        public OnWebSocketClosed OnClosed = new OnWebSocketClosed();

        ClientWebSocket WebSocket;
        Uri ServerEndpoint;

        public bool IsConnected()
        {
            if (WebSocket == null)
                return false;

            return WebSocket.State == WebSocketState.Open;
        }

        public async Task<bool> Connect(Uri endpoint, int attemptTries = 3)
        {
            if (WebSocket == null)
            {
                WebSocket = new ClientWebSocket();
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

            if (WebSocket.State != WebSocketState.Open)
            {
                OnConnectFailed.Invoke(connectionMessage);
                return false;
            }

            OnConnectSuccess.Invoke();

            StartReceiving();

            // Return based on websocket state because ReceiveAsync can trigger a WebSocket to abort in a weird scenario.
            return WebSocket.State == WebSocketState.Open;
        }

        void StartReceiving()
        {
            Task.Run(() => Receive());
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
                            OnReceive.Invoke(Encoding.UTF8.GetString(receiveBuffer, 0, bufferOffset));
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
                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutting down the websocket.", System.Threading.CancellationToken.None);
                OnClosed.Invoke(WebSocketCloseStatus.NormalClosure);
            }
        }

        ~ApexWebsocket()
        {
            var socketClose = CloseSocket();
        }
    }
}
