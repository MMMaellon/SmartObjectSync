
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
        [HideInInspector]
        public bool SetupRan = false;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public virtual void Reset()
        {
            if (!SetupRan)
            {
                SmartObjectSyncEditor.SetupStates(GetComponent<SmartObjectSync>());
            }
            SetupRan = true;
        }
#endif
        [HideInInspector]
        public int stateID;
        [HideInInspector]
        public SmartObjectSync sync;

        public virtual void EnterState()
        {
            if (Utilities.IsValid(sync))
            {
                if (!sync.IsLocalOwner())
                {
                    Networking.SetOwner(Networking.LocalPlayer, sync.gameObject);
                }
                sync.state = (stateID + SmartObjectSync.STATE_CUSTOM);
            }
        }
        public virtual void ExitState()
        {
            if (sync)
            {
                if (!sync.IsLocalOwner())
                {
                    Networking.SetOwner(Networking.LocalPlayer, sync.gameObject);
                }
                sync.state = SmartObjectSync.STATE_FALLING;
            }
        }

        public bool IsActiveState()
        {
            return sync.customState == this;
        }

        public abstract void OnEnterState();

        public abstract void OnExitState();

        // Summary:
        //     The owner executes this command when serializing data to the other players
        //     You should override this function to set all the synced variables that other players need
        public abstract void OnSmartObjectSerialize();


        // Summary:
        //     Non-owners execute this command when they receive data from the owner and begin interpolating towards the synced data
        public abstract void OnInterpolationStart();

        // Summary:
        //     All players execute this command during the interpolation period. The interpolation period for owners is one frame
        //     the 'interpolation' parameter is a value between 0.0 and 1.0 representing how far along the interpolation period we are
        public abstract void Interpolate(float interpolation);

        // Summary:
        //     All players execute once thie at the end of the interpolation period
        //     Return true to extend the interpolation period by another frame
        //     Return false to end the interpolation period and disable the update loop for optimization
        public abstract bool OnInterpolationEnd();
    }
}