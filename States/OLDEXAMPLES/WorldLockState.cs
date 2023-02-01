
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


// #if !COMPILER_UDONSHARP && UNITY_EDITOR
// using UnityEditor;


// namespace MMMaellon
// {
//     [CustomEditor(typeof(WorldLockState)), CanEditMultipleObjects]

//     public class WorldLockStateEditor : SmartObjectSyncStateEditor
//     {
//         public void OnEnable()
//         {
//             foreach (var target in targets)
//             {
//                 var state = target as WorldLockState;
//                 if (state && (state.sync == null || state.sync.states[state.stateID] != state))
//                 {
//                     Component.DestroyImmediate(state);
//                     return;
//                 }
//                 target.hideFlags = SmartObjectSyncEditor.hideHelperComponentsAndNoErrors ? HideFlags.HideInInspector : HideFlags.None;
//             }
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
    public class WorldLockState: SmartObjectSyncState
    {
        public Vector3 startPos;
        public Quaternion startRot;

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
        }
        public override void Interpolate(float interpolation)
        {
            transform.position = sync.HermiteInterpolatePosition(startPos, Vector3.zero, sync.pos, Vector3.zero, interpolation);
            transform.rotation = sync.HermiteInterpolateRotation(startRot, Vector3.zero, sync.rot, Vector3.zero, interpolation);
            if (sync.rigid)
            {
                sync.rigid.velocity = Vector3.zero;
                sync.rigid.angularVelocity = Vector3.zero;
            }
        }
        public override bool OnInterpolationEnd()
        {
            return true;
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