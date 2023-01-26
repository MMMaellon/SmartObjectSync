
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
        public override void OnInspectorGUI()
        {
            if (target && UdonSharpEditor.UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            base.OnInspectorGUI();
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

        public Vector3 endPos;
        public Quaternion endRot;

        public bool interpolationEnded = false;

        public void Start()
        {
        }

        public override void OnEnterState()
        {
            
        }

        public override void OnExitState()
        {
            
        }

        public override void OnInterpolationStart()
        {
            interpolationEnded = false;
            startPos = transform.position;
            startRot = transform.rotation;
            if (sync.rigid && !sync.rigid.isKinematic)
            {
                startVel = sync.rigid.velocity;
                startSpin = sync.rigid.angularVelocity;
            }
            else
            {
                startVel = Vector3.zero;
                startSpin = Vector3.zero;
            }
        }
        public override void Interpolate(float interpolation)
        {
            if (interpolationEnded || sync.IsLocalOwner())
            {
                return;
            }
            transform.position = sync.HermiteInterpolatePosition(startPos, startVel, sync.pos, Vector3.zero, interpolation);
            transform.rotation = sync.HermiteInterpolateRotation(startRot, startSpin, sync.rot, Vector3.zero, interpolation);
            
            //because of weird floating point precision errors, it makes sense to note down what the "real" ending transform is
            endPos = transform.position;
            endRot = transform.rotation;
        }

        public override bool OnInterpolationEnd()
        {
            interpolationEnded = true;
            if (sync.IsLocalOwner() || sync.rigid == null || sync.rigid.isKinematic || sync.rigid.IsSleeping())
            {
                return false;
            }

            sync._print("is sleeping: " + sync.rigid.IsSleeping());

            //only update positions if we're not already where we should be
            //if there's a frame where we don't set the position, there's a possibility of the rigidbody falling asleep which we want
            if (ObjectMoved())
            {
                transform.position = sync.pos;
                transform.rotation = sync.rot;
                endPos = transform.position;
                endRot = transform.rotation;
                sync.rigid.velocity = Vector3.zero;
                sync.rigid.angularVelocity = Vector3.zero;
            }
            sync.rigid.Sleep();
            return true;
        }

        public override void OnSmartObjectSerialize()
        {
            sync.pos = transform.position;
            sync.rot = transform.rotation;
            sync.vel = Vector3.zero;
            sync.spin = Vector3.zero;
        }

        public bool ObjectMoved()
        {
            return endPos != transform.position || endRot != transform.rotation;
        }
    }
}