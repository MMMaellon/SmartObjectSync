
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class ChildAttachmentState : SmartObjectSyncState
    {
        public bool returnToStartingParentOnExit = true;
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(parentTransformName))] string _parentTransformName = "";

        [System.NonSerialized]
        public Transform _parentTransform = null;
        public Transform parentTransform
        {
            get => _parentTransform;
            set
            {
                _parentTransform = value;
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
                    return;
                }
                GameObject parentObj = GameObject.Find(value);
                if (Utilities.IsValid(parentObj))
                {
                    _parentTransformName = value;
                    parentTransform = parentObj.transform;
                    transform.SetParent(parentObj.transform, true);
                    if (Utilities.IsValid(transform.parent))
                    {
                        if (sync.IsLocalOwner())
                        {
                            sync.pos = transform.localPosition;
                            sync.rot = transform.localRotation;
                        }
                        sync.startPos = transform.localPosition;
                        sync.startRot = transform.localRotation;
                    }
                    else if (sync.IsLocalOwner())
                    {
                        ExitState();
                    }
                    return;
                }
                _parentTransformName = "";
                parentTransform = null;
            }
        }
        Transform startingParent;
        void Start()
        {
            if (sync.IsLocalOwner())
            {
                startingParent = transform.parent;
                parentTransformName = GetFullPath(startingParent);
            }
        }
        public override void OnEnterState()
        {
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
            if (returnToStartingParentOnExit)
            {
                parentTransformName = GetFullPath(startingParent);
            }
        }

        public override void OnSmartObjectSerialize()
        {

        }

        public override void OnInterpolationStart()
        {

        }

        public override void Interpolate(float interpolation)
        {
            transform.localPosition = sync.HermiteInterpolatePosition(sync.startPos, Vector3.zero, sync.pos, Vector3.zero, interpolation);
            transform.localRotation = sync.HermiteInterpolateRotation(sync.startRot, Vector3.zero, sync.rot, Vector3.zero, interpolation);
        }

        public override bool OnInterpolationEnd()
        {
            return transform.localPosition != sync.pos || transform.localRotation != sync.rot;
        }

        public override void OnDrop()
        {
            if (sync.IsLocalOwner() && sync.rigid.isKinematic)
            {
                EnterState();
            }
        }

        public void Attach(Transform t)
        {
            sync.TakeOwnership(false);
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