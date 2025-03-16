
#if UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDKBase;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UdonSharp.Internal;
using VRC.Udon;

namespace MMMaellon
{
    [InitializeOnLoad]
    public class SmartObjectSyncDefineManager
    {
        static SmartObjectSyncDefineManager()
        {
            AddDefinesIfMissing(EditorUserBuildSettings.selectedBuildTargetGroup, new string[] { "MMM_SMARTOBJECTSYNC", "MMM_SMARTOBJECTSYNC_V3" });
        }

        private static void AddDefinesIfMissing(BuildTargetGroup buildTarget, params string[] newDefines)
        {
            bool definesChanged = false;
            string existingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
            HashSet<string> defineSet = new HashSet<string>();

            if (existingDefines.Length > 0)
            {
                defineSet = new HashSet<string>(existingDefines.Split(';'));
            }

            foreach (string newDefine in newDefines)
            {
                if (defineSet.Add(newDefine))
                {
                    definesChanged = true;
                }
            }

            if (definesChanged)
            {
                string finalDefineString = string.Join(";", defineSet.ToArray());
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, finalDefineString);
                Debug.LogFormat("Set Scripting Define Symbols for selected build target ({0}) to: {1}", buildTarget.ToString(), finalDefineString);
            }
        }
    }
}
#endif
