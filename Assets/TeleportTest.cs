
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TeleportTest : UdonSharpBehaviour
{
    public MMMaellon.SmartObjectSync[] objs;
    public float cooldown = 5f;
    void Start()
    {

    }

    float lastTeleport = -1001f;
    public void Update()
    {
        if (Networking.LocalPlayer.isMaster && lastTeleport + cooldown < Time.timeSinceLevelLoad)
        {
            lastTeleport = Time.timeSinceLevelLoad;
            foreach (MMMaellon.SmartObjectSync obj in objs)
            {
                obj.TakeOwnership(false);
                obj.TeleportTo(obj.spawnPos + Vector3.up * Random.Range(0f, 1f), Quaternion.identity, Vector3.zero, Vector3.zero);
            }
        }
    }
}
