
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class StackableState : GenericAttachmentState
    {
        [System.NonSerialized]
        public Rigidbody rootParent;
        [System.NonSerialized]
        public Collider immediateParent;
        public float maxTiltAngle = 90f;


        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(rootParentName))]
        public string _rootParentName;
        
        public string rootParentName{
            get => _rootParentName;
            set
            {
                GameObject parentObj = GameObject.Find(value);
                if (Utilities.IsValid(parentObj))
                {
                    rootParent = parentObj.GetComponent<Rigidbody>();
                    if (!Utilities.IsValid(rootParent)){
                        _rootParentName = "";
                        if (sync.IsLocalOwner() && IsActiveState())
                        {
                            ExitState();
                        }
                    } else
                    {
                        _rootParentName = value;
                        if (sync.IsLocalOwner() && !IsActiveState())
                        {
                            EnterState();
                        }
                    }
                    if (sync.IsLocalOwner())
                    {
                        CalcParentTransform();
                        RequestSerialization();
                        //make sure serialization happens this frame
                        OnSmartObjectSerialize();
                    }
                } else
                {
                    rootParent = null;
                    if (sync.IsLocalOwner() && IsActiveState())
                    {
                        ExitState();
                    }
                }
            }
        }
        
        public override void CalcParentTransform()
        {
            if (Utilities.IsValid(rootParent))
            {
                parentPos = rootParent.transform.position;
                parentRot = rootParent.transform.rotation;
            }
        }

        public override void OnExitState()
        {
            base.OnExitState();
            _rootParentName = "";
            rootParent = null;
        }

        public override void Interpolate(float interpolation)
        {
            if (!Utilities.IsValid(rootParent))
            {
                if (sync.IsLocalOwner())
                {
                    ExitState();
                }
                return;
            }
            CalcParentTransform();
            if (sync.IsLocalOwner())
            {
                //we allow changes in position along the gravity vector
                transform.position = parentPos + parentRot * sync.pos;
                transform.rotation = parentRot * sync.rot;
                sync.rigid.velocity = rootParent.velocity;
                sync.rigid.angularVelocity = rootParent.angularVelocity;
                Vector3 projectedDistance = Vector3.Project(rootParent.transform.position - sync.transform.position, Physics.gravity);
                if (Vector3.Angle(projectedDistance, Physics.gravity) > maxTiltAngle)
                {
                    //we're no longer balanced
                    sync._print("no longer balanced");
                    ExitState();
                    return;
                }
            } else
            {
                transform.position = sync.HermiteInterpolatePosition(parentPos + parentRot * startPos, Vector3.zero, parentPos + parentRot * sync.pos, Vector3.zero, interpolation);
                transform.rotation = sync.HermiteInterpolateRotation(parentRot * startRot, Vector3.zero, parentRot * sync.rot, Vector3.zero, interpolation);
                sync.rigid.velocity = Vector3.zero;
                sync.rigid.angularVelocity = Vector3.zero;
            }
        }

        StackableState otherStack;
        Rigidbody lastCollided;
        public void OnCollisionEnter(Collision other)
        {
            if (!Utilities.IsValid(other) || !Utilities.IsValid(other.collider) || !Utilities.IsValid(other.rigidbody))
            {
                return;
            }
            Vector3 contactVector = other.transform.position - sync.transform.position;
            if (Vector3.Angle(contactVector, Physics.gravity) < 90)
            {
                sync._print("We landed on something");
                //we landed on top of something else
                if (sync.IsAttachedToPlayer() || (sync.state >= SmartObjectSync.STATE_CUSTOM && !IsActiveState()))
                {
                    sync._print("we're being held or something");
                    lastCollided = other.rigidbody;
                } else
                {
                    Stack(other.rigidbody);
                }
            }
            else
            {
                sync._print("something landed on us");
                //something landed on us
                if (!sync.IsLocalOwner())
                {
                    //We only let the bottom one do the work
                    return;
                }

                otherStack = other.rigidbody.GetComponent<StackableState>();
                if (Utilities.IsValid(otherStack) && !otherStack.sync.IsAttachedToPlayer() && !(otherStack.sync.state >= SmartObjectSync.STATE_CUSTOM && !otherStack.IsActiveState()))
                {
                    otherStack.sync.TakeOwnership(false);
                    otherStack.Stack(sync.rigid);
                }
            }
        }


        public void OnCollisionExit(Collision other)
        {
            if (Utilities.IsValid(other.rigidbody) == lastCollided)
            {
                lastCollided = null;
            }
        }

        public override void OnDrop()
        {
            Stack(lastCollided);
            lastCollided = null;
        }

        public void Stack(Rigidbody stackRoot)
        {
            if (!sync.IsLocalOwner() || !Utilities.IsValid(stackRoot) || stackRoot == rootParent || sync.IsAttachedToPlayer() || (sync.state >= SmartObjectSync.STATE_CUSTOM && !IsActiveState()))
            {
                sync._print("!sync.IsLocalOwner() " + !sync.IsLocalOwner());
                sync._print("!Utilities.IsValid(stackRoot) " + !Utilities.IsValid(stackRoot));
                sync._print("stackRoot == rootParent " + (stackRoot == rootParent));
                sync._print("sync.IsAttachedToPlayer() " + sync.IsAttachedToPlayer());
                sync._print("(sync.state >= SmartObjectSync.STATE_CUSTOM && !IsActiveState()) " + (sync.state >= SmartObjectSync.STATE_CUSTOM && !IsActiveState()));
                return;
            }
            if (Utilities.IsValid(rootParent) && Vector3.Distance(rootParent.transform.position, sync.transform.position) < Vector3.Distance(stackRoot.transform.position, sync.transform.position))
            {
                sync._print("second");
                return;
            }
            sync._print("Stack");

            otherStack = stackRoot.GetComponent<StackableState>();
            int chaincounter = 10;
            while (Utilities.IsValid(otherStack) && Utilities.IsValid(otherStack.rootParent) && chaincounter > 0)
            {
                if (otherStack.rootParent == sync.rigid)
                {
                    //circular dependency
                    sync._print("circular dependency");
                    return;
                }
                stackRoot = otherStack.rootParent;
                otherStack = stackRoot.GetComponent<StackableState>();
                chaincounter--;
            }

            Vector3 contactVector = stackRoot.transform.position - sync.transform.position;
            if (Vector3.Angle(contactVector, Physics.gravity) > maxTiltAngle)
            {
                sync._print("angle was bad");
                return;
            }
            rootParentName = GetFullPath(stackRoot.transform);
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