
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class InteractButton : UdonSharpBehaviour
{
    public UdonBehaviour udon;
    public string eventName;
    void Start()
    {
        
    }

    public override void Interact()
    {
        udon.SendCustomEvent(eventName);
    }
}
