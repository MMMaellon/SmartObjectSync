
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


// #if !COMPILER_UDONSHARP && UNITY_EDITOR
// using UnityEditor;


// namespace MMMaellon
// {
//     [CustomEditor(typeof(BoneAttachmentState)), CanEditMultipleObjects]

//     public class BoneAttachmentStateEditor : SmartObjectSyncStateEditor
//     {
//         public void OnEnable()
//         {
//             foreach (var target in targets)
//             {
//                 var state = target as BoneAttachmentState;
//                 if (state && (state.sync == null))
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
    public class BoneAttachmentState : GenericAttachmentState
    {
        public bool hasBones = false;
        public HumanBodyBones bone;

        public override void OnEnterState()
        {
            bone = (HumanBodyBones) (-1 - sync.state);
        }


        public override void OnInterpolationStart()
        {
            //if the avatar we're wearing doesn't have the bones required, fallback to attach to playspace
            if (sync.IsLocalOwner())
            {
                CalcParentTransform();
                if (!hasBones)
                {
                    sync._printErr("Avatar is missing the correct bone. Falling back to playspace attachment.");
                    sync.state = SmartObjectSync.STATE_ATTACHED_TO_PLAYSPACE;
                    return;
                }
            }
            base.OnInterpolationStart();
        }
        public override void CalcParentTransform()
        {
            if (Utilities.IsValid(sync.owner)){
                parentPos = sync.owner.GetBonePosition(bone);
                parentRot = sync.owner.GetBoneRotation(bone);
                hasBones = parentPos != Vector3.zero;
                parentPos = hasBones ? parentPos : sync.owner.GetPosition();
                parentRot = hasBones ? parentRot : sync.owner.GetRotation();
            }
        }
    }
}