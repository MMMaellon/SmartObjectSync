
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
                    Component.DestroyImmediate(helper);
                    return;//return early to avoid bugs with drawing the gui
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
        // [System.NonSerialized]
        // public int queuedPhysicsEvent = -1;
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

        // public void FixedUpdate()
        // {
        //     if (!sync || !sync.IsLocalOwner())
        //     {
        //         queuedPhysicsEvent = -1;
        //         return;
        //     }
        //     if (queuedPhysicsEvent >= 0 && sync._state == queuedPhysicsEvent)
        //     {
        //         sync.state = sync.state;
        //     }
        //     queuedPhysicsEvent = -1;
        // }

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