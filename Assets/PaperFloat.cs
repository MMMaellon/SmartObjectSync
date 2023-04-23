
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Generic;
#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(SmartObjectSync))]
    public class PaperFloat : SmartObjectSyncListener
    {
        public Vector3 paperNormal = Vector3.up;
        public float maxMovementAgainstNormal = 0.5f;
        public float airResistance = 0.25f;
        [System.NonSerialized]
        public bool _loop;
        bool loopRequested = false;
        public bool loop{
            get => _loop;
            set
            {
                _loop = value;
                if (!loopRequested && value)
                {
                    if (Utilities.IsValid(sync))
                    {
                        lastVelocity = sync.rigid.velocity;
                        lastAngularVelocity = sync.rigid.angularVelocity;
                    } else
                    {
                        lastVelocity = Vector3.zero;
                        lastAngularVelocity = Vector3.zero;
                    }
                    loopRequested = true;
                    SendCustomEventDelayedFrames(nameof(UpdateLoop), 0);
                }
            }
        }
        Vector3 lastVelocity;
        Vector3 lastAngularVelocity;
        Vector3 againstVelocity;
        Vector3 withVelocity;
        Vector3 transformNormal;
        public void UpdateLoop()
        {
            loopRequested = false;
            if (!loop)
            {
                return;
            }
            loopRequested = true;
            SendCustomEventDelayedFrames(nameof(UpdateLoop), 0);
            if (!Utilities.IsValid(sync))
            {
                return;
            }
            transformNormal = transform.TransformVector(paperNormal);
            withVelocity = Vector3.ProjectOnPlane(sync.rigid.velocity, transformNormal);
            againstVelocity = Vector3.Project(sync.rigid.velocity, transformNormal);
            if (againstVelocity.magnitude > maxMovementAgainstNormal)
            {
                sync.rigid.velocity = withVelocity + againstVelocity.normalized * maxMovementAgainstNormal;
            } else
            {
                sync.rigid.velocity = withVelocity + againstVelocity;
            }

            sync.rigid.AddTorque(Quaternion.FromToRotation(transformNormal, sync.rigid.velocity).eulerAngles * Vector3.Dot(transformNormal.normalized, sync.rigid.velocity.normalized) * airResistance * Time.deltaTime);
        }

        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {
            
        }

        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {
            loop = newState == SmartObjectSync.STATE_FALLING || newState == SmartObjectSync.STATE_INTERPOLATING || newState == SmartObjectSync.STATE_TELEPORTING;
        }

        public SmartObjectSync sync;
        void Start()
        {
            if (!Utilities.IsValid(sync))
            {
                sync = GetComponent<SmartObjectSync>();
            }
            sync.AddListener(this);
            loop = sync.state == SmartObjectSync.STATE_FALLING || sync.state == SmartObjectSync.STATE_INTERPOLATING || sync.state == SmartObjectSync.STATE_TELEPORTING;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset(){
            SerializedObject obj = new SerializedObject(this);
            obj.FindProperty("sync").objectReferenceValue = GetComponent<SmartObjectSync>();
            obj.ApplyModifiedProperties();
        }
#endif
    }
}
