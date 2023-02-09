#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UnityEngine;
using UdonSharpEditor;
using System.Collections.Immutable;

namespace MMMaellon
{
    [CustomEditor(typeof(SmartObjectSyncState), true), CanEditMultipleObjects]

    public class SmartObjectSyncStateEditor : Editor
    {
        public static void SetupSmartObjectSyncState(SmartObjectSync sync)
        {
            if (sync)
            {
                SmartObjectSyncEditor.SetupStates(sync);
            }
            else
            {
                
                Debug.LogFormat("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
            }
        }


        public static void SetupSelectedSmartObjectSyncStates()
        {
            bool syncFound = false;
            foreach (SmartObjectSync sync in Selection.GetFiltered<SmartObjectSync>(SelectionMode.Editable))
            {
                syncFound = true;
                SetupSmartObjectSyncState(sync);
            }

            if (!syncFound)
            {
                Debug.LogFormat("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
            }
        }

        public static void SelectedSmartObjectSyncStatesAddSync()
        {
            bool StateFound = false;
            foreach (SmartObjectSyncState State in Selection.GetFiltered<SmartObjectSyncState>(SelectionMode.Editable))
            {
                if (State.GetComponent<SmartObjectSync>() == null)
                {
                    StateFound = true;
                    State.gameObject.AddUdonSharpComponent<SmartObjectSync>();
                }
            }

            if (!StateFound)
            {
                Debug.LogFormat("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
            }
        }

        public override void OnInspectorGUI()
        {
            int StateCount = 0;
            int setupCount = 0;
            int syncCount = 0;
            foreach (SmartObjectSyncState State in Selection.GetFiltered<SmartObjectSyncState>(SelectionMode.Editable))
            {
                if (State)
                {
                    StateCount++;
                }
            }
            if (setupCount > 0 || syncCount > 0)
            {
                if (syncCount > 0)
                {
                    EditorGUILayout.HelpBox(@"State requires a SmartObjectSync component", MessageType.Warning);
                    if (GUILayout.Button(new GUIContent("Add SmartObjectSync")))
                    {
                        SetupSelectedSmartObjectSyncStates();
                    }
                }
                if (setupCount > 0)
                {
                    EditorGUILayout.HelpBox(@"State not yet set up", MessageType.Warning);
                    if (GUILayout.Button(new GUIContent("State Setup")))
                    {
                        SetupSelectedSmartObjectSyncStates();
                    }
                }
            }
            if (target == null || target.hideFlags == HideFlags.HideInInspector)
            {
                return;
            }
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            base.OnInspectorGUI();
        }
    }
}

#endif
