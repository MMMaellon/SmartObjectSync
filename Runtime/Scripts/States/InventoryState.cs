
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Generic;

namespace MMMaellon
{
    [CustomEditor(typeof(InventoryState)), CanEditMultipleObjects]

    public class InventoryStateEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (!target)
            {
                return;
            }
            InventoryState state = target as InventoryState;
            if (!state)
            {
                return;
            }
            if (!state.manager)
            {
                EditorGUILayout.HelpBox(@"An InventoryManager is required for this state to work. Please add one to the scene and then hit the 'Auto Setup' button on it.", MessageType.Warning);
            }

            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            base.OnInspectorGUI();
        }

        public static void Setup(InventoryState state, InventoryManager manager)
        {
            SerializedObject serialized = new SerializedObject(state);
            serialized.FindProperty("manager").objectReferenceValue = manager;
            serialized.ApplyModifiedProperties();
        }
    }
}
#endif

namespace MMMaellon
{
    public class InventoryState : SmartObjectSyncState
    {
        public InventoryManager manager;
        [System.NonSerialized, UdonSynced(UdonSyncMode.None)]
        public int inventoryIndex = 0;//0 or greater means it's in the left hand, -1 or less means its in the left hand inventory

        [System.NonSerialized]
        public int nonstatic_leftInventoryItemCount = 0;
        [System.NonSerialized]
        public int nonstatic_rightInventoryItemCount = 0;
        public float inventoryItemScale = 0.1f;
        [System.NonSerialized]
        public Vector3 startScale;
        [System.NonSerialized]
        public bool useLeftHandInventory = true;

        [System.NonSerialized]
        public float interpolationStart = -1001f;
        [System.NonSerialized]
        public Vector3 startPos;
        [System.NonSerialized]
        public Quaternion startRot;
        Collider col;
        [System.NonSerialized]
        public Vector3 offset;

        void Start()
        {
            startScale = transform.localScale;
            col = sync.GetComponent<Collider>();
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public override void Reset()
        {
            if (!Utilities.IsValid(manager) && !SetupRan)
            {
                InventoryManager foundManager = GameObject.FindObjectOfType<InventoryManager>();
                if (Utilities.IsValid(foundManager))
                {
                    InventoryStateEditor.Setup(this, foundManager);
                }
            }
            base.Reset();
            SetupRan = true;
        }
#endif

        public override void OnEnterState()
        {
            if (sync.IsLocalOwner())
            {
                if (useLeftHandInventory)
                {
                    manager.AddToLeftInventory(this);
                }
                else
                {
                    manager.AddToRightInventory(this);
                }
            }
            sync.rigid.detectCollisions = false;
        }

        public override void OnExitState()
        {
            if (sync.IsLocalOwner())
            {
                manager.RemoveFromInventory(inventoryIndex);
            }
            transform.localScale = startScale;
            sync.rigid.detectCollisions = true;
        }

        public override void OnInterpolationStart()
        {
            interpolationStart = Time.timeSinceLevelLoad;
            if (Utilities.IsValid(sync.owner))
            {
                startPos = Quaternion.Inverse(sync.owner.GetRotation()) * (transform.position - sync.owner.GetPosition());
                startRot = Quaternion.Inverse(sync.owner.GetRotation()) * (transform.rotation);
            }
            offset = CalcOffset();
        }
        public override void Interpolate(float interpolation)
        {
            if (Utilities.IsValid(sync.owner))
            {
                float slowerInterpolation = manager.inventoryInterpolationTime <= 0 ? 1.0f : Mathf.Clamp01((Time.timeSinceLevelLoad - interpolationStart) / manager.inventoryInterpolationTime);
                transform.position = sync.HermiteInterpolatePosition(sync.owner.GetPosition() + sync.owner.GetRotation() * startPos, Vector3.zero, CalcPos(), Vector3.zero, slowerInterpolation);
                transform.rotation = sync.HermiteInterpolateRotation(sync.owner.GetRotation() * startRot, Vector3.zero, CalcRot(), Vector3.zero, slowerInterpolation);
                if (sync.IsLocalOwner())
                {
                    transform.localScale = Vector3.Lerp(transform.localScale, Vector3.Lerp(new Vector3(0.000001f, 0.000001f, 0.000001f), startScale * inventoryItemScale, inventoryIndex < 0 ? manager.leftInventoryOpen : manager.rightInventoryOpen), slowerInterpolation);
                } else
                {
                    transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(0.000001f, 0.000001f, 0.000001f), slowerInterpolation);
                }
            }
            if (sync.rigid)
            {
                sync.rigid.velocity = Vector3.zero;
                sync.rigid.angularVelocity = Vector3.zero;
                if (sync.IsLocalOwner())
                {
                    sync.rigid.detectCollisions = sync.owner.IsUserInVR() && ((inventoryIndex < 0 && manager.leftInventoryOpen >= 1.0f) || (inventoryIndex >= 0 && manager.rightInventoryOpen >= 1.0f));
                }
            }
        }

        public Vector3 CalcPos()
        {
            if (Utilities.IsValid(sync.owner))
            {
                if (sync.IsLocalOwner() && !sync.owner.IsUserInVR())
                {
                    VRCPlayerApi.TrackingData data = sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                    return data.position + (data.rotation * manager.desktopInventoryPlacement) + (sync.owner.GetRotation() * offset);
                }
                if (inventoryIndex < 0)
                {
                    Vector3 leftHand = sync.owner.GetBonePosition(HumanBodyBones.LeftHand);
                    if (leftHand == Vector3.zero)
                    {
                        leftHand = sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                    }
                    if (leftHand == Vector3.zero)
                    {
                        leftHand = sync.owner.GetPosition();
                    }
                    if(!sync.IsLocalOwner()){
                        return leftHand;
                    }
                    Vector3 projected = Vector3.ProjectOnPlane(leftHand - sync.owner.GetPosition(), sync.owner.GetRotation() * Vector3.left) + sync.owner.GetPosition();
                    return Vector3.Lerp(leftHand, Vector3.Lerp(projected, leftHand, 0.5f) + sync.owner.GetRotation() * offset, manager.leftInventoryOpen);
                } else
                {
                    Vector3 rightHand = sync.owner.GetBonePosition(HumanBodyBones.RightHand);
                    if (rightHand == Vector3.zero)
                    {
                        rightHand = sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                    }
                    if (rightHand == Vector3.zero)
                    {
                        rightHand = sync.owner.GetPosition();
                    }
                    if(!sync.IsLocalOwner()){
                        return rightHand;
                    }
                    Vector3 projected = Vector3.ProjectOnPlane(rightHand - sync.owner.GetPosition(), sync.owner.GetRotation() * Vector3.left) + sync.owner.GetPosition();
                    return Vector3.Lerp(rightHand, Vector3.Lerp(projected, rightHand, 0.5f) + sync.owner.GetRotation() * offset, manager.rightInventoryOpen);
                }
            }
            return transform.position;
        }

        public Quaternion CalcRot()
        {
            if (sync.IsLocalOwner())
            {
                return sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
            }
            return transform.rotation;
        }

        public override bool OnInterpolationEnd()
        {
            return true;
        }

        public override void OnSmartObjectSerialize()
        {
        }
        public Vector3 CalcOffset()
        {
            if (inventoryIndex < 0)
            {
                return Vector3.up * manager.inventoryOffset + Vector3.right * manager.inventorySpacing * ((-1 - inventoryIndex) - ((manager.leftInventoryCache.Length - 1) / 2.0f));
            }
            else
            {
                return Vector3.up * manager.inventoryOffset + Vector3.right * manager.inventorySpacing * (inventoryIndex - ((manager.rightInventoryCache.Length - 1) / 2.0f));
            }
        }

        public override void OnPickup()
        {
            useLeftHandInventory = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left) != sync.pickup;
            //we measure where the middle finger is and assume the wrist is opposite it
            //we do this on pickup to account for changing avatars in between uses
            if (useLeftHandInventory)
            {
                Vector3 leftMiddleFinger = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftMiddleProximal);
                Vector3 leftHand = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftHand);
                if (leftMiddleFinger != Vector3.zero && leftHand != Vector3.zero)
                {
                    manager.inventoryOffset = Vector3.Distance(leftHand, leftMiddleFinger);
                }
            }
            else
            {
                Vector3 rightMiddleFinger = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.RightMiddleProximal);
                Vector3 rightHand = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.RightHand);
                if (rightMiddleFinger != Vector3.zero && rightHand != Vector3.zero)
                {
                    manager.inventoryOffset = Vector3.Distance(rightHand, rightMiddleFinger);
                }
            }
        }

        public override void OnDrop()
        {
            sync._print("Inventory OnDrop");
            if (useLeftHandInventory)
            {
                if (IsHoveringLeftInventory())
                {
                    EnterState();
                }
            }
            else
            {
                if (IsHoveringRightInventory())
                {
                    EnterState();
                }
            }
        }

        public bool IsHoveringLeftInventory()
        {
            Vector3 leftHand = sync.owner.GetBonePosition(HumanBodyBones.LeftHand);
            Vector3 inventoryPoint = leftHand != Vector3.zero ? leftHand : sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
            if (col && col.bounds.Contains(inventoryPoint))
            {
                return true;
            }
            else
            {
                Vector3 closestPoint = col == null ? transform.position : col.ClosestPoint(inventoryPoint);
                if (Vector3.Distance(closestPoint, inventoryPoint) < manager.inventoryHitboxSize)
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsHoveringRightInventory()
        {
            Vector3 rightHand = sync.owner.GetBonePosition(HumanBodyBones.RightHand);
            Vector3 inventoryPoint = rightHand != Vector3.zero ? rightHand : sync.owner.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
            if (col && col.bounds.Contains(inventoryPoint))
            {
                return true;
            }
            else
            {
                Vector3 closestPoint = col == null ? transform.position : col.ClosestPoint(inventoryPoint);
                if (Vector3.Distance(closestPoint, inventoryPoint) < manager.inventoryHitboxSize)
                {
                    return true;
                }
            }
            return false;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            base.OnOwnershipTransferred(player);
            if (IsActiveState() && sync.IsLocalOwner())
            {
                ExitState();
            }
        }
    }
}