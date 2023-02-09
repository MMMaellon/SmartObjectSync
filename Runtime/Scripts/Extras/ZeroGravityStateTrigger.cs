using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon
{
    public class ZeroGravityStateTrigger : UdonSharpBehaviour
    {
        public void OnTriggerStay(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            ZeroGravityState otherState = other.GetComponent<ZeroGravityState>();

            if (Utilities.IsValid(otherState) && otherState.sync.IsLocalOwner() && !otherState.sync.IsAttachedToPlayer() && otherState.sync.state < SmartObjectSync.STATE_CUSTOM)
            {
                otherState.EnterState();
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            ZeroGravityState otherState = other.GetComponent<ZeroGravityState>();

            if (Utilities.IsValid(otherState) && otherState.sync.IsLocalOwner() && otherState.IsActiveState())
            {
                otherState.ExitState();
            }
        }
    }
}