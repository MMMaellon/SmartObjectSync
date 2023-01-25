
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;


namespace MMMaellon
{
    [CustomEditor(typeof(BoneAttachmentState))]

    public class BoneAttachmentStateEditor : Editor
    {
        void OnEnable()
        {
            if(target)
                target.hideFlags = SmartObjectSyncEditor.hideHelperComponents ? HideFlags.HideInInspector : HideFlags.None;
        }
    }
}

#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BoneAttachmentState : SmartObjectSyncState
    {

        void Start()
        {
            InterpolateOnOwner = false;
            InterpolateAfterInterpolationPeriod = false;
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

        }
        public override void Interpolate(float interpolation)
        {

        }
        public override void OnInterpolationEnd()
        {

        }

        public override void OnSmartObjectSerialize()
        {

        }
    }
}