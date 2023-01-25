#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UnityEngine;
using UdonSharpEditor;
using System.Collections.Immutable;

namespace MMMaellon
{
    [CustomEditor(typeof(SmartObjectSyncExtension), true), CanEditMultipleObjects]

    public class SmartObjectSyncExtensionEditor : Editor
    {
        public static void SetupSmartObjectSyncExtension(SmartObjectSync sync)
        {
            if (sync)
            {
                SmartObjectSyncEditor.SetupExtensions(sync);
            }
            else
            {
                
                Debug.LogFormat("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
            }
        }


        public static void SetupSelectedSmartObjectSyncExtensions()
        {
            bool syncFound = false;
            foreach (SmartObjectSync sync in Selection.GetFiltered<SmartObjectSync>(SelectionMode.Editable))
            {
                syncFound = true;
                SetupSmartObjectSyncExtension(sync);
            }

            if (!syncFound)
            {
                Debug.LogFormat("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
            }
        }

        public static void SelectedSmartObjectSyncExtensionsAddSync()
        {
            bool extensionFound = false;
            foreach (SmartObjectSyncExtension extension in Selection.GetFiltered<SmartObjectSyncExtension>(SelectionMode.Editable))
            {
                if (extension.GetComponent<SmartObjectSync>() == null)
                {
                    extensionFound = true;
                    extension.gameObject.AddUdonSharpComponent<SmartObjectSync>();
                }
            }

            if (!extensionFound)
            {
                Debug.LogFormat("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
            }
        }

        public override void OnInspectorGUI()
        {
            int extensionCount = 0;
            int setupCount = 0;
            int syncCount = 0;
            foreach (SmartObjectSyncExtension extension in Selection.GetFiltered<SmartObjectSyncExtension>(SelectionMode.Editable))
            {
                if (extension)
                {
                    extensionCount++;
                    if (extension.GetComponent<SmartObjectSync>() == null)
                    {
                        syncCount++;
                    }
                    else if ((extension.sync != extension.GetComponent<SmartObjectSync>()) || (extension.sync != null && extension.sync.GetExtension(extension.state) != extension))
                    {
                        setupCount++;
                    }
                }
            }
            if (setupCount > 0 || syncCount > 0)
            {
                if (syncCount > 0)
                {
                    EditorGUILayout.HelpBox(@"Extension requires a SmartObjectSync component", MessageType.Warning);
                    if (GUILayout.Button(new GUIContent("Add SmartObjectSync")))
                    {
                        SetupSelectedSmartObjectSyncExtensions();
                    }
                }
                if (setupCount > 0)
                {
                    EditorGUILayout.HelpBox(@"Extension not yet setup", MessageType.Warning);
                    if (GUILayout.Button(new GUIContent("Extension Setup")))
                    {
                        SetupSelectedSmartObjectSyncExtensions();
                    }
                }
            }
            EditorGUILayout.Space();
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}

#endif
