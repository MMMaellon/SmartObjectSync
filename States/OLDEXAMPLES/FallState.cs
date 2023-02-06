
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


// #if !COMPILER_UDONSHARP && UNITY_EDITOR
// using UnityEditor;


// namespace MMMaellon
// {
//     [CustomEditor(typeof(FallState)), CanEditMultipleObjects]

//     public class FallStateEditor : SmartObjectSyncStateEditor
//     {
//         public void OnEnable()
//         {
//             foreach (var target in targets)
//             {
//                 var state = target as FallState;
//                 if (state && (state.sync == null || state.sync.states[state.stateID] != state))
//                 {
//                     Component.DestroyImmediate(state);
//                     return;
//                 }
//                 target.hideFlags = SmartObjectSyncEditor.hideHelperComponentsAndNoErrors ? HideFlags.HideInInspector : HideFlags.None;
//             }
//             base.OnInspectorGUI();
//         }
//         public override void OnInspectorGUI()
//         {
//             OnEnable();
//             base.OnInspectorGUI();
//         }
//     }
// }

// #endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FallState : SmartObjectSyncState
    {

        public Vector3 startPos;
        public Quaternion startRot;
        public Vector3 startVel;
        public Vector3 startSpin;

        void Start()
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
            startPos = transform.position;
            startRot = transform.rotation;
            startVel = sync.rigid.velocity;
            startSpin = sync.rigid.angularVelocity;
        }
        public override void Interpolate(float interpolation)
        {
            if (sync.IsLocalOwner())
            {
                if (transform.position.y <= sync.respawnHeight)
                {
                    sync.Respawn();
                }
                return;
            }
            
            if (sync.rigid && !sync.rigid.isKinematic && sync.rigid.useGravity)
            {
                transform.position = sync.HermiteInterpolatePosition(startPos, startVel, sync.pos, startVel + Physics.gravity * sync.lerpTime, interpolation);
                transform.rotation = sync.HermiteInterpolateRotation(startRot, startSpin, sync.rot, startSpin, interpolation);
            } else
            {
                transform.position = sync.HermiteInterpolatePosition(startPos, startVel, sync.pos, startVel, interpolation);
                transform.rotation = sync.HermiteInterpolateRotation(startRot, startSpin, sync.rot, startSpin, interpolation);
            }
        }
        public override bool OnInterpolationEnd()
        {
            if (sync.IsLocalOwner())
            {
                if (sync.rigid == null || sync.rigid.isKinematic || sync.rigid.IsSleeping())
                {
                    //wait around for the rigidbody to fall asleep
                    sync.state = SmartObjectSync.STATE_SLEEPING;
                } else if (NonGravitationalAcceleration())
                {
                    //some force other than gravity is acting upon our object
                    sync.RequestSerialization();
                }
                //returning true means we extend the interpolation period
                return true;
            }

            if (sync.rigid)
            {
                sync.rigid.velocity = sync.vel;
                sync.rigid.angularVelocity = sync.spin;
            }

            return false;
        }

        public override void OnSmartObjectSerialize()
        {
            sync.pos = transform.position;
            sync.rot = transform.rotation;
            if (sync.rigid && !sync.rigid.isKinematic)
            {
                sync.vel = sync.rigid.velocity;
                sync.spin = sync.rigid.angularVelocity;
            } else
            {
                sync.vel = Vector3.zero;
                sync.spin = Vector3.zero;
            }
        }


        Vector3 gravityLessVelocity;
        Vector3 gravityProjection;
        Vector3 gravityProjectionSynced;
        public bool NonGravitationalAcceleration()
        {
            if (!sync.rigid)
            {
                return false;
            }
            //returns true of object's velocity changed along an axis other than gravity's
            //this will let us know if the object stayed in projectile motion or if another force acted upon it
            gravityProjection = Vector3.Project(sync.rigid.velocity, Physics.gravity);
            gravityProjectionSynced = Vector3.Project(sync.vel, Physics.gravity);

            if (Vector3.Dot(gravityProjection, Physics.gravity) < Vector3.Dot(gravityProjectionSynced, Physics.gravity))
            {
                //This means that the object was moving against the gravity
                //there is definitely a non-gravitational velocity change
                return true;
            }

            //we know that the object acelerated along the gravity vector,
            //but if it also acelerated on another axis then another force acted upon it
            //here we remove the influence of gravity and compare the velocity with the last synced velocity
            gravityLessVelocity = sync.vel - gravityProjectionSynced;
            return Vector3.Distance(gravityLessVelocity, sync.rigid.velocity - gravityProjection) > 0.001f;
        }
    }
}