
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.SmartObjectSyncExtra
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(ChildAttachmentState))]
    public class ChildAttachmentStateSetter : SmartObjectSyncListener
    {
        public float collisionCooldown = 0.25f;
        public bool attachOnCollision = true;
        public bool attachOnTrigger = true;
        public bool attachWhileHeldOrAttachedToPlayer = true;
        public bool attachWhileInCustomState = false;
        public bool startInState = false;
        public Collider[] attachColliders;
        public Transform targetParent;
        float lastDetach = -1001f;

        [System.NonSerialized]
        public ChildAttachmentState child;
        public void Start()
        {
            child = GetComponent<ChildAttachmentState>();
            child.sync.AddListener(this);
            if (startInState && child.sync.IsLocalOwner())
            {
                Attach();
            }
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
            if (child.IsActiveState() && collision.gameObject.transform == child.parentTransform)
            {
                //we're already attached
                return;
            }
            foreach (Collider collider in attachColliders)
            {
                if (collider == collision.collider)
                {
                    Attach();
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
            foreach (Collider collider in attachColliders)
            {
                if (collider == other)
                {
                    Attach();
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
                if (startInState && Utilities.IsValid(newOwner) && newOwner.isLocal)
                {
                    Attach();
                }
            }
        }

        public void Attach()
        {
            child.Attach(targetParent);
            child.sync.pos = Vector3.zero;
            child.sync.rot = Quaternion.identity;
        }
    }
}