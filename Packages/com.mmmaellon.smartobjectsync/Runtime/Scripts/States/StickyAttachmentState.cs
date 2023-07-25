
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class StickyAttachmentState : SmartObjectSyncState
    {
        public bool attachOnCollision = true;
        public LayerMask stickyCollisionLayers;
        public bool attachOnPickupUseDown = false;
        [System.NonSerialized, UdonSynced, FieldChangeCallback(nameof(parentTransformName))]
        string _parentTransformName = "";

        [System.NonSerialized]
        public Transform _parentTransform = null;

        [System.NonSerialized]
        public Vector3 startPos;

        [System.NonSerialized]
        public Quaternion startRot;
        
        private Transform lastCollided = null;
        private Collider rigidCollider;
        private Collider parentCollider;
        public Transform parentTransform{
            get => _parentTransform;
            set
            {
                if (Utilities.IsValid(parentCollider) && Utilities.IsValid(rigidCollider))
                {
                    Physics.IgnoreCollision(rigidCollider, parentCollider, false);
                }
                _parentTransform = value;
                parentCollider = Utilities.IsValid(_parentTransform) ? _parentTransform.GetComponent<Collider>() : null;
                if (Utilities.IsValid(parentCollider) && Utilities.IsValid(rigidCollider))
                {
                    Physics.IgnoreCollision(rigidCollider, parentCollider, true);
                }
                if (sync.IsOwnerLocal())
                {
                    RequestSerialization();
                }
            }
        }

        public string parentTransformName
        {
            get => _parentTransformName;
            set
            {
                if (!Utilities.IsValid(value) || value == "" || value == null)
                {
                    _parentTransformName = "";
                    parentTransform = null;
                    if (sync.IsLocalOwner() && IsActiveState())
                    {
                        ExitState();
                    }
                    return;
                }
                GameObject parentObj = GameObject.Find(value);
                if (Utilities.IsValid(parentObj))
                {
                    // StickyAttachmentState otherSticky = parentObj.GetComponent<StickyAttachmentState>();
                    // while (otherSticky != null && otherSticky.sync && otherSticky.sync.customState == otherSticky && otherSticky.parentTransform != null)
                    // {
                    //     parentObj = otherSticky.gameObject;
                    //     if (otherSticky.parentTransform == transform)
                    //     {
                    //         //circular dependency detected
                    //         return;
                    //     }
                    //     otherSticky = otherSticky.parentTransform.GetComponent<StickyAttachmentState>();
                    // }
                    _parentTransformName = value;
                    parentTransform = parentObj.transform;
                    if (sync.IsLocalOwner() && !IsActiveState())
                    {
                        EnterState();
                    }
                } else
                {
                    _parentTransformName = "";
                    parentTransform = null;
                    if (sync.IsLocalOwner() && IsActiveState())
                    {
                        ExitState();
                    }
                }
            }
        }
        void Start()
        {
            rigidCollider = GetComponent<Collider>();
        }
        public void OnCollisionEnter(Collision other)
        {
            //ignore collisions if we're already in the attached state
            if (!sync.IsLocalOwner() || !Utilities.IsValid(other) || !Utilities.IsValid(other.collider) || (((1 << other.collider.gameObject.layer) | stickyCollisionLayers) != stickyCollisionLayers))
            {
                return;
            }
            lastCollided = other.collider.transform;
            if (!attachOnCollision)
            {
                return;
            }
            if (IsActiveState())
            {
                if (lastCollided == parentTransform)
                {
                    //something just caused us to collide with the same object again
                    //this means we're probably in an unstable state and need to reserialize
                    //serialization takes a few frames, so we call the OnSmartObjectSerialize callback immediately
                    RequestSerialization();
                    OnSmartObjectSerialize();
                    return;
                }
                return;
            } else if (sync.IsAttachedToPlayer() || sync.state >= SmartObjectSync.STATE_CUSTOM)
            {
                return;
            }
            Attach(lastCollided);
        }

        public void OnCollisionExit(Collision other)
        {
            if (Utilities.IsValid(other) && Utilities.IsValid(other.collider) && other.collider.transform == lastCollided){
                lastCollided = null;
            }
        }

        public override void OnPickupUseDown()
        {
            if (!attachOnPickupUseDown)
            {
                return;
            }
            if (lastCollided)
            {
                Attach(lastCollided);
            }
        }

        public void Attach(Transform attachTarget)
        {
            if (!Utilities.IsValid(attachTarget))
            {
                return;
            }
            if (!sync.IsLocalOwner())
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            parentTransformName = GetFullPath(attachTarget);
        }

        public string GetFullPath(Transform target)
        {
            Transform pathBuilder = target;
            string tempName = "";
            while (Utilities.IsValid(pathBuilder))
            {
                tempName = "/" + pathBuilder.name + tempName;
                pathBuilder = pathBuilder.parent;
            }
            return tempName;
        }

        public override void OnEnterState()
        {
            
        }

        public override void OnExitState()
        {
            _parentTransformName = "";
            parentTransform = null;
        }

        public override void OnSmartObjectSerialize()
        {
            if(parentTransform){
                sync.pos = parentTransform.InverseTransformPoint(transform.position);
                sync.rot = Quaternion.Inverse(parentTransform.rotation) * transform.rotation;
                sync.vel = Vector3.zero;
                sync.spin = Vector3.zero;
            } else if (sync.IsLocalOwner())
            {
                //parent transform is invalid
                ExitState();
            }
        }

        public override void OnInterpolationStart()
        {
            if (parentTransform)
            {
                startPos = parentTransform.InverseTransformPoint(transform.position);
                startRot = Quaternion.Inverse(parentTransform.rotation) * transform.rotation;
            } else if (sync.IsLocalOwner())
            {
                //parent transform is invalid
                ExitState();
            }
        }

        public override void Interpolate(float interpolation)
        {
            if (parentTransform)
            {
                transform.position = sync.HermiteInterpolatePosition(parentTransform.TransformPoint(startPos), Vector3.zero, parentTransform.TransformPoint(sync.pos), Vector3.zero, interpolation);
                transform.rotation = sync.HermiteInterpolateRotation(parentTransform.rotation * startRot, Vector3.zero, parentTransform.rotation * sync.rot, Vector3.zero, interpolation);
                sync.rigid.velocity = Vector3.zero;
                sync.rigid.angularVelocity = Vector3.zero;
                sync.rigid.Sleep();
            } else if (sync.IsLocalOwner())
            {
                //parent transform is invalid
                ExitState();
            }
        }

        public override bool OnInterpolationEnd()
        {
            return true;
        }
    }
}