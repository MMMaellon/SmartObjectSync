using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using VRC.SDKBase.Editor.Source.Helpers;

namespace VRC.SDK3.Editor
{
    [InitializeOnLoad]
    public class SDK3ImportFix
    {
        private const string worldsReimportedKey = "WORLDS_REIMPORTED";

        static SDK3ImportFix()
        {
            // Skip if we've already checked for the canary file during this Editor Session
            if (!SessionState.GetBool(worldsReimportedKey, false))
            {
                // Check for canary file in Library - package probably needs a reimport after a Library wipe
                string canaryFilePath = Path.Combine("Library", worldsReimportedKey);
                if (File.Exists(canaryFilePath))
                {
                    SessionState.SetBool(worldsReimportedKey, true);
                }
                else
                {
#pragma warning disable 4014
                    ReloadSDK();
#pragma warning restore 4014
                    File.WriteAllText(canaryFilePath, worldsReimportedKey);
                }
            }
        }

        public static async Task ReloadSDK()
        {
            // Set session key to true, limiting the reload to one run per session
            SessionState.SetBool(worldsReimportedKey, true);
            
            //Wait for project to finish compiling
            while (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                await Task.Delay(250);
            }
            
            ReloadUtil.ReloadSDK();
        }
    }
}