
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
    [CustomEditor(typeof(InventoryManager))]

    public class InventoryManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button(new GUIContent("Auto Setup All Inventory Objects")))
            {
                Setup(target as InventoryManager);
            }

            if (target && UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            base.OnInspectorGUI();
        }

        public static void Setup(InventoryManager manager)
        {
            foreach (InventoryState state in GameObject.FindObjectsOfType<InventoryState>())
            {
                InventoryStateEditor.Setup(state, manager);
            }
        }
    }
}

#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class InventoryManager : UdonSharpBehaviour
    {
        public Vector3 desktopInventoryPlacement = new Vector3(0, -0.25f, 0.5f);
        public float inventorySpacing = 0.05f;
        public float inventoryInterpolationTime = 0.25f;
        public float inventoryHitboxSize = 0.001f;//accounts for floating point errors

        [System.NonSerialized]
        public InventoryState[] leftInventoryCache = new InventoryState[0];

        [System.NonSerialized]
        public InventoryState[] rightInventoryCache = new InventoryState[0];
        public bool AllowVR = true;
        public bool AllowDesktopShortcuts = true;
        public KeyCode addToInventoryShortcut = KeyCode.B;
        [System.NonSerialized]
        public float leftInventoryOpen = 1.0f;
        [System.NonSerialized]
        public float rightInventoryOpen = 1.0f;

        [System.NonSerialized]
        public float inventoryOffset = 0.25f;

        public Vector3 debugOffset;

        public GameObject debugLeftPoint;
        public GameObject debugRightPoint;

        [Tooltip("Negative values mean it's unlimited")]
        public int maxLeftInventorySize = -1001;
        [Tooltip("Negative values mean it's unlimited")]
        public int maxRightInventorySize = -1001;
        void Start()
        {

        }

        public void AddToLeftInventory(InventoryState state)
        {
            state.inventoryIndex = -1 - leftInventoryCache.Length;
            InventoryState[] newCache = new InventoryState[leftInventoryCache.Length + 1];
            leftInventoryCache.CopyTo(newCache, 0);
            newCache[newCache.Length - 1] = state;
            leftInventoryCache = newCache;


            foreach (InventoryState s in leftInventoryCache)
            {
                //restart interpolation
                s.OnInterpolationStart();
            }
        }
        public void AddToRightInventory(InventoryState state)
        {
            state.inventoryIndex = rightInventoryCache.Length;
            InventoryState[] newCache = new InventoryState[rightInventoryCache.Length + 1];
            rightInventoryCache.CopyTo(newCache, 0);
            newCache[newCache.Length - 1] = state;
            rightInventoryCache = newCache;


            foreach (InventoryState s in rightInventoryCache)
            {
                //restart interpolation
                s.OnInterpolationStart();
            }
        }

        public void RemoveFromInventory(int index)
        {
            if (index < 0)
            {
                RemoveFromLeftInventory(-1 - index);
            }
            else
            {
                RemoveFromRightInventory(index);
            }
        }

        private void RemoveFromLeftInventory(int index)
        {
            if (index >= 0 && index < leftInventoryCache.Length)
            {
                InventoryState[] newCache = new InventoryState[leftInventoryCache.Length - 1];
                for (int i = 0; i < newCache.Length; i++)
                {
                    if (i < index)
                    {
                        newCache[i] = leftInventoryCache[i];
                    }
                    else if (i >= index)
                    {
                        newCache[i] = leftInventoryCache[i + 1];
                        newCache[i].inventoryIndex = -1 - i;
                    }
                }
                leftInventoryCache = newCache;

                foreach (InventoryState state in leftInventoryCache)
                {
                    //restart interpolation
                    state.OnInterpolationStart();
                }
            }
        }
        private void RemoveFromRightInventory(int index)
        {
            if (index >= 0 && index < rightInventoryCache.Length)
            {
                InventoryState[] newCache = new InventoryState[rightInventoryCache.Length - 1];
                for (int i = 0; i < newCache.Length; i++)
                {
                    if (i < index)
                    {
                        newCache[i] = rightInventoryCache[i];
                    }
                    else if (i >= index)
                    {
                        newCache[i] = rightInventoryCache[i + 1];
                        newCache[i].inventoryIndex = i;
                    }
                }
                rightInventoryCache = newCache;

                foreach (InventoryState state in rightInventoryCache)
                {
                    //restart interpolation
                    state.OnInterpolationStart();
                }
            }
        }
        float lastHapticLeft = -1001f;
        float lastHapticRight = -1001f;
        public override void PostLateUpdate()
        {
            if (Networking.LocalPlayer.IsUserInVR())
            {
                VRCPlayerApi.TrackingData leftData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
                VRCPlayerApi.TrackingData rightData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
                VRCPlayerApi.TrackingData headData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

                leftInventoryOpen = Mathf.Clamp01(Mathf.Min((45f - Vector3.Angle(leftData.rotation * Vector3.up, Vector3.up)) / 15f, (45f - Vector3.Angle(leftData.position - headData.position, headData.rotation * Vector3.forward)) / 15.0f));
                rightInventoryOpen = Mathf.Clamp01(Mathf.Min((45f - Vector3.Angle(rightData.rotation * Vector3.down, Vector3.up)) / 15f, (45f - Vector3.Angle(rightData.position - headData.position, headData.rotation * Vector3.forward)) / 15.0f));

                if (lastHapticLeft + 0.1f < Time.timeSinceLevelLoad)
                {
                    VRC_Pickup leftPickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);
                    InventoryState leftState = leftPickup == null ? null : leftPickup.GetComponent<InventoryState>();
                    if (leftState && leftState.IsHoveringRightInventory())
                    {
                        Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.1f, 1.0f, 0.1f);
                        lastHapticLeft = Time.timeSinceLevelLoad;
                    }
                }
                if (lastHapticRight + 0.1f < Time.timeSinceLevelLoad)
                {
                    VRC_Pickup rightPickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
                    InventoryState rightState = rightPickup == null ? null : rightPickup.GetComponent<InventoryState>();
                    if (rightState && rightState.IsHoveringLeftInventory())
                    {
                        Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.1f, 1.0f, 0.1f);
                        lastHapticRight = Time.timeSinceLevelLoad;
                    }
                }

                return;
            }
            if (!AllowDesktopShortcuts)
            {
                return;
            }

            if (Input.GetKeyDown(addToInventoryShortcut) && (maxLeftInventorySize < 0 || maxLeftInventorySize > leftInventoryCache.Length))
            {
                VRC_Pickup pickup = Networking.LocalPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
                if (!pickup)
                {
                    return;
                }
                InventoryState state = pickup.GetComponent<InventoryState>();
                if (!state)
                {
                    return;
                }
                state.EnterState();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                if (leftInventoryCache.Length > 0 && leftInventoryCache[0] != null)
                {
                    leftInventoryCache[0].ExitState();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                if (leftInventoryCache.Length > 1 && leftInventoryCache[1] != null)
                {
                    leftInventoryCache[1].ExitState();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                if (leftInventoryCache.Length > 2 && leftInventoryCache[2] != null)
                {
                    leftInventoryCache[2].ExitState();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                if (leftInventoryCache.Length > 3 && leftInventoryCache[3] != null)
                {
                    leftInventoryCache[3].ExitState();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                if (leftInventoryCache.Length > 4 && leftInventoryCache[4] != null)
                {
                    leftInventoryCache[4].ExitState();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
            {
                if (leftInventoryCache.Length > 5 && leftInventoryCache[5] != null)
                {
                    leftInventoryCache[5].ExitState();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
            {
                if (leftInventoryCache.Length > 6 && leftInventoryCache[6] != null)
                {
                    leftInventoryCache[6].ExitState();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
            {
                if (leftInventoryCache.Length > 7 && leftInventoryCache[7] != null)
                {
                    leftInventoryCache[7].ExitState();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
            {
                if (leftInventoryCache.Length > 8 && leftInventoryCache[8] != null)
                {
                    leftInventoryCache[8].ExitState();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
            {
                if (leftInventoryCache.Length > 9 && leftInventoryCache[9] != null)
                {
                    leftInventoryCache[9].ExitState();
                }
            }
        }
    }
}