
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class RigidbodyDebugPrinter : UdonSharpBehaviour
{
    Rigidbody rigid;
    void Start()
    {
        rigid = GetComponent<Rigidbody>();
        printCollisionDetectionMode();
        SendCustomEventDelayedSeconds(nameof(printCollisionDetectionMode), 5f);
    }

    public void printCollisionDetectionMode()
    {
        if (!rigid)
        {
            return;
        }
        switch (rigid.collisionDetectionMode)
        {
            case (CollisionDetectionMode.Continuous):
                {
                    Debug.LogWarning("-------------------" + gameObject.name + " Continuous");
                    break;
                }
            case (CollisionDetectionMode.ContinuousDynamic):
                {
                    Debug.LogWarning("-------------------" + gameObject.name + " ContinuousDynamic");
                    break;
                }
            case (CollisionDetectionMode.ContinuousSpeculative):
                {
                    Debug.LogWarning("-------------------" + gameObject.name + " ContinuousSpeculative");
                    break;
                }
            case (CollisionDetectionMode.Discrete):
                {
                    Debug.LogWarning("-------------------" + gameObject.name + " Discrete");
                    break;
                }
        }
    }
}
