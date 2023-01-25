
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;


namespace MMMaellon
{
    [CustomEditor(typeof(SleepState))]

    public class SleepStateEditor : Editor
    {
        void OnEnable()
        {
            if (target)
                target.hideFlags = SmartObjectSyncEditor.hideHelperComponents ? HideFlags.HideInInspector : HideFlags.None;
        }
    }
}

#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SleepState : SmartObjectSyncState
    {
        public Vector3 startPos;
        public Quaternion startRot;
        public Vector3 startVel;
        public Vector3 startSpin;

        public void Start()
        {
            InterpolateOnOwner = false;
            InterpolateAfterInterpolationPeriod = true;
            ExitStateOnOwnershipTransfer = false;
        }

        public override void OnEnterState()
        {
            
        }

        public override void OnExitState()
        {
            
        }

        public override void OnInterpolationStart()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            if (sync.rigid && !sync.rigid.isKinematic)
            {
                startVel = sync.rigid.velocity;
                startSpin = sync.rigid.angularVelocity;
            } else
            {
                startVel = Vector3.zero;
                startSpin = Vector3.zero;
            }
        }
        public override void Interpolate(float interpolation)
        {
            transform.position = sync.HermiteInterpolatePosition(startPos, startVel, sync.pos, sync.vel, interpolation);
            transform.rotation = sync.HermiteInterpolateRotation(startRot, startSpin, sync.rot, sync.spin, interpolation);
        }

        public override void OnInterpolationEnd()
        {
            startPos = sync.pos;
            startRot = sync.rot;
            if (sync.rigid && !sync.rigid.isKinematic)
            {
                sync.rigid.velocity = Vector3.zero;
                sync.rigid.angularVelocity = Vector3.zero;
            }
        }

        public override void OnSmartObjectSerialize()
        {
            sync.pos = transform.position;
            sync.rot = transform.rotation;
            sync.vel = Vector3.zero;
            sync.spin = Vector3.zero;
        }
    }
}