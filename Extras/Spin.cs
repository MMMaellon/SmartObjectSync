
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Spin : UdonSharp.UdonSharpBehaviour {
    private Vector3 startPos;
    private Vector3 endPos;
    private Quaternion startRot;
    private Quaternion endRot;
    bool up = true;
    float lerpTime = 0.1f;
    float interpolation = 0.0f;
    void Start()
    {
        startPos = transform.position;
        endPos = startPos + Vector3.up;
        startRot = transform.rotation;
        endRot = startRot * Quaternion.AngleAxis(180, new Vector3(1,2,3));
    }
    void Update() {
        if (up)
        {
            interpolation = Mathf.Lerp(interpolation, 1.0f, 0.2f);
            if (interpolation > 0.99f)
            {
                up = false;
            }
        } else
        {
            interpolation = Mathf.Lerp(interpolation, 0.0f, 0.2f);
            if (interpolation < 0.01f)
            {
                up = true;
            }
        }
        transform.position = HermiteInterpolatePosition(startPos, Vector3.zero, endPos, Vector3.zero, interpolation);
        transform.rotation = HermiteInterpolateRotation(startRot, Vector3.zero, endRot, Vector3.zero, interpolation);
    }

    //Helpers
    Vector3 posControl1;
    Vector3 posControl2;
    Quaternion rotControl1;
    Quaternion rotControl2;
    public Vector3 HermiteInterpolatePosition(Vector3 startPos, Vector3 startVel, Vector3 endPos, Vector3 endVel, float interpolation)
    {
        posControl1 = startPos + startVel * lerpTime * interpolation / 3f;
        posControl2 = endPos - endVel * lerpTime * (1.0f - interpolation) / 3f;
        return Vector3.Lerp(posControl1, posControl2, interpolation);
    }
    public Quaternion HermiteInterpolateRotation(Quaternion startRot, Vector3 startSpin, Quaternion endRot, Vector3 endSpin, float interpolation)
    {
        rotControl1 = startRot * Quaternion.Euler(startSpin * lerpTime * interpolation / 3f);
        rotControl2 = endRot * Quaternion.Euler(-1.0f * endSpin * lerpTime * (1.0f - interpolation) / 3f);
        return Quaternion.Slerp(rotControl1, rotControl2, interpolation);
    }
}