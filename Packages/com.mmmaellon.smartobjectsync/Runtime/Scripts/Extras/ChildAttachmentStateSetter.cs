
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.SmartObjectSyncExtra
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(ChildAttachmentState))]
    public class ChildAttachmentStateSetter : SmartObjectSyncListener
    {
        public float collisionCooldown = 0.1f;
        public bool attachOnCollision = true;
        public bool attachOnTrigger = true;
        public bool attachWhileHeldOrAttachedToPlayer = true;
        public bool attachWhileInCustomState = false;
        public Collider[] attachColliders;
        public Transform targetParent;
        float lastDetach = -1001f;
        public ChildAttachmentState child;
        public void Start()
        {
            if (!Utilities.IsValid(child))
            {
                child = GetComponent<ChildAttachmentState>();
            }
            child.sync.AddListener(this);
        }

        public void OnCollisionStay(Collision collision)
        {
            if (lastDetach + collisionCooldown > Time.timeSinceLevelLoad)
            {
                return;
            }
            if (!child.sync.IsLocalOwner() || !Utilities.IsValid(collision.collider))
            {
                return;
            }
            if (!attachOnCollision || (child.sync.IsAttachedToPlayer() && !attachWhileHeldOrAttachedToPlayer) || (Utilities.IsValid(child.sync.customState) && !attachWhileInCustomState))
            {
                return;
            }
            if (child.IsActiveState() && collision.transform == child.parentTransform)
            {
                //we're already attached
                return;
            }
            if (collision.transform == child.lastTransform && child.lastCollide + collisionCooldown > Time.timeSinceLevelLoad)
            {
                child.lastCollide = Time.timeSinceLevelLoad;
                return;
            }
            if(child.lastCollide + collisionCooldown > Time.timeSinceLevelLoad){
                return;
            }
            foreach (Collider collider in attachColliders)
            {
                if (collider == collision.collider)
                {
                    child.Attach(targetParent);
                    return;
                }
            }
        }

        public void OnTriggerStay(Collider other)
        {
            if (lastDetach + collisionCooldown > Time.timeSinceLevelLoad)
            {
                return;
            }
            if (!child.sync.IsLocalOwner() || !Utilities.IsValid(other))
            {
                return;
            }
            if (!attachOnTrigger || (child.sync.IsAttachedToPlayer() && !attachWhileHeldOrAttachedToPlayer) || (Utilities.IsValid(child.sync.customState) && !attachWhileInCustomState))
            {
                return;
            }
            if (child.IsActiveState() && other.transform == child.parentTransform)
            {
                //we're already attached
                return;
            }
            if (other.transform == child.lastTransform && child.lastCollide + collisionCooldown > Time.timeSinceLevelLoad)
            {
                child.lastCollide = Time.timeSinceLevelLoad;
                return;
            }
            if(child.lastCollide + collisionCooldown > Time.timeSinceLevelLoad){
                return;
            }
            foreach (Collider collider in attachColliders)
            {
                if (collider == other)
                {
                    child.Attach(targetParent);
                    return;
                }
            }
        }

        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {
            if (oldState == (child.stateID + SmartObjectSync.STATE_CUSTOM) && !child.IsActiveState())
            {
                lastDetach = Time.timeSinceLevelLoad;
            }
        }
        bool firstOwner = true;
        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {
            if (firstOwner)
            {
                firstOwner = false;
            }
        }
    }
}
