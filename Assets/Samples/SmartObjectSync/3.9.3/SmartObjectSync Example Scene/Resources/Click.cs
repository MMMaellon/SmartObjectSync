
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Click : UdonSharpBehaviour
{
    public UdonBehaviour[] behaviours;
    public string udonEvent = "Respawn";
    void Start()
    {
        
    }
    public override void Interact()
    {
        foreach (UdonBehaviour behaviour in behaviours)
        {
            behaviour.SendCustomEvent(udonEvent);
        }
    }
}
