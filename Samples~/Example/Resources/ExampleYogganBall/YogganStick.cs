
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class YogganStick : UdonSharpBehaviour
{
    public Transform ball;
    void Start()
    {
        UnPoof();
    }

    public override void OnPickupUseDown()
    {
        base.OnPickupUseDown();
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Poof));
    }
    public override void OnPickupUseUp()
    {
        base.OnPickupUseUp();
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(UnPoof));
    }
    public override void OnDrop()
    {
        base.OnDrop();
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(UnPoof));
    }

    public void Poof()
    {
        ball.localScale = new Vector3(1, 1, 1);
    }

    public void UnPoof()
    {
        ball.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    }
}
