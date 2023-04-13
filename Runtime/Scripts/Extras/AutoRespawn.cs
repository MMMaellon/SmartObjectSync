
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class AutoRespawn : SmartObjectSyncListener
    {
        public override void OnChangeOwner(SmartObjectSync sync, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {
            
        }

        public float lastSleepTime = -1001f;
        public override void OnChangeState(SmartObjectSync sync, int oldState, int newState)
        {
            if (!sync.IsLocalOwner())
            {
                return;
            }
            if (newState == SmartObjectSync.STATE_SLEEPING)
            {
                lastSleepTime = Time.realtimeSinceStartup;
            } else
            {

            }
        }

        void Start()
        {

        }
    }
}