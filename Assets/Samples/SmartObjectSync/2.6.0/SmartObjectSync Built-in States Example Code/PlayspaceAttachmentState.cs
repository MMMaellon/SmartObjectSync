
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


// #if !COMPILER_UDONSHARP && UNITY_EDITOR
// using UnityEditor;


// namespace MMMaellon
// {
//     [CustomEditor(typeof(PlayspaceAttachmentState)), CanEditMultipleObjects]

//     public class PlayspaceAttachmentStateEditor : SmartObjectSyncStateEditor
//     {
//         public void OnEnable()
//         {
//             foreach (var target in targets)
//             {
//                 var state = target as PlayspaceAttachmentState;
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
    public class PlayspaceAttachmentState : GenericAttachmentState
    {
        public override void CalcParentTransform()
        {
            if (Utilities.IsValid(sync.owner))
            {
                parentPos = sync.owner.GetPosition();
                parentRot = sync.owner.GetRotation();
            }
        }
    }
}
