
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class ChildAttachmentState : SmartObjectSyncState
    {
        public bool disableCollisions = false;
        [Tooltip("Time to wait after detaching before we turn on collisions again")]
        public float collisionCooldown = 0.1f;
        [Tooltip("How the child should move when attaching itself to the parent, local to the parent's transform. If this is all zeros then the child just lerps smoothly in place. If this is is (0,0,1), then the child will lerp smoothly to (0,0,1) and then slide along the z axis into place")]
        public Vector3 attachmentVector = Vector3.zero;
        public bool returnToStartingParentOnExit = true;
        public bool automaticallySetTransforms = true;
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
            startingParent = transform.parent;
            parentTransformName = GetFullPath(startingParent);
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
        }

        public override void OnExitState()
        {
            if (disableCollisions)
            {
                SendCustomEventDelayedSeconds(nameof(EnableCollisions), collisionCooldown);
            }
            sync.rigid.isKinematic = false;
            if (returnToStartingParentOnExit)
            {
                _parentTransformName = GetFullPath(startingParent);
                parentTransform = startingParent;
            }
        }

        public void EnableCollisions()
        {
            sync.rigid.detectCollisions = true;
        }

        public override void OnSmartObjectSerialize()
        {
            if (automaticallySetTransforms)
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
            EnterState();
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
    }
}