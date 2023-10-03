
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class ChildAttachmentState : SmartObjectSyncState
    {
        public bool attachOnCollisionEnter = false;
        public bool attachOnTriggerEnter = false;
        public bool attachOnPickupUseDown = false;
        public bool attachOnDrop = false;
        public LayerMask collisionLayers;
        public bool disableCollisions = false;
        [Tooltip("Time to wait after detaching before we turn on collisions again")]
        public float collisionCooldown = 0.1f;
        [Tooltip("How the child should move when attaching itself to the parent, local to the parent's transform. If this is all zeros then the child just lerps smoothly in place. If this is is (0,0,1), then the child will lerp smoothly to (0,0,1) and then slide along the z axis into place")]
        public Vector3 attachmentVector = Vector3.zero;
        public bool returnToDefaultParentOnExit = true;
        public bool forceNullDefaultParent = false;
        public bool forceZeroLocalTransforms = true;
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(parentTransformName))] string _parentTransformName = "";

        [System.NonSerialized, FieldChangeCallback(nameof(parentTransform))]
        public Transform _parentTransform = null;
        public Transform parentTransform
        {
            get => _parentTransform;
            set
            {
                _parentTransform = value;
                transform.SetParent(_parentTransform, true);
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
                if (!Utilities.IsValid(value) || value == "")
                {
                    _parentTransformName = "";
                    parentTransform = null;
                    return;
                }
                GameObject parentObj = GameObject.Find(value);
                if (Utilities.IsValid(parentObj))
                {
                    _parentTransformName = value;
                    parentTransform = parentObj.transform;
                    if (IsActiveState())
                    {
                        if (Utilities.IsValid(transform.parent))
                        {
                            sync.startPos = transform.localPosition;
                            sync.startRot = transform.localRotation;
                        }
                        else if (sync.IsLocalOwner())
                        {
                            ExitState();
                        }
                    }
                    return;
                }
                _parentTransformName = "";
                parentTransform = null;
            }
        }
        Transform startingParent;
        bool firstInterpolation = true;
        void Start()
        {
            if (forceNullDefaultParent)
            {
                startingParent = null;
            }
            else
            {
                startingParent = transform.parent;
            }
            // parentTransformName = GetFullPath(startingParent);
        }
        public override void EnterState()
        {
            Attach(transform.parent);
        }
        public override void OnEnterState()
        {
            firstInterpolation = true;
            if (disableCollisions)
            {
                sync.rigid.detectCollisions = false;
            }
            sync.rigid.isKinematic = true;
            
            
            if (!Utilities.IsValid(parentTransform))
            {
                if (sync.IsLocalOwner())
                {
                    ExitState();
                }
                return;
            }
            
            if (forceZeroLocalTransforms)
            {
                sync.pos = Vector3.zero;
                sync.rot = Quaternion.identity;
            }
        }

        public override void OnExitState()
        {
            if (disableCollisions)
            {
                SendCustomEventDelayedSeconds(nameof(EnableCollisions), collisionCooldown);
            }
            sync.rigid.isKinematic = false;
            if (returnToDefaultParentOnExit)
            {
                _parentTransformName = GetFullPath(startingParent);
                parentTransform = startingParent;
            }
        }

        public void EnableCollisions()
        {
            if (IsActiveState())
            {
                return;
            }
            sync.rigid.detectCollisions = true;
        }

        public override void OnSmartObjectSerialize()
        {
            if (forceZeroLocalTransforms)
            {
                sync.pos = Vector3.zero;
                sync.rot = Quaternion.identity;
            }
            else
            {
                sync.pos = transform.localPosition;
                sync.rot = transform.localRotation;
            }
        }

        public override void OnInterpolationStart()
        {
            sync.startPos = transform.localPosition;
            sync.startRot = transform.localRotation;
        }

        public override void Interpolate(float interpolation)
        {
            if (attachmentVector == Vector3.zero || !firstInterpolation)
            {
                transform.localPosition = sync.HermiteInterpolatePosition(sync.startPos, Vector3.zero, sync.pos, Vector3.zero, interpolation);
                transform.localRotation = sync.HermiteInterpolateRotation(sync.startRot, Vector3.zero, sync.rot, Vector3.zero, interpolation);
            } else if (interpolation < 0.5)
            {
                    transform.localPosition = sync.HermiteInterpolatePosition(sync.startPos, Vector3.zero, sync.pos + attachmentVector, Vector3.zero, interpolation * 2);
                    transform.localRotation = sync.HermiteInterpolateRotation(sync.startRot, Vector3.zero, sync.rot, Vector3.zero, interpolation * 2);
            } else
            {
                transform.localPosition = sync.HermiteInterpolatePosition(sync.pos + attachmentVector, Vector3.zero, sync.pos, Vector3.zero, (interpolation - 0.5f) * 2f);
                transform.localRotation = sync.rot;
            }
        }

        public override bool OnInterpolationEnd()
        {
            firstInterpolation = false;
            return transform.localPosition != sync.pos || transform.localRotation != sync.rot;
        }

        public void Attach(Transform t)
        {
            sync.TakeOwnership(false);
            if (IsActiveState())
            {
                ExitState();
            }
            parentTransformName = GetFullPath(t);
            base.EnterState();
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
        public void OnCollisionStay(Collision collision)
        {
            if (!sync.IsLocalOwner())
            {
                return;
            }
            if (!Utilities.IsValid(collision) || !Utilities.IsValid(collision.collider) || (((1 << collision.collider.gameObject.layer) | collisionLayers) != collisionLayers))
            {
                return;
            }
            if (sync.state > SmartObjectSync.STATE_NO_HAND_HELD || sync.state < SmartObjectSync.STATE_SLEEPING)
            {
                return;
            }

            if (!attachOnCollisionEnter && !(attachRequested && !sync.IsHeld()))
            {
                return;
            }

            Attach(collision.collider.transform);
        }


        public void OnTriggerStay(Collider other)
        {
            if (!sync.IsLocalOwner())
            {
                return;
            }
            if (!Utilities.IsValid(other) || (((1 << other.gameObject.layer) | collisionLayers) != collisionLayers)){
                return;
            }
            if (sync.state > SmartObjectSync.STATE_NO_HAND_HELD || sync.state < SmartObjectSync.STATE_SLEEPING)
            {
                return;
            }
            if (!attachOnTriggerEnter && !(attachRequested && !sync.IsHeld()))
            {
                return;
            }
            Attach(other.transform);
        }

        public override void OnDrop()
        {
            if (attachOnDrop)
            {
                RequestAttach();
            }
        }
        public override void OnPickupUseDown()
        {
            if (attachOnPickupUseDown)
            {
                RequestAttach();
            }
        }
        public void RequestAttach()
        {
            attachRequested = true;
            SendCustomEventDelayedFrames(nameof(ClearAttachRequest), 2);
        }
        bool attachRequested = false;
        public void ClearAttachRequest()
        {
            attachRequested = false;
        }
    }
}