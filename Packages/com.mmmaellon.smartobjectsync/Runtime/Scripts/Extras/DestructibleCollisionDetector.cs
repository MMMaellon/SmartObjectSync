
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class DestructibleCollisionDetector : UdonSharpBehaviour
    {
        public DestructibleObject destructible;
        public float limit = 1;
        public bool allowParticleCollisions = true;
        public bool takeOwnership = true;
        SmartObjectSync otherSync;
        public void OnCollisionEnter(Collision collision)
        {
            if (collision.impulse.magnitude > limit)
            {
                if (takeOwnership)
                {
                    if (Utilities.IsValid(collision.collider))
                    {
                        otherSync = collision.collider.GetComponent<SmartObjectSync>();
                        if (Utilities.IsValid(otherSync) && !otherSync.IsLocalOwner())
                        {
                            return;
                        }
                    }
                    Networking.SetOwner(Networking.LocalPlayer, destructible.gameObject);
                    destructible.wholeObject.TakeOwnership(false);
                }
                destructible.Break();
            }
        }

        public void OnParticleCollision(GameObject other)
        {
            if (!allowParticleCollisions)
            {
                return;
            }
            if (takeOwnership)
            {
                if (Utilities.IsValid(other))
                {
                    otherSync = other.GetComponent<SmartObjectSync>();
                    if (Utilities.IsValid(otherSync) && !otherSync.IsLocalOwner())
                    {
                        return;
                    }
                }
                Networking.SetOwner(Networking.LocalPlayer, destructible.gameObject);
                destructible.wholeObject.TakeOwnership(false);
            }
            destructible.Break();
        }
    }
}