
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

        int randomSeed = 0;
        int randomCount = 0;
        public void Start()
        {
            randomSeed = Random.Range(0, 10);
        }
        public void Update()
        {
            if (!sync || !Utilities.IsValid(sync.owner))
            {
                // Debug.LogWarning(name + " is missing sync or sync owner");
                enabled = false;
                return;
            }

            if (sync.interpolationStartTime < 0)
            {
                // sync._printErr("waiting for first sync" + sync.interpolationStartTime);
                //if we haven't received data yet, do nothing. Otherwise this will move the object to the origin
                enabled = false;
                return;
            }

            if (!disableRequested)
            {
                sync.Interpolate();
            }
            if (serializeRequested && !Networking.IsClogged)
            {
                if (sync.state == SmartObjectSync.STATE_SLEEPING)
                {
                    //prioritize sleep serialization because it will settle our world faster
                    sync.RequestSerialization();
                } else
                {
                    //randomize it, so we stagger the synchronizations
                    randomCount = (randomCount + 1) % 10;
                    if (randomCount == randomSeed)
                    {
                        sync.RequestSerialization();
                    }
                }
            }
        }

        // public void OnEnable()
        // {
        //     sync._print("Helper OnEnable");
        // }

        // public void OnDisable()
        // {
        //     sync._print("Helper OnDisable");
        // }

        [System.NonSerialized]
        public bool disableRequested = false;
        public void Enable()
        {
            disableRequested = false;
            enabled = true;
        }

        public void Disable()
        {
            if (serializeRequested)
            {
                disableRequested = true;
            } else
            {
                enabled = false;
            }
        }

        public bool IsEnabled()
        {
            return enabled && !disableRequested;
        }

        [System.NonSerialized]
        public bool serializeRequested = false;
        public void OnSerializationFailure()
        {
            sync._printErr("OnSerializationFailure");
            serializeRequested = true;
            enabled = true;//we're just going to wait around for the network to allow us to synchronize again
        }
        public void OnSerializationSuccess()
        {
            serializeRequested = false;
        }
    }
}