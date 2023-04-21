
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace VRC.Udon
{
    internal abstract class AbstractUdonBehaviourEventProxy : MonoBehaviour
    {
        public UdonBehaviour EventReceiver { get; set; }
    }
}
