
using UnityEngine;

namespace VRC.Udon
{
    [AddComponentMenu("")]
    internal class OnWillRenderObjectProxy : AbstractUdonBehaviourEventProxy
    {
        private void OnWillRenderObject()
        {
            EventReceiver.ProxyOnWillRenderObject();
        }
    }
}