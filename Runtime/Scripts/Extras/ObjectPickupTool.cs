
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ObjectPickupTool : UdonSharpBehaviour
    {
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(closed))]
        public bool _closed;
        
        public bool closed{
            get => _closed;
            set
            {
                _closed = value;
                if (animator)
                {
                    animator.SetBool(animatorParameter, value);
                }
            }
        }
        public SphereCollider objectPickupCollider;
        public SmartObjectSync sync;
        public Animator animator;
        public string animatorParameter = "closed";
        public LayerMask layers;

        [System.NonSerialized]
        public SmartObjectSync[] pickedObjs = new SmartObjectSync[0];

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        void Reset()
        {
            if (!sync)
            {
                sync = GetComponent<SmartObjectSync>();
            }
            if (!animator)
            {
                animator = GetComponent<Animator>();
            }
        }
#endif
        void Start()
        {
        }

        public override void OnDrop()
        {
            closed = false;
            RequestSerialization();
            DropObjects();
        }

        public override void OnPickupUseDown()
        {
            closed = true;
            RequestSerialization();
            if (objectPickupCollider)
            {
                foreach (Collider col in Physics.OverlapSphere(objectPickupCollider.transform.position, objectPickupCollider.radius * objectPickupCollider.transform.lossyScale.x, layers))
                {
                    if (!Utilities.IsValid(col) || col.gameObject == gameObject)
                    {
                        continue;
                    }

                    SmartObjectSync newObj = col.GetComponent<SmartObjectSync>();
                    if (newObj)
                    {
                        PickupObject(newObj);
                    }
                }
            }
        }
        public override void OnPickupUseUp()
        {
            closed = false;
            RequestSerialization();
            DropObjects();
        }

        public void PickupObject(SmartObjectSync newObj)
        {
            if (!newObj)
            {
                return;
            }
            if (sync.state == SmartObjectSync.STATE_LEFT_HAND_HELD)
            {
                newObj.AttachToBone(HumanBodyBones.LeftHand);
            }
            else if (sync.state == SmartObjectSync.STATE_RIGHT_HAND_HELD)
            {
                newObj.AttachToBone(HumanBodyBones.RightHand);
            }
            else if (sync.state == SmartObjectSync.STATE_NO_HAND_HELD)
            {
                newObj.AttachToBone(HumanBodyBones.Head);
            }
            else
            {
                //chopsticks aren't being held
                return;
            }
            SmartObjectSync[] newArray = new SmartObjectSync[pickedObjs.Length + 1];
            pickedObjs.CopyTo(newArray, 0);
            newArray[newArray.Length - 1] = newObj;
            pickedObjs = newArray;
        }

        public void DropObjects()
        {
            foreach (SmartObjectSync obj in pickedObjs)
            {
                if (obj && (obj.state < 0))
                {
                    obj.state = SmartObjectSync.STATE_INTERPOLATING;
                }
            }
            pickedObjs = new SmartObjectSync[0];
        }
    }
}