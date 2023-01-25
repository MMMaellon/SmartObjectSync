
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using JetBrains.Annotations;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class SmartObjectSyncState : UdonSharpBehaviour
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            SmartObjectSyncEditor.SetupStates(GetComponent<SmartObjectSync>());
        }
#endif
        [HideInInspector]
        public int stateID;
        [HideInInspector]
        public SmartObjectSync sync;

        public bool InterpolateOnOwner = false;
        public bool InterpolateAfterInterpolationPeriod = false;
        public bool ExitStateOnOwnershipTransfer = true;

        public void EnterState()
        {
            if (sync && Utilities.IsValid(sync.owner))
            {
                if (!sync.owner.isLocal)
                {
                    Networking.SetOwner(Networking.LocalPlayer, sync.gameObject);
                }
                sync.state = stateID;
            }
        }
        public void ExitState()
        {
            if (sync && Utilities.IsValid(sync.owner))
            {
                if (!sync.owner.isLocal)
                {
                    Networking.SetOwner(Networking.LocalPlayer, sync.gameObject);
                }
                sync.state = SmartObjectSync.STATE_FALLING;
            }
        }

        public abstract void OnEnterState();

        public abstract void OnExitState();

        // Summary:
        //     The owner executes this command when serializing data to the other players.
        //     You should override this function to set all the synced variables that other players need.
        public abstract void OnSmartObjectSerialize();

        /*
        Non-owners execute this function right when they receive data from the owner
        */
        public abstract void OnInterpolationStart();
        /*
        Return what the global rotation of the object should be.
        */
        public abstract void Interpolate(float interpolation);
        /*
        Return what the global rotation of the object should be.
        */
        public abstract void OnInterpolationEnd();
    }
}