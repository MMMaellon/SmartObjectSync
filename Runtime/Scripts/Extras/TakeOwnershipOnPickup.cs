
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TakeOwnershipOnPickup : UdonSharpBehaviour
{
    public GameObject[] targets;
    void Start()
    {
        
    }

    public override void OnPickup()
    {
        foreach (GameObject obj in targets)
        {
            if (obj)
            {
                Networking.SetOwner(Networking.LocalPlayer, obj);
            }
        }
    }
}
