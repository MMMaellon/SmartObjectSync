using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using VRC.SDKBase.Editor;

namespace VRC.PackageManagement
{
    public class UnityWindowWebSocketClient
    {
        private string _serverIp = "";
        private int _serverPort = 0;
        private ClientWebSocket _client = null;
        private CancellationTokenSource _retrySource;
        private int _retryFrequencyMS = 5000;
        private const string MessageProjectConnected = "projectConnected";

        public UnityWindowWebSocketClient()
        {
            InitializeClient();
        }

        private void InitializeClient()
        {
            _serverIp = "localhost";
            _serverPort = 5477;
            CreateClient();
        }
        
        private void CreateClient()
        {
            // Dispose client if it exists
            if (_client != null) _client.Dispose();
            
            // Construct a new client
            _client = new ClientWebSocket();

#pragma warning disable 4014
            RetryConnectionAsync();
#pragma warning restore 4014
        }

        private void LogForWebsocket(string message)
        {
            if(VRCPackageSettings.Instance.debugVCCConnection)
                Debug.Log(message);
        }

        private async Task RetryConnectionAsync()
        {
            _retrySource = new CancellationTokenSource();
            
            while (_client.State != WebSocketState.Open)
            {
                // Connect to VCC
                var endpoint = $"ws://{_serverIp}:{_serverPort}";
                LogForWebsocket($"Trying to connect to the WebSocket server at {endpoint}");
                try
                {
                    await _client.ConnectAsync(new Uri(endpoint), _retrySource.Token);
                }
                catch (OperationCanceledException)
                {
                    // no-op
                }
                catch (Exception ex)
                {
                    LogForWebsocket($"Failed to Connect: {ex.Message}");
                    _client.Dispose();
                    _client = new ClientWebSocket();
                }

                if (_client.State != WebSocketState.Open)
                {
                    await Task.Delay(_retryFrequencyMS);
                }
            }
#pragma warning disable 4014
            ServerConnected();
#pragma warning restore 4014
        }

        private async Task Receive(ClientWebSocket socket)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            do
            {
                WebSocketReceiveResult result;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        LogForWebsocket("Closing WebSocket connection");
                        await Disconnect();
                        
#pragma warning disable 4014
                        _client.Dispose();
                        _client = new ClientWebSocket();
                        RetryConnectionAsync();
#pragma warning restore 4014
                        
                        break;
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                        LogForWebsocket(await reader.ReadToEndAsync());
                }
            } while (true);
        }

        private async Task ServerConnected()
        {
            LogForWebsocket($"WebSocket Connection Established");
            
            // Send Message
            LogForWebsocket($"Sending Connection Message");
            await SendProjectConnectedMessage();

            // Receive Message
            await Receive(_client);
        }

        private async Task SendProjectConnectedMessage()
        {
            await SendWSMessage(new WSMessage()
            {
                messageType = MessageProjectConnected,
                data = System.IO.Directory.GetCurrentDirectory()
            });
        }

        public async Task SendWSMessage(WSMessage message)
        { 
            var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
            try
            {
                await _client.SendAsync(sendBuffer, WebSocketMessageType.Text, true, _retrySource.Token);
            }
            catch (Exception ex)
            {
                LogForWebsocket(ex.Message);
            }
        }

        public async Task Disconnect()
        {
            if (_client != null)
            {
                if (_client.State == WebSocketState.Open || _client.State == WebSocketState.CloseReceived)
                {
                    await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing WebSocket Connection", CancellationToken.None);
                }
                _client.Dispose();
            }
            _retrySource.Cancel();
        }
    }
    
    public struct WSMessage
    {
        public string messageType { get; set; }
        public object data { get; set; }
    }
}
