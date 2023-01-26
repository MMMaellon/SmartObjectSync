
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;


namespace MMMaellon
{
    [CustomEditor(typeof(RightHandHeldState))]

    public class RightHandHeldStateEditor : Editor
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
    public class RightHandHeldState : SmartObjectSyncState
    {

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

        }
        public override void Interpolate(float interpolation)
        {

        }
        public override bool OnInterpolationEnd()
        {
            return false;
        }

        public override void OnSmartObjectSerialize()
        {

        }
    }
}
