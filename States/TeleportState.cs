
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;


namespace MMMaellon
{
    [CustomEditor(typeof(TeleportState)), CanEditMultipleObjects]

    public class TeleportStateEditor : SmartObjectSyncStateEditor
    {
        public void OnEnable()
        {
            foreach (var target in targets)
            {
                var state = target as TeleportState;
                if (state && (state.sync == null || state.sync.states[state.stateID] != state))
                {
                    Component.DestroyImmediate(state);
                    return;
                }
                target.hideFlags = SmartObjectSyncEditor.hideHelperComponentsAndNoErrors ? HideFlags.HideInInspector : HideFlags.None;
            }
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
    public class TeleportState : SmartObjectSyncState
    {

        void Start()
        {
        }

        public override void OnEnterState()
        {
            //owner sets transforms on enter state to make it snappier
            //non-owners still wait until everything has been deserialized
            if (sync.IsLocalOwner())
            {
                transform.position = sync.pos;
                transform.rotation = sync.rot;
                if (sync.rigid && !sync.rigid.isKinematic)
                {
                    sync.rigid.velocity = sync.vel;
                    sync.rigid.rotation = sync.rot;
                }
            }
        }

        public override void OnExitState()
        {
            
        }


        public override void OnInterpolationStart()
        {
            //owner sets transforms on enter state to make it snappier
            //non-owners still wait until everything has been deserialized
            if (!sync.IsLocalOwner())
            {
                transform.position = sync.pos;
                transform.rotation = sync.rot;
                if (sync.rigid && !sync.rigid.isKinematic)
                {
                    sync.rigid.velocity = sync.vel;
                    sync.rigid.rotation = sync.rot;
                }
            }
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
            //all variables should already be set
        }
    }
}
