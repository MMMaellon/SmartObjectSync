
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


// #if !COMPILER_UDONSHARP && UNITY_EDITOR
// using UnityEditor;


// namespace MMMaellon
// {
//     [CustomEditor(typeof(LeftHandHeldState)), CanEditMultipleObjects]

//     public class LeftHandHeldStateEditor : SmartObjectSyncStateEditor
//     {
//         public void OnEnable()
//         {
//             foreach (var target in targets)
//             {
//                 var state = target as LeftHandHeldState;
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
    public class LeftHandHeldState : BoneAttachmentState
    {
        public override void OnEnterState()
        {
            bone = HumanBodyBones.LeftHand;
        }
        public override void Interpolate(float interpolation)
        {
            //let the VRC_pickup script handle transforms for the local owner
            //only reposition it for non-owners
            if (!sync.IsLocalOwner())
            {
                base.Interpolate(interpolation);
            }
            else
            {
                CalcParentTransform();
            }
        }
    }
}
