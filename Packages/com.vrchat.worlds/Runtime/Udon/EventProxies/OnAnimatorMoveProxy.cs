using UnityEngine;

namespace VRC.Udon
{
    [AddComponentMenu("")]
    internal class OnAnimatorMoveProxy : AbstractUdonBehaviourEventProxy
    {
        private void OnAnimatorMove()
        {
            EventReceiver.ProxyOnAnimatorMove();
        }
    }
}