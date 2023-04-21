using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WatsonTcp;

namespace VRC.PackageManagement
{
    public class UnityWindowClient
    {
        private string _serverIp = "";
        private int _serverPort = 0;
        private bool _DebugMessages = false;
        private WatsonTcpClient _client = null;
        private CancellationTokenSource _retrySource;
        
        public UnityWindowClient()
        {
            _rpcLookup = new Dictionary<string, StringDelegate>()
            {
                {"GetUnityProjectName", GetUnityProjectName},
            };
            
            InitializeClient();
        }

        ~UnityWindowClient()
        {
            Disconnect();
        }
        
        private void InitializeClient()
        {
            _serverIp = IPAddress.Loopback.ToString();
            _serverPort = 10101;
            CreateClient();
        }
        
        private void CreateClient()
        { 
            if (_client != null) _client.Dispose();

            _client = new WatsonTcpClient(_serverIp, _serverPort);
        
            _client.Events.ServerConnected += ServerConnected;
            _client.Events.ServerDisconnected += ServerDisconnected;
            _client.Events.MessageReceived += MessageReceived;

            _client.Callbacks.SyncRequestReceived = SyncRequestReceived;

            // _Client.Settings.IdleServerTimeoutMs = 5000;
            _client.Settings.DebugMessages = _DebugMessages;
            // _client.Settings.Logger = Logger;

            _client.Keepalive.EnableTcpKeepAlives = true;
            _client.Keepalive.TcpKeepAliveInterval = 1;
            _client.Keepalive.TcpKeepAliveTime = 1;
            _client.Keepalive.TcpKeepAliveRetryCount = 3;
            
            #pragma warning disable 4014
            RetryConnectionAsync();
            #pragma warning restore 4014
        }

        private void ConnectClient()
        {
            try
            {
                Task.Run(_client.Connect);
            }
            catch (Exception)
            {
                
            }
        }

        private int _retryFrequencyMS = 5000;
        
        public async Task RetryConnectionAsync()
        {
            _retrySource = new CancellationTokenSource();
            while (!_client.Connected)
            {
                ConnectClient();
                await Task.Delay(_retryFrequencyMS, _retrySource.Token);
            }
        }

        public void Disconnect()
        {
            _retrySource.Cancel();
            
            if (_client != null)
            {
                if (_client.Connected)
                {
                    _client.Disconnect();
                }
                _client.Dispose();
            }
        }
        
        private void MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            // Log("Message from " + args.IpPort + ": ");
            // if (args.Data != null) Log(Encoding.UTF8.GetString(args.Data));
            // else Log("[null]");

            if (args.Metadata != null && args.Metadata.Count > 0)
            {
                // Log("Metadata:");
                foreach (KeyValuePair<object, object> curr in args.Metadata)
                {
                    Log($"{curr.Key} : {curr.Value}");
                };
            } 
        }
        
        public delegate string StringDelegate();
        private Dictionary<string, StringDelegate> _rpcLookup;
        
        private SyncResponse SyncRequestReceived(SyncRequest req)
        {
            if (req.Metadata != null && req.Metadata.Count > 0)
            {
                if (req.Metadata.TryGetValue("command", out object commandName))
                {
                    if (commandName.ToString() == "RPC")
                    {
                        string methodName = Encoding.UTF8.GetString(req.Data);
                        if (_rpcLookup.TryGetValue(methodName, out StringDelegate action))
                        {
                            return new SyncResponse(req, null, action());
                        }
                    }
                }
            }
            return new SyncResponse(req, string.Empty);
        }

        public string GetUnityProjectName()
        {
            return new System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory())
                .Name;
        }
        
        private void ServerConnected(object sender, ConnectionEventArgs args)
        {
            // Cancel connection attempts if running
            _retrySource.Cancel();
            Log(args.IpPort + " connected");
        }

        private async void ServerDisconnected(object sender, DisconnectionEventArgs args)
        {
            Log(args.IpPort + " disconnected: " + args.Reason.ToString());
            // Give the server a second after a disconnect before we retry
            await Task.Delay(1000);
            #pragma warning disable 4014
            RetryConnectionAsync();
            #pragma warning restore 4014
        }

        private void SendAndWait()
        {
            string userInput = System.Guid.NewGuid().ToString();
            int timeoutMs = 5000;
            Dictionary<object, object> metadata = new Dictionary<object, object>();
            metadata.Add("foo", "bar");
            try
            {
                SyncResponse resp = _client.SendAndWait(timeoutMs, userInput, metadata);
                if (resp.Metadata != null && resp.Metadata.Count > 0)
                {
                    foreach (KeyValuePair<object, object> curr in resp.Metadata)
                    {
                        Log("  " + curr.Key.ToString() + ": " + curr.Value.ToString());
                    }
                }

                Log(Severity.Info, "Response: " + Encoding.UTF8.GetString(resp.Data));
            }
            catch (Exception e)
            {
                Log(Severity.Critical, "Exception: " + e.ToString());
            }
        }

        private void Log(string msg)
        {
            Log(Severity.Debug, msg);
        }
        private void Log(Severity sev, string msg)
        {
            switch (sev)
            {
                case Severity.Alert:
                case Severity.Critical:
                case Severity.Emergency:
                case Severity.Error:
                    UnityEngine.Debug.LogError(msg);
                break;
                case Severity.Warn:
                    UnityEngine.Debug.LogWarning(msg);
                    break;
                case Severity.Info:
                case Severity.Debug:
                default:
                    UnityEngine.Debug.Log(msg);
                    break;
            }
        }
    }

}