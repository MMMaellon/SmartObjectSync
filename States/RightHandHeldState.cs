
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;


namespace MMMaellon
{
    [CustomEditor(typeof(RightHandHeldState)), CanEditMultipleObjects]

    public class RightHandHeldStateEditor : SmartObjectSyncStateEditor
    {
        public void OnEnable()
        {
            foreach (var target in targets)
            {
                var state = target as RightHandHeldState;
                if (state && (state.sync == null || state.sync.states[state.stateID] != state))
                {
                    Component.DestroyImmediate(state);
                    return;
                }
                target.hideFlags = SmartObjectSyncEditor.hideHelperComponentsAndNoErrors ? HideFlags.HideInInspector : HideFlags.None;
            }
            base.OnInspectorGUI();
        }
        public override void OnInspectorGUI()
        {
            OnEnable();
            base.OnInspectorGUI();
        }
    }
}

#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class RightHandHeldState : BoneAttachmentState
    {
        public override void OnEnterState()
        {
            bone = HumanBodyBones.RightHand;
        }
        public override void Interpolate(float interpolation)
        {
            //let the VRC_pickup script handle transforms for the local owner
            //only reposition it for non-owners
            //we need to keep the parent transform up to date though
            if (!sync.IsLocalOwner())
            {
                base.Interpolate(interpolation);
            } else
            {
                CalcParentTransform();
            }
        }
    }
}
