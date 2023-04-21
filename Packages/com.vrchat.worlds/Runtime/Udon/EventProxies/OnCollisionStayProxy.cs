
using UnityEngine;

namespace VRC.Udon
{
    [AddComponentMenu("")]
    internal class OnCollisionStayProxy : AbstractUdonBehaviourEventProxy
    {
        private void OnCollisionStay(Collision other)
        {
            EventReceiver.ProxyOnCollisionStay(other);
        }
    }
}