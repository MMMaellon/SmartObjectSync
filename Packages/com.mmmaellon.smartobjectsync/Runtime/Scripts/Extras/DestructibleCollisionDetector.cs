
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
        public void OnCollisionEnter(Collision collision)
        {
            if (collision.impulse.magnitude > limit){
                if (takeOwnership)
                {
                    if (!Utilities.IsValid(collision.collider) || !Networking.LocalPlayer.IsOwner(collision.collider.gameObject))
                    {
                        return;
                    }
                    Networking.LocalPlayer.TakeOwnership(destructible.gameObject);
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
                if (!Networking.LocalPlayer.IsOwner(other) && !(Utilities.IsValid(other) && Utilities.IsValid(other.GetComponentInParent<SmartObjectSync>()) && other.GetComponentInParent<SmartObjectSync>().IsLocalOwner()))
                {
                    return;
                }
                Networking.LocalPlayer.TakeOwnership(destructible.gameObject);
            }
            destructible.Break();
        }
    }
}