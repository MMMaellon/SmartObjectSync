
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class YogganBall : UdonSharpBehaviour
{
    MMMaellon.SmartObjectSync sync;
    MeshRenderer mesh;
    Vector2 offset;
    void Start()
    {
        sync = GetComponent<MMMaellon.SmartObjectSync>();
        mesh = GetComponent<MeshRenderer>();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other && Networking.LocalPlayer.IsOwner(gameObject))
        {
            YogganGoal goal = other.GetComponent<YogganGoal>();
            if (goal)
            {
                goal.BroadcastParticles();
                sync.Respawn();
            }
        }
    }

    public void Update()
    {
        if (mesh)
        {
            offset.y = (offset.y - 0.0053784f) % 1f;
            mesh.sharedMaterial.mainTextureOffset = offset;
        }
    }

    public override void OnPlayerCollisionEnter(VRCPlayerApi player)
    {
        if (player != null && player.isLocal)
        {
            VRC_Pickup leftPickup = player.GetPickupInHand(VRC_Pickup.PickupHand.Left);
            VRC_Pickup rightPickup = player.GetPickupInHand(VRC_Pickup.PickupHand.Right);

            if (leftPickup)
            {
                leftPickup.Drop();
            }
            if (rightPickup)
            {
                rightPickup.Drop();
            }

            player.Respawn();
        }
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player != null && player.isLocal)
        {
            VRC_Pickup leftPickup = player.GetPickupInHand(VRC_Pickup.PickupHand.Left);
            VRC_Pickup rightPickup = player.GetPickupInHand(VRC_Pickup.PickupHand.Right);

            if (leftPickup)
            {
                leftPickup.Drop();
            }
            if (rightPickup)
            {
                rightPickup.Drop();
            }

            player.Respawn();
        }
    }
}
