
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class SmartObjectSync_Extension_Sticky : SmartObjectSyncExtension
    {
        [UdonSynced(UdonSyncMode.None)]
        Vector3 position;
        void Start()
        {

        }

        public override void OnPickupUseDown()
        {
            Activate();
        }

        public override void OnActivate(bool IsOwner)
        {
            position = transform.position;
        }

        public override Vector3 CalcPosition(bool IsOwner)
        {
            return position;
        }
    }
}
