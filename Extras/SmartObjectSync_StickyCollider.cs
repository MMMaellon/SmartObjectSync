
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SmartObjectSync_StickyCollider : SmartObjectSyncState
    {
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(parentTransformName))] string _parentTransfromName = "";

        [System.NonSerialized]
        public Transform parentTransform = null;

        [System.NonSerialized]
        public Vector3 startPos;

        [System.NonSerialized]
        public Quaternion startRot;
        
        public string parentTransformName{
            get => _parentTransfromName;
            set
            {
                _parentTransfromName = value;
                var parentObj = GameObject.Find(value);
                if (Utilities.IsValid(parentObj))
                {

                    SmartObjectSync_StickyCollider otherSticky = parentObj.GetComponent<SmartObjectSync_StickyCollider>();

                    if (otherSticky != null && otherSticky.sync && otherSticky.sync.IsLocalOwner() && otherSticky.sync.state == otherSticky.stateID && otherSticky.parentTransform == transform)
                    {
                        //means that two people threw stickies and they collided in mid-air and they're both trying to stick to eachother
                        Networking.SetOwner(Networking.LocalPlayer, gameObject);
                        sync.state = SmartObjectSync.STATE_FALLING;
                        return;
                    }
                    parentTransform = parentObj.transform;
                    if (sync.IsLocalOwner()){
                        sync.state = stateID;
                    }
                }
            }
        }
        void Start()
        {
            
        }
        SmartObjectSync otherSync;
        public void OnCollisionEnter(Collision other)
        {
            if (!sync.IsLocalOwner() || !Utilities.IsValid(other) || !Utilities.IsValid(other.collider) || !Utilities.IsValid(other.collider.name))
            {
                return;
            }

            SmartObjectSync_StickyCollider otherSticky = other.collider.GetComponent<SmartObjectSync_StickyCollider>();

            if (otherSticky != null && otherSticky.sync && otherSticky.sync.state == otherSticky.stateID && otherSticky.parentTransform == transform)
            {
                return;
            }

            Transform pathBuilder = other.collider.transform;
            var tempName = "";
            while (Utilities.IsValid(pathBuilder))
            {
                tempName = "/" + pathBuilder.name + tempName;
                pathBuilder = pathBuilder.parent;
            }
            parentTransformName = tempName;
        }

        public override void OnEnterState()
        {
            
        }

        public override void OnExitState()
        {
            
        }

        public override void OnSmartObjectSerialize()
        {
            sync.pos = parentTransform.InverseTransformPoint(transform.position);
            sync.rot = Quaternion.Inverse(parentTransform.rotation) * transform.rotation;
            sync.vel = Vector3.zero;
            sync.spin = Vector3.zero;
        }

        public override void OnInterpolationStart()
        {
            startPos = parentTransform.InverseTransformPoint(transform.position);
            startRot = Quaternion.Inverse(parentTransform.rotation) * transform.rotation;
        }

        public override void Interpolate(float interpolation)
        {
            transform.position = sync.HermiteInterpolatePosition(parentTransform.TransformPoint(startPos), Vector3.zero, parentTransform.TransformPoint(sync.pos), Vector3.zero, interpolation);
            transform.rotation = sync.HermiteInterpolateRotation(parentTransform.rotation * startRot, Vector3.zero, parentTransform.rotation * sync.rot, Vector3.zero, interpolation);
            if (sync.rigid)
            {
                sync.rigid.velocity = Vector3.zero;
                sync.rigid.angularVelocity = Vector3.zero;
            }
        }

        public override bool OnInterpolationEnd()
        {
            return true;
        }
    }
}