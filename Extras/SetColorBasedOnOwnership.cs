
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SetColorBasedOnOwnership : UdonSharpBehaviour
{
    public MeshRenderer meshRenderer;
    public Color isOwner = Color.red;
    public Color isNotOwner = Color.blue;
    void Start()
    {
        if (meshRenderer)
        {
            //makes it unique
            meshRenderer.material.color = isOwner;
        }
        OnOwnershipTransferred(Networking.GetOwner(gameObject));
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (meshRenderer && Utilities.IsValid(player))
        {
            meshRenderer.sharedMaterial.color = player.isLocal ? isOwner : isNotOwner;
        }
    }
}
