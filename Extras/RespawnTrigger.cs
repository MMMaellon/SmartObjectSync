
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class RespawnTrigger : UdonSharpBehaviour
    {
        void Start()
        {

        }
        
        public void OnTriggerEnter(Collider other)
        {
            if (!Utilities.IsValid(other))
            {
                return;
            }
            SmartObjectSync otherSync = other.GetComponent<SmartObjectSync>();

            if (Utilities.IsValid(otherSync))
            {
                otherSync.Respawn();
            }
        }
    }
}