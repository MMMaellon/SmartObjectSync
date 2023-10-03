
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Generic;
#endif

namespace MMMaellon.SmartObjectSyncExtra
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual), RequireComponent(typeof(ConfigurableJoint))]
    public class JointAttachmentState : GenericAttachmentState
    {
        [HideInInspector]
        public ConfigurableJoint joint;
        [HideInInspector]
        public JointAttachmentStateSetter setter;
        public bool attachOnCollision = true;

        [System.NonSerialized]
        public Rigidbody connectedBody;

        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(parentRigidName))]
        string _parentRigidName = "";

        [System.NonSerialized, FieldChangeCallback(nameof(parentRigid))]
        public Rigidbody _parentRigid = null;
        public Rigidbody parentRigid
        {
            get => _parentRigid;
            set
            {
                _parentRigid = value;
                joint.connectedBody = value;
            }
        }
        public string parentRigidName
        {
            get => _parentRigidName;
            set
            {
                if (!Utilities.IsValid(value) || value == "")
                {
                    _parentRigidName = "";
                    parentRigid = null;
                    return;
                }
                GameObject parentObj = GameObject.Find(value);
                if (Utilities.IsValid(parentObj))
                {
                    _parentRigidName = value;
                    parentRigid = parentObj.GetComponent<Rigidbody>();
                    if (IsActiveState())
                    {
                        if (Utilities.IsValid(transform.parent))
                        {
                            CalcParentTransform();
                            sync.startPos = CalcPos();
                            sync.startRot = CalcRot();
                            startPos = sync.startPos;
                            startRot = sync.startRot;
                            if (sync.IsLocalOwner())
                            {
                                sync.pos = startPos;
                                sync.rot = startRot;
                            }
                        }
                        else if (sync.IsLocalOwner())
                        {
                            ExitState();
                        }
                    }
                    return;
                }
                _parentRigidName = "";
                parentRigid = null;
            }
        }
        void Start()
        {
            if (Utilities.IsValid(joint.connectedBody) && sync.IsLocalOwner())
            {
                parentRigidName = GetFullPath(joint.connectedBody);
                EnterState();
            }
            else
            {
                joint.xMotion = ConfigurableJointMotion.Free;
                joint.yMotion = ConfigurableJointMotion.Free;
                joint.zMotion = ConfigurableJointMotion.Free;
                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularYMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Free;
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public override void Reset()
        {
            base.Reset();
            SetupJointAttachmentState(this);
        }

        public static void SetupJointAttachmentState(JointAttachmentState state)
        {
            if (!Utilities.IsValid(state) || (state.joint != null && state.joint.gameObject == state.gameObject && state.setter != null && state.setter.gameObject == state.gameObject))
            {
                //null or already set up
                return;
            }
            SerializedObject obj = new SerializedObject(state);
            obj.FindProperty("joint").objectReferenceValue = state.GetComponent<ConfigurableJoint>();
            obj.FindProperty("setter").objectReferenceValue = state.GetComponent<JointAttachmentStateSetter>();
            obj.ApplyModifiedProperties();
        }
#endif
        public override void OnEnterState()
        {
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
        }

        public override void OnExitState()
        {
            joint.xMotion = ConfigurableJointMotion.Free;
            joint.yMotion = ConfigurableJointMotion.Free;
            joint.zMotion = ConfigurableJointMotion.Free;
            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;
            lastDetach = Time.timeSinceLevelLoad;
            parentRigidName = "";
        }
        public override void OnInterpolationStart()
        {
            if (Utilities.IsValid(setter))
            {
                startPos = transform.position;
                startRot = transform.rotation;
            }
        }

        public override void Interpolate(float interpolation)
        {
            if (Utilities.IsValid(setter))
            {
                setter.Interpolate(interpolation);
            } else
            {
                base.Interpolate(interpolation);
            }
        }

        public override bool OnInterpolationEnd()
        {
            //resets the offsets and stuff
            joint.connectedBody = null;
            joint.connectedBody = parentRigid;
            return false;
            // return sync.IsLocalOwner() && base.OnInterpolationEnd();
        }

        public override void CalcParentTransform()
        {
            if (!Utilities.IsValid(joint.connectedBody))
            {
                if (sync.IsLocalOwner())
                {
                    ExitState();
                }
                return;
            }
            parentPos = joint.connectedBody.transform.position;
            parentRot = joint.connectedBody.transform.rotation;
        }

        public void Attach(Rigidbody r)
        {
            sync.TakeOwnership(false);
            if (IsActiveState())
            {
                ExitState();
            }
            parentRigidName = GetFullPath(r);
            EnterState();
        }
        public string GetFullPath(Rigidbody target)
        {
            Transform pathBuilder = target.transform;
            string tempName = "";
            while (Utilities.IsValid(pathBuilder))
            {
                tempName = "/" + pathBuilder.name + tempName;
                pathBuilder = pathBuilder.parent;
            }
            return tempName;
        }

        public float cooldown = 0.5f;
        float lastDetach = -1001f;
        Joint otherJoint;
        public void OnCollisionEnter(Collision collision)
        {
            if (!attachOnCollision || !sync.IsLocalOwner() || lastDetach + cooldown > Time.timeSinceLevelLoad || !Utilities.IsValid(collision.rigidbody))
            {
                return;
            }
            otherJoint = collision.rigidbody.GetComponent<Joint>();
            if(Utilities.IsValid(otherJoint))
            {
                //chains of joints crashes Unity for me, so we're gonna disable it
                return;
            }
            if (!sync.IsAttachedToPlayer() && sync.state < SmartObjectSync.STATE_CUSTOM)
            {
                Attach(collision.rigidbody);
            }
        }

        // public override Vector3 CalcPos()
        // {
        //     return Utilities.IsValid(setter) ? setter.CalcPos() : base.CalcPos();
        // }

        // public override Quaternion CalcRot()
        // {
        //     return Utilities.IsValid(setter) ? setter.CalcRot() : base.CalcRot();
        // }
    }
}