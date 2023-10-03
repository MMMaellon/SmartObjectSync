
using MMMaellon;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ObjectWhitelist : UdonSharpBehaviour
{
    public GameObject[] objectsToDisable;
    public VRC_Pickup[] pickupsToMakeUnpickupable;
    public UdonBehaviour[] interactsToMakeUninteractable;
    public string[] whiteListedUsernames;
    string localUsername;
    public bool checkWhitelistAtStart = true;
    public void Start()
    {
        if (checkWhitelistAtStart)
        {
            CheckWhitelist();
        }
    }
    public void EnableObjects()
    {
        foreach (GameObject obj in objectsToDisable)
        {
            if (Utilities.IsValid(obj))
            {
                obj.SetActive(true);
            }
        }
        foreach (VRC_Pickup pickup in pickupsToMakeUnpickupable)
        {
            if (Utilities.IsValid(pickup))
            {
                pickup.pickupable = true;
            }
        }
        foreach (UdonBehaviour interact in interactsToMakeUninteractable)
        {
            if (Utilities.IsValid(interact))
            {
                interact.DisableInteractive = !true;
            }
        }
    }
    SmartObjectSync sync;
    public void CheckWhitelist()
    {
        localUsername = Networking.LocalPlayer.displayName;
        foreach (string username in whiteListedUsernames)
        {
            if (username == localUsername)
            {
                EnableObjects();
                return;
            }
        }
        DisableObjects();
    }
    public void DisableObjects()
    {
        foreach (GameObject obj in objectsToDisable)
        {
            if (Utilities.IsValid(obj))
            {
                obj.SetActive(false);
            }
        }
        foreach (VRC_Pickup pickup in pickupsToMakeUnpickupable)
        {
            if (Utilities.IsValid(pickup))
            {
                sync = pickup.GetComponent<SmartObjectSync>();
                if (Utilities.IsValid(sync))
                {
                    sync.pickupable = false;
                }
                else
                {
                    pickup.pickupable = false;
                }
            }
        }
        foreach (UdonBehaviour interact in interactsToMakeUninteractable)
        {
            if (Utilities.IsValid(interact))
            {
                interact.DisableInteractive = !false;
            }
        }
    }
}
