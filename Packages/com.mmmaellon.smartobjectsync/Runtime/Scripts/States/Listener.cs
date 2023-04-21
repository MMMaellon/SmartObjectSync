
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using JetBrains.Annotations;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class SmartObjectSyncListener : UdonSharpBehaviour
    {
        public abstract void OnChangeState(SmartObjectSync sync, int oldState, int newState);
        public abstract void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner);
    }
}