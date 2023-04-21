
using UnityEngine;

namespace VRC.Udon
{
    [AddComponentMenu("")]
    internal class OnRenderObjectProxy : AbstractUdonBehaviourEventProxy
    {
        private void OnRenderObject()
        {
            EventReceiver.ProxyOnRenderObject();
        }
    }
}