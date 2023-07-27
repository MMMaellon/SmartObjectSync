
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
        public void OnCollisionEnter(Collision collision)
        {
            if (collision.impulse.magnitude > limit)
            {
                destructible.Break();
            }
        }

        public void OnParticleCollision(GameObject other)
        {
            if (!allowParticleCollisions)
            {
                return;
            }
            destructible.Break();
        }
    }
}