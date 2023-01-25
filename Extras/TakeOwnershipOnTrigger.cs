
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class TakeOwnershipOnTrigger : UdonSharpBehaviour
    {
        public SmartObjectSync pickupOwner;
        void Start()
        {

        }

        public void OnTriggerEnter(Collider other)
        {
            if (other && Utilities.IsValid(pickupOwner.owner))
            {
                SmartObjectSync puck = other.GetComponent<SmartObjectSync>();
                if (puck && other.GetComponent<YogganBall>())
                {
                    if (pickupOwner.owner.isLocal)
                    {
                        puck.TakeOwnership(true);
                    } else
                    {
                        //Slow down
                        puck.helper.enabled = false;
                        puck.rigid.velocity = Vector3.zero;
                    }
                }
            }
        }
    }
}
