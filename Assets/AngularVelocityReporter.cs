
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [RequireComponent(typeof(Rigidbody))]
    public class AngularVelocityReporter : UdonSharpBehaviour
    {
        public Rigidbody rigid;
        void Start()
        {
            rigid = GetComponent<Rigidbody>();
        }

        Quaternion lastRot = Quaternion.identity;
        float angle;
        Vector3 axis;
        Vector3 calculated;
        Vector3 euler;
        public void FixedUpdate()
        {
            Debug.Log("AngularVelocity: " + rigid.angularVelocity.x + ", " + rigid.angularVelocity.y + ", " + rigid.angularVelocity.z + "\n");
            (Quaternion.Normalize(Quaternion.Inverse(lastRot) * transform.rotation)).ToAngleAxis(out angle, out axis);
            calculated = ((transform.rotation * axis) * angle * Mathf.Deg2Rad / Time.deltaTime);
            Debug.Log("CalculatedAngularVelocity: " + calculated + "\n");
            
            lastRot = transform.rotation;
        }

        public void ResetAngularVelocity()
        {
            rigid.angularVelocity = Vector3.zero;
        }

        public void AddXAngularVelocity()
        {
            rigid.angularVelocity += new Vector3(0.1f, 0, 0);
        }

        public void AddYAngularVelocity()
        {
            rigid.angularVelocity += new Vector3(0, 0.1f, 0);
        }

        public void AddZAngularVelocity()
        {
            rigid.angularVelocity += new Vector3(0, 0, 0.1f);
        }
        public void SubXAngularVelocity()
        {
            rigid.angularVelocity -= new Vector3(0.1f, 0, 0);
        }

        public void SubYAngularVelocity()
        {
            rigid.angularVelocity -= new Vector3(0, 0.1f, 0);
        }

        public void SubZAngularVelocity()
        {
            rigid.angularVelocity -= new Vector3(0, 0, 0.1f);
        }
    }
}
