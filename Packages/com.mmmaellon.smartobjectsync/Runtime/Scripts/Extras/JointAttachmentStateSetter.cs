
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.SmartObjectSyncExtra
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(JointAttachmentState))]
    public class JointAttachmentStateSetter : SmartObjectSyncListener
    {
        public float collisionCooldown = 0.25f;
        public bool attachOnCollision = true;
        public bool attachOnTrigger = true;
        public bool attachWhileHeldOrAttachedToPlayer = true;
        public bool attachWhileInCustomState = false;
        public Collider[] attachColliders;
        public Transform attachPoint;
        public Vector3 attachmentVector = Vector3.zero;
        public Rigidbody targetRigid;
        float lastDetach = -1001f;

        [System.NonSerialized]
        public JointAttachmentState jointState;
        public void Start()
        {
            jointState = GetComponent<JointAttachmentState>();
            jointState.sync.AddListener(this);
        }

        public void OnCollisionStay(Collision collision)
        {
            if (lastDetach + collisionCooldown > Time.timeSinceLevelLoad)
            {
                return;
            }
            if (!jointState.sync.IsLocalOwner() || !Utilities.IsValid(collision.collider))
            {
                return;
            }
            if (!attachOnCollision || (jointState.sync.IsAttachedToPlayer() && !attachWhileHeldOrAttachedToPlayer) || (Utilities.IsValid(jointState.sync.customState) && !attachWhileInCustomState))
            {
                return;
            }
            if (jointState.IsActiveState() && jointState.parentRigid == targetRigid)
            {
                //we're already attached
                return;
            }
            foreach (Collider collider in attachColliders)
            {
                if (collider == collision.collider)
                {
                    jointState.Attach(targetRigid);
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
            if (!jointState.sync.IsLocalOwner() || !Utilities.IsValid(other))
            {
                return;
            }
            if (!attachOnTrigger || (jointState.sync.IsAttachedToPlayer() && !attachWhileHeldOrAttachedToPlayer) || (Utilities.IsValid(jointState.sync.customState) && !attachWhileInCustomState))
            {
                return;
            }
            if (jointState.IsActiveState() && jointState.parentRigid == targetRigid)
            {
                //we're already attached
                return;
            }
            foreach (Collider collider in attachColliders)
            {
                if (collider == other)
                {
                    jointState.Attach(targetRigid);
                    return;
                }
            }
        }

        // public Vector3 CalcPos()
        // {
        //     return Quaternion.Inverse(jointState.parentRot) * (attachPoint.position - jointState.parentPos);
        // }
        // public Quaternion CalcRot()
        // {
        //     return Quaternion.Inverse(jointState.parentRot) * attachPoint.rotation;
        // }
        public void Interpolate(float interpolation)
        {
            if (interpolation < 0.5){
                transform.position = jointState.sync.HermiteInterpolatePosition(jointState.startPos, Vector3.zero, attachPoint.position + attachPoint.rotation * attachmentVector, Vector3.zero, interpolation * 2);
                transform.rotation = jointState.sync.HermiteInterpolateRotation(jointState.startRot, Vector3.zero, attachPoint.rotation, Vector3.zero, interpolation * 2);
            } else {
                transform.position = jointState.sync.HermiteInterpolatePosition(attachPoint.position + attachPoint.rotation * attachmentVector, Vector3.zero, attachPoint.position, Vector3.zero, (interpolation - 0.5f) * 2f);
                transform.rotation = attachPoint.rotation;
            }
            jointState.sync.rigid.velocity = Vector3.zero;
            jointState.sync.rigid.angularVelocity = Vector3.zero;
        }

        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {
            if (oldState == (jointState.stateID + SmartObjectSync.STATE_CUSTOM) && !jointState.IsActiveState())
            {
                lastDetach = Time.timeSinceLevelLoad;
            }
        }
        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {
            
        }
    }
}