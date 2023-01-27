
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Immutable;


namespace MMMaellon
{
    [CustomEditor(typeof(SmartObjectSyncHelper)), CanEditMultipleObjects]

    public class SmartObjectSyncHelperEditor : Editor
    {
        void OnEnable()
        {
            if (target)
            {
                target.hideFlags = HideFlags.NotEditable;
            }
        }
        public override void OnInspectorGUI()
        {
            foreach (var target in targets)
            {
                SmartObjectSyncHelper helper = target as SmartObjectSyncHelper;
                if (helper && (helper.sync == null || helper.sync.helper != helper))
                {
                    int deleteCount = 0;

                    foreach (SmartObjectSyncState state in helper.GetComponents<SmartObjectSyncState>())
                    {
                        if (state && (state.sync == null || (state.stateID < 0 && state.sync._bone_attached_state != state) || (state.sync.states[state.stateID] != state)))
                        {
                            deleteCount++;
                            Component.DestroyImmediate(state);
                        }
                    }
                    if (deleteCount == 0)
                    {
                        Component.DestroyImmediate(helper);
                        return;//return early to avoid bugs with drawing the gui
                    }
                }
            }

            if (target && UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            base.OnInspectorGUI();
        }
    }
}

#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SmartObjectSyncHelper : UdonSharpBehaviour
    {
        [HideInInspector]
        public SmartObjectSync sync;
        public void Update()
        {
            if (!sync || !Utilities.IsValid(sync.owner))
            {
                Debug.LogWarning(name + " is missing sync or sync owner");
                enabled = false;
                return;
            }

            if (sync.interpolationStartTime < 0)
            {
                sync._printErr("waiting for first sync" + sync.interpolationStartTime);
                //if we haven't received data yet, do nothing. Otherwise this will move the object to the origin
                enabled = false;
                return;
            }

            sync.Interpolate();
        }

        public void OnEnable()
        {
            if(sync){
                sync._print("Helper OnEnable");
            }
        }

        public void OnDisable()
        {
            if(sync){
                sync._print("Helper OnDisable");
            }
        }
    }
}