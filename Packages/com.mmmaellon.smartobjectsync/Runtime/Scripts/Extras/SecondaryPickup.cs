
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [RequireComponent(typeof(VRC.SDK3.Components.VRCPickup)), RequireComponent(typeof(SmartObjectSync))]//slightly after everything else
    public class SecondaryPickup : SmartObjectSyncListener
    {
        [System.NonSerialized]
        public SmartObjectSync sync;
        public SmartObjectSync primaryPickup;
        public Transform constrainedObject;
        public bool resetOnDrop = false;
        public bool disableWhenPrimaryPickupDropped = false;
        public bool allowMultiplePickupOwners = true;

        [Tooltip("0 means the object will move with the primary pickup. 1 means the object will move with this pickup.")]
        public float positionBias = 0.5f;
        Vector3 startPos;
        Quaternion startRot;
        Vector3 startPosPrimary;
        Quaternion startRotPrimary;
        Vector3 constrainedStartPos;
        Quaternion constrainedStartRot;
        Vector3 constrainedStartPosPrimary;
        Quaternion constrainedStartRotPrimary;
        Vector3 constrainedStartPosInverse;
        Vector3 calcedVel;
        Vector3 calcedSpin;
        public override void OnChangeState(SmartObjectSync s, int oldState, int newState)
        {
            if (!Utilities.IsValid(sync))
            {
                return;
            }
            enabled = enabled || s.IsHeld();
            if (s == sync)
            {
                if (!s.IsHeld() && (oldState == SmartObjectSync.STATE_LEFT_HAND_HELD || oldState == SmartObjectSync.STATE_RIGHT_HAND_HELD || oldState == SmartObjectSync.STATE_NO_HAND_HELD))
                {
                    if (!resetOnDrop)
                    {
                        RecordCurrentOffsets();
                    }
                    else
                    {
                        ConstrainPrimaryPickupToThis();
                    }
                    if (!primaryPickup.IsHeld())
                    {
                        if (primaryPickup.IsLocalOwner())
                        {
                            primaryPickup.rigid.velocity = calcedVel;
                            primaryPickup.rigid.angularVelocity = calcedSpin;
                            primaryPickup.Serialize();
                        }
                        ConstrainObjectToGrips();
                    }
                }
                else if (s.IsLocalOwner() && newState == SmartObjectSync.STATE_TELEPORTING && primaryPickup.state != SmartObjectSync.STATE_TELEPORTING)
                {
                    primaryPickup.Respawn();
                }
            }
            else if (s == primaryPickup)
            {
                if (!s.IsHeld() && (oldState == SmartObjectSync.STATE_LEFT_HAND_HELD || oldState == SmartObjectSync.STATE_RIGHT_HAND_HELD || oldState == SmartObjectSync.STATE_NO_HAND_HELD))
                {
                    if (!resetOnDrop)
                    {
                        RecordCurrentOffsets();
                    }
                    else
                    {
                        ConstrainPrimaryPickupToThis();
                    }
                    if (disableWhenPrimaryPickupDropped)
                    {
                        sync.pickup.pickupable = primaryPickup.pickup.IsHeld;
                        sync.pickup.Drop();
                    }
                } else if (s.IsLocalOwner() && newState == SmartObjectSync.STATE_TELEPORTING && sync.state != SmartObjectSync.STATE_TELEPORTING)
                {
                    sync.Respawn();
                }
            }
        }

        public override void OnChangeOwner(SmartObjectSync s, VRCPlayerApi oldPlayer, VRCPlayerApi newPlayer)
        {
            if (allowMultiplePickupOwners || !newPlayer.isLocal)
            {
                return;
            }

            if (s == sync)
            {
                Networking.SetOwner(newPlayer, primaryPickup.gameObject);
            }
            else
            {
                Networking.SetOwner(newPlayer, sync.gameObject);
            }
        }
        VRCPlayerApi _localPlayer;

        void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            sync = GetComponent<SmartObjectSync>();
            sync.AddListener(this);
            primaryPickup.AddListener(this);
            RecordOffsets();

            if (disableWhenPrimaryPickupDropped)
            {
                sync.pickup.pickupable = primaryPickup.pickup.IsHeld;
                sync.pickup.Drop();
            }
        }

        public void RecordOffsets()
        {
            constrainedStartPos = sync.transform.InverseTransformPoint(constrainedObject.position);
            constrainedStartRot = Quaternion.Inverse(sync.transform.rotation) * constrainedObject.rotation;
            constrainedStartPosInverse = constrainedObject.InverseTransformPoint(sync.transform.position);
            constrainedStartPosPrimary = primaryPickup.transform.InverseTransformPoint(constrainedObject.position);
            constrainedStartRotPrimary = Quaternion.Inverse(primaryPickup.transform.rotation) * constrainedObject.rotation;
            startPos = primaryPickup.transform.InverseTransformPoint(sync.transform.position);
            startRot = Quaternion.Inverse(primaryPickup.transform.rotation) * sync.transform.rotation;
            startPosPrimary = sync.transform.InverseTransformPoint(primaryPickup.transform.position);
            startRotPrimary = Quaternion.Inverse(sync.transform.rotation) * primaryPickup.transform.rotation;
            currentPickupPos = startPos;
            currentPickupRot = startRot;
            currentPosPrimary = startPosPrimary;
            currentRotPrimary = startRotPrimary;
            RecordGrabOffset();
        }

        Vector3 currentPickupPos;
        Quaternion currentPickupRot;
        Vector3 currentPosPrimary;
        Quaternion currentRotPrimary;
        public void RecordCurrentOffsets()
        {
            currentPickupPos = primaryPickup.transform.InverseTransformPoint(sync.transform.position);
            currentPickupRot = Quaternion.Inverse(primaryPickup.transform.rotation) * sync.transform.rotation;
            currentPosPrimary = sync.transform.InverseTransformPoint(primaryPickup.transform.position);
            currentRotPrimary = Quaternion.Inverse(sync.transform.rotation) * primaryPickup.transform.rotation;
        }

        public void RecordGrabOffset()
        {
            startOffset = Quaternion.Inverse(primaryPickup.transform.rotation) * (GetGrabPos(sync) - GetGrabPos(primaryPickup));
        }

        public Vector3 GetGrabPos(SmartObjectSync pickup)
        {
            if (pickup.pickup.orientation == VRC_Pickup.PickupOrientation.Gun)
            {
                if (Utilities.IsValid(pickup.pickup.ExactGun))
                {
                    return pickup.pickup.ExactGun.position;
                }
            }
            else if (pickup.pickup.orientation == VRC_Pickup.PickupOrientation.Grip && Utilities.IsValid(pickup.pickup.ExactGrip))
            {
                if (Utilities.IsValid(pickup.pickup.ExactGrip))
                {
                    return pickup.pickup.ExactGrip.position;
                }
            }
            return pickup.transform.position;
        }
        Vector3 startOffset;
        Vector3 newOffset;
        Vector3 worldOffset;
        Vector3 newWorldOffset;
        Quaternion adjustmentRotation;
        Vector3 axis;
        float angle;
        public override void PostLateUpdate()
        {
            if (!Utilities.IsValid(sync))
            {
                return;
            }
            if (sync.IsHeld())
            {
                if (primaryPickup.IsHeld())
                {
                    ConstrainObjectToGrips();
                }
                else
                {
                    ConstrainPrimaryPickupToThis();
                    RecordTransforms();
                }
            }
            else if (primaryPickup.IsHeld())
            {
                ConstrainThisToPrimaryPickup();
            }
            else
            {
                enabled = false;
            }
        }

        public void ConstrainObjectToGrips()
        {
            if (sync.pickup.IsHeld)
            {
                if (sync.state == SmartObjectSync.STATE_NO_HAND_HELD)
                {
                    sync.noHand_CalcParentTransform();
                }
                else
                {
                    sync.bone_CalcParentTransform();
                }
                sync.generic_Interpolate(1.0f);
            }
            newOffset = Quaternion.Inverse(primaryPickup.transform.rotation) * (GetGrabPos(sync) - GetGrabPos(primaryPickup));
            worldOffset = primaryPickup.transform.rotation * startOffset;
            newWorldOffset = primaryPickup.transform.rotation * newOffset;
            adjustmentRotation = Quaternion.FromToRotation(worldOffset, newWorldOffset);
            adjustmentRotation.ToAngleAxis(out angle, out axis);
            constrainedObject.position = primaryPickup.transform.TransformPoint(constrainedStartPosPrimary);
            constrainedObject.rotation = primaryPickup.transform.rotation * constrainedStartRotPrimary;
            constrainedObject.RotateAround(GetGrabPos(primaryPickup), axis, angle);
            constrainedObject.position += Vector3.Lerp(Vector3.zero, sync.transform.position - constrainedObject.TransformPoint(constrainedStartPosInverse), positionBias);
        }
        Vector3 currentPos;
        Quaternion currentRot;
        public void ConstrainThisToPrimaryPickup()
        {
            currentPos = primaryPickup.transform.position;
            currentRot = primaryPickup.transform.rotation;
            if (resetOnDrop)
            {
                primaryPickup.transform.position = currentPos;
                primaryPickup.transform.rotation = currentRot;
                sync.transform.position = primaryPickup.transform.TransformPoint(startPos);
                sync.transform.rotation = currentRot * startRot;
                constrainedObject.position = primaryPickup.transform.TransformPoint(constrainedStartPosPrimary);
                constrainedObject.rotation = currentRot * constrainedStartRotPrimary;
            }
            else
            {
                primaryPickup.transform.position = currentPos;
                primaryPickup.transform.rotation = currentRot;
                sync.transform.position = primaryPickup.transform.TransformPoint(currentPickupPos);
                sync.transform.rotation = currentRot * currentPickupRot;
                ConstrainObjectToGrips();
            }
        }
        [System.NonSerialized]
        public Vector3 lastPos;
        [System.NonSerialized]
        public Quaternion lastRot;
        public void ConstrainPrimaryPickupToThis()
        {
            currentPos = sync.transform.position;
            currentRot = sync.transform.rotation;
            if (resetOnDrop)
            {
                primaryPickup.transform.position = sync.transform.TransformPoint(startPosPrimary);
                primaryPickup.transform.rotation = currentRot * startRotPrimary;
                sync.transform.position = currentPos;
                sync.transform.rotation = currentRot;
                constrainedObject.position = sync.transform.TransformPoint(constrainedStartPos);
                constrainedObject.rotation = currentRot * constrainedStartRot;
            }
            else
            {
                primaryPickup.transform.position = sync.transform.TransformPoint(currentPosPrimary);
                primaryPickup.transform.rotation = currentRot * currentRotPrimary;
                sync.transform.position = currentPos;
                sync.transform.rotation = currentRot;
                ConstrainObjectToGrips();
            }
            primaryPickup.rigid.velocity = Vector3.zero;
            primaryPickup.rigid.angularVelocity = Vector3.zero;
            calcedVel = CalcVel();
            calcedSpin = CalcSpin();
        }

        public void RecordTransforms()
        {
            lastPos = primaryPickup.transform.position;
            lastRot = primaryPickup.transform.rotation;
        }
        public Vector3 CalcVel()
        {
            return (sync.transform.TransformPoint(currentPosPrimary) - lastPos) / Time.deltaTime;
        }

        public Vector3 CalcSpin()
        {
            //angular velocity is normalized rotation axis * angle in radians: https://answers.unity.com/questions/49082/rotation-quaternion-to-angular-velocity.html
            (Quaternion.Inverse(lastRot) * currentRot * currentRotPrimary).ToAngleAxis(out angle, out axis);
            return axis * angle * Mathf.Deg2Rad / Time.deltaTime;
        }
    }
}