using UnityEngine;
using VRC.SDKBase;

namespace MMMaellon
{
    public class ZeroGravityState : SmartObjectSyncState
    {
        public override void OnEnterState()
        {
            //gravity off
            sync.rigid.useGravity = false;
        }

        public override void OnExitState()
        {
            //gravity on
            sync.rigid.useGravity = true;
        }

        public override void OnInterpolationStart()
        {
            if (sync.IsLocalOwner())
            {
                //on the local owner, we let physics control everything so we do nothing here
                return;
            }
            //take note of our current transforms and velocities
            sync.startPos = transform.position;
            sync.startRot = transform.rotation;
            sync.startVel = sync.rigid.velocity;
            sync.startSpin = sync.rigid.angularVelocity;
        }
        public override void Interpolate(float interpolation)
        {
            if (sync.IsLocalOwner())
            {
                //on the local owner, we let physics control everything so we do nothing here
                return;
            }
            //we smoothly interpolate between our current location and the synced location
            transform.position = sync.HermiteInterpolatePosition(sync.startPos, sync.startVel, sync.pos, sync.vel, interpolation);
            transform.rotation = sync.HermiteInterpolateRotation(sync.startRot, sync.startSpin, sync.rot, sync.spin, interpolation);
            sync.rigid.velocity = sync.vel;
            sync.rigid.angularVelocity = sync.spin;
        }

        public override bool OnInterpolationEnd()
        {
            //returning false means we don't want to run the loop every frame

            if (sync.IsLocalOwner())
            {
                //we keep the loop alive for the local owner so we know when to respawn
                if (transform.position.y < sync.respawnHeight)
                {
                    sync.Respawn();
                }
                return sync.rigid.velocity.y < 0;//only keep the loop alive if we're actively moving down
            }
            return false;
        }

        public override void OnSmartObjectSerialize()
        {
            //sync current transforms and velocities
            sync.pos = transform.position;
            sync.rot = transform.rotation;
            sync.vel = sync.rigid.velocity;
            sync.spin = sync.rigid.angularVelocity;
        }

        //Collision Events
        //mostly copied from SmartObjectSync.cs
        SmartObjectSync otherSync;
        public void OnCollisionEnter(Collision other)
        {
            if (!IsActiveState())
            {
                //we only want to process collision events while we're in the zero gravity state
                //otherwise let the other states handle collisions
                return;
            }
            if (sync.IsLocalOwner())
            {
                //sync that we just collided with something and our velocity is about to change
                sync.Serialize();
                //decide if we need to take ownership of the object we collided with
                if (sync.takeOwnershipOfOtherObjectsOnCollision && Utilities.IsValid(other) && Utilities.IsValid(other.collider))
                {
                    otherSync = other.collider.GetComponent<SmartObjectSync>();
                    if (otherSync && !otherSync.IsLocalOwner() && otherSync.allowOthersToTakeOwnershipOnCollision && !otherSync.IsAttachedToPlayer() && (otherSync.state == SmartObjectSync.STATE_SLEEPING || !otherSync.takeOwnershipOfOtherObjectsOnCollision || otherSync.rigid.velocity.sqrMagnitude < sync.rigid.velocity.sqrMagnitude))
                    {
                        otherSync.TakeOwnership(true);
                        otherSync.Serialize();
                    }
                }
            }
        }

        public void OnCollisionExit(Collision other)
        {
            if (!IsActiveState())
            {
                //we only want to process collision events while we're in the zero gravity state
                //otherwise let the other states handle collisions
                return;
            }

            if (sync.IsLocalOwner())
            {
                //sync that we just collided with something and our velocity is about to change
                sync.Serialize();
            }
        }
    }
}