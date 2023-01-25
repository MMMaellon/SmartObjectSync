
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public abstract class SmartObjectSyncExtension : UdonSharpBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            SmartObjectSyncExtensionEditor.SetupSmartObjectSyncExtension(GetComponent<SmartObjectSync>());
        }
#endif
        [HideInInspector]
        public int state;
        [HideInInspector]
        public SmartObjectSync sync;

        public bool RunEveryFrameOnOwner = false;
        public bool RunEveryFrameOnNonOwner = false;
        public bool IgnoreLerping = false;



        public Vector3 _CalcPosition()
        {
            return CalcPosition(Utilities.IsValid(sync.owner) && sync.owner.isLocal);
        }
        public Quaternion _CalcRotation()
        {
            return CalcRotation(Utilities.IsValid(sync.owner) && sync.owner.isLocal);
        }

        public void _OnActivate()
        {
            OnActivate(Utilities.IsValid(sync.owner) && sync.owner.isLocal);
        }
        public void _OnDeactivate()
        {
            OnDeactivate(Utilities.IsValid(sync.owner) && sync.owner.isLocal);
        }

        public void Activate()
        {
            sync._print("Activate");
            if (sync && Utilities.IsValid(sync.owner))
            {
                if (!sync.owner.isLocal)
                {
                    Networking.SetOwner(Networking.LocalPlayer, sync.gameObject);
                }
                sync._print("Activate + " + state);
                sync.state = state;
            }
        }

        public void Deactivate()
        {
            if (sync && Utilities.IsValid(sync.owner))
            {
                if (!sync.owner.isLocal)
                {
                    Networking.SetOwner(Networking.LocalPlayer, sync.gameObject);
                }
                sync.state = SmartObjectSync.STATE_LERPING;
            }
        }

        public virtual void OnActivate(bool IsOwner)
        {

        }

        public virtual void OnDeactivate(bool IsOwner)
        {

        }

        /*
        Return what the global position of the object should be.
        */
        public virtual Vector3 CalcPosition(bool IsOwner)
        {
            return transform.position;
        }
        /*
        Return what the global rotation of the object should be.
        */
        public virtual Quaternion CalcRotation(bool IsOwner)
        {
            return transform.rotation;
        }
    }
}
