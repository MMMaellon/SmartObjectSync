
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.SmartObjectSyncExtra
{
    public class DesktopDualWield : UdonSharpBehaviour
    {
        public KeyCode dualWieldPickupShortcut = KeyCode.Tab;
        public KeyCode dualWieldDropShortcut = KeyCode.Tab;
        [System.NonSerialized]
        public SmartObjectSync secondPickup = null;
        public bool sendPickupUseEvents = true;
        public bool useMouseButton = true;
        [Tooltip("0 is left click, 1 is right click, 2 is middle click")]
        public int mouseButtonId = 0;
        [Tooltip("Only used when Use Mouse Button is false and Send Pickup Use Events is true")]
        public KeyCode pickupUseEventShortcut = KeyCode.Q;
        VRC_Pickup pickup;
        Vector3 attachOffset;
        Quaternion attachRotationOffset;
        VRCPlayerApi.TrackingData headData;
        public bool lowNetworkTrafficMode = false;
        public override void PostLateUpdate()
        {
            if (Utilities.IsValid(secondPickup))
            {
                if (!secondPickup.IsLocalOwner() || secondPickup.state != SmartObjectSync.STATE_NO_HAND_HELD)
                {
                    secondPickup = null;
                } else if (Input.GetKeyDown(dualWieldDropShortcut))
                {
                    foreach (UdonBehaviour udon in secondPickup.GetComponents<UdonBehaviour>())
                    {
                        udon.SendCustomEvent("OnDrop");
                    }
                    if (lowNetworkTrafficMode)
                    {
                        secondPickup.enabled = true;
                    }
                    secondPickup.state = SmartObjectSync.STATE_INTERPOLATING;
                } else
                {
                    if (lowNetworkTrafficMode && secondPickup.lastSerializeRequest > secondPickup.lastPickup)
                    {
                        //disable for that good network traffic
                        secondPickup.enabled = false;
                    }
                    //manually move in place
                    headData = secondPickup.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                    secondPickup.transform.position = headData.position + headData.rotation * attachOffset;
                    secondPickup.transform.rotation = headData.rotation * attachRotationOffset;

                    if (sendPickupUseEvents)
                    {
                        if ((useMouseButton && Input.GetMouseButtonDown(mouseButtonId)) || (!useMouseButton && Input.GetKeyDown(pickupUseEventShortcut)))
                        {
                            foreach (UdonSharpBehaviour udon in secondPickup.GetComponents<UdonSharpBehaviour>())
                            {
                                udon.SendCustomEvent("_onPickupUseDown");
                            }
                        }
                        else if ((useMouseButton && Input.GetMouseButtonUp(mouseButtonId)) || (!useMouseButton && Input.GetKeyUp(pickupUseEventShortcut)))
                        {
                            foreach (UdonSharpBehaviour udon in secondPickup.GetComponents<UdonSharpBehaviour>())
                            {
                                udon.SendCustomEvent("_onPickupUseUp");
                            }
                        }
                    }
                }
            } else if (Input.GetKeyDown(dualWieldPickupShortcut))
            {
                pickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
                if (Utilities.IsValid(pickup))
                {
                    secondPickup = pickup.GetComponent<SmartObjectSync>();
                    if (Utilities.IsValid(secondPickup))
                    {
                        secondPickup.pickup.Drop();
                        secondPickup.state = SmartObjectSync.STATE_NO_HAND_HELD;
                        secondPickup.pos.x = -secondPickup.pos.x;//flip on x axis;

                        secondPickup.noHand_CalcParentTransform();
                        secondPickup.preventPickupJitter();
                        secondPickup.RequestSerialization();

                        headData = secondPickup.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                        attachOffset = Quaternion.Inverse(headData.rotation) * (secondPickup.transform.position - headData.position);
                        attachRotationOffset = Quaternion.Inverse(headData.rotation) * secondPickup.transform.rotation;
                    }
                }
            }
        }
    }
}