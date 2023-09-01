 using System;
 using System.Threading.Tasks;
 using UnityEditor;
 using UnityEngine;
 using VRC.PackageManagement;
 
 public class VRCSocketClient
 {

     private static UnityWindowWebSocketClient _client = null;
     
     static VRCSocketClient ()
     {
     }

     [InitializeOnLoadMethod]
     private static async Task Initialize()
     {
         if (_client != null)
         {
             try
             {
                 await _client.Disconnect();
             }
             catch (Exception e)
             {
                 Debug.LogError($"Failed to disconnect WebSocket client. Error: {e.Message}");
             }
         }
         
         // Websocket client
         _client = new UnityWindowWebSocketClient();
     }
 }