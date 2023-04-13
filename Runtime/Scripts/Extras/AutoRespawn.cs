
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [RequireComponent(typeof(SmartObjectSync))]
    public class AutoRespawn : SmartObjectSyncListener
    {
        public SmartObjectSync sync;
        public override void OnChangeOwner(SmartObjectSync s, VRCPlayerApi oldOwner, VRCPlayerApi newOwner)
        {
            
        }
        
        public float lastSleepTime = -1001f;
        public override void OnChangeState(SmartObjectSync s, int oldState, int newState)
        {
            if (s != sync || !sync.IsLocalOwner())
            {
                return;
            }
            if (newState == SmartObjectSync.STATE_SLEEPING)
            {
                lastSleepTime = Time.realtimeSinceStartup;
                SendCustomEventDelayedSeconds(nameof(Respawn), lastSleepTime);
            } else
            {
                lastSleepTime = -1001f;
            }
        }

        void Start()
        {
            sync = GetComponent<SmartObjectSync>();
            sync.AddListener(this);
        }

        public void Respawn()
        {
            if (lastSleepTime - 0.01f < Time.realtimeSinceStartup)//0.01f for safety against floating point errors
            {
                sync.Respawn();
            }
        }
    }
}