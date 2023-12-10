
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
        public bool kinematicWhileAttached = true;
        public bool forceNullDefaultParent = false;
        public bool forceZeroLocalTransforms = true;
        public bool repeatEventsOnReparent = true;
        public bool forceSlowLerp = false;
        public float slowLerpDuration = 0.25f;
        public float momentumMultiplier = 1f;
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(parentTransformName))]
        string _parentTransformName = "";

        [System.NonSerialized]
        public Transform lastTransform = null;
        [System.NonSerialized, FieldChangeCallback(nameof(parentTransform))]
        public Transform _parentTransform = null;
        public Transform parentTransform
        {
            get => _parentTransform;
            set
            {
                lastTransform = _parentTransform;
                _parentTransform = value;
                transform.SetParent(_parentTransform, true);
                if (repeatEventsOnReparent && IsActiveState() && sync.interpolationStartTime + sync.lagTime > Time.timeSinceLevelLoad)
                {
                    if (Utilities.IsValid(sync.listeners))
                    {
                        foreach (SmartObjectSyncListener listener in sync.listeners)
                        {
                            if (Utilities.IsValid(listener))
                            {
                                listener.OnChangeState(sync, sync.lastState, sync._state);
                            }
                        }
                    }
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
        bool lastKinematic = false;
        public override void OnEnterState()
        {
            transform.SetParent(_parentTransform, true);//bug workaround
            firstInterpolation = true;
            if (disableCollisions)
            {
                sync.rigid.detectCollisions = false;
            }
            lastKinematic = sync.rigid.isKinematic;
            sync.rigid.isKinematic = kinematicWhileAttached;
            
            
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
            sync.rigid.isKinematic = lastKinematic;
            lastCollide = Time.timeSinceLevelLoad;
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
            if (forceSlowLerp)
            {
                Lerp(Mathf.Clamp01((Time.timeSinceLevelLoad - sync.interpolationStartTime) / slowLerpDuration));
            }
            else
            {
                Lerp(interpolation);
            }
        }

        public void Lerp(float interpolation)
        {
            if (attachmentVector == Vector3.zero || !firstInterpolation)
            {
                transform.localPosition = sync.HermiteInterpolatePosition(sync.startPos, Vector3.zero, sync.pos, Vector3.zero, interpolation);
                transform.localRotation = sync.HermiteInterpolateRotation(sync.startRot, Vector3.zero, sync.rot, Vector3.zero, interpolation);
            }
            else if (interpolation < 0.5)
            {
                transform.localPosition = CustomHermiteInterpolatePosition(sync.startPos, Vector3.zero, sync.pos + attachmentVector, -attachmentVector * momentumMultiplier, interpolation * 2);
                transform.localRotation = sync.HermiteInterpolateRotation(sync.startRot, Vector3.zero, sync.rot, Vector3.zero, interpolation * 2);
            }
            else
            {
                transform.localPosition = CustomHermiteInterpolatePosition(sync.pos + attachmentVector, -attachmentVector * momentumMultiplier, sync.pos, Vector3.zero, (interpolation - 0.5f) * 2f);
                transform.localRotation = sync.rot;
            }
        }

        Vector3 posControl1;
        Vector3 posControl2;
        public Vector3 CustomHermiteInterpolatePosition(Vector3 startPos, Vector3 startVel, Vector3 endPos, Vector3 endVel, float interpolation)
        {//Shout out to Kit Kat for suggesting the improved hermite interpolation
            if (forceSlowLerp)
            {
                posControl1 = startPos + startVel * slowLerpDuration * interpolation / 3f;
                posControl2 = endPos - endVel * slowLerpDuration * (1.0f - interpolation) / 3f;
                return Vector3.Lerp(Vector3.Lerp(posControl1, endPos, interpolation), Vector3.Lerp(startPos, posControl2, interpolation), interpolation);
            }
            else
            {
                posControl1 = startPos + startVel * sync.lagTime * interpolation / 3f;
                posControl2 = endPos - endVel * sync.lagTime * (1.0f - interpolation) / 3f;
                return Vector3.Lerp(Vector3.Lerp(posControl1, endPos, interpolation), Vector3.Lerp(startPos, posControl2, interpolation), interpolation);
            }
        }

        public override bool OnInterpolationEnd()
        {
            if (forceSlowLerp && firstInterpolation && ((Time.timeSinceLevelLoad - sync.interpolationStartTime) / slowLerpDuration) < 1)
            {
                return true;
            }
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
        
        [System.NonSerialized]
        public float lastCollide;
        public void OnCollisionStay(Collision collision)
        {
            if (!sync.IsLocalOwner())
            {
                return;
            }
            if (!Utilities.IsValid(collision) || !Utilities.IsValid(collision.collider) || (((1 << collision.gameObject.layer) | collisionLayers) != collisionLayers))
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
            if(collision.transform == lastTransform && lastCollide + collisionCooldown > Time.timeSinceLevelLoad){
                lastCollide = Time.timeSinceLevelLoad;
                return;
            }

            if(lastCollide + collisionCooldown > Time.timeSinceLevelLoad){
                return;
            }

            Attach(collision.transform);
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
            if(other.transform == lastTransform && lastCollide + collisionCooldown > Time.timeSinceLevelLoad){
                lastCollide = Time.timeSinceLevelLoad;
                return;
            }
            if(lastCollide + collisionCooldown > Time.timeSinceLevelLoad){
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
