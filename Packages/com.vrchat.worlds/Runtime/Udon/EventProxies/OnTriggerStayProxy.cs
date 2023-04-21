
using UnityEngine;

namespace VRC.Udon
{
    [AddComponentMenu("")]
    internal class OnTriggerStayProxy : AbstractUdonBehaviourEventProxy
    {
        private void OnTriggerStay(Collider other)
        {
            EventReceiver.ProxyOnTriggerStay(other);
        }
    }
}