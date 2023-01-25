
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Generic;


namespace MMMaellon
{
    [CustomEditor(typeof(SmartObjectSync)), CanEditMultipleObjects]

    public class SmartObjectSyncEditor : Editor
    {
        public static bool hideHelperComponents = true;
        public static void SetupSmartObjectSync(SmartObjectSync sync)
        {
            if (sync)
            {
                
                SerializedObject serializedSync = new SerializedObject(sync);
                serializedSync.FindProperty("pickup").objectReferenceValue = sync.GetComponent<VRC_Pickup>();
                serializedSync.FindProperty("rigid").objectReferenceValue = sync.GetComponent<Rigidbody>();

                if (!sync.helper)
                {
                    //we need to create the helper
                    sync.helper = UdonSharpComponentExtensions.AddUdonSharpComponent<SmartObjectSyncHelper>(sync.gameObject);
                    SerializedObject serializedHelper = new SerializedObject(sync.helper);
                    serializedHelper.FindProperty("sync").objectReferenceValue = sync;
                    serializedHelper.ApplyModifiedProperties();
                    serializedSync.FindProperty("helper").objectReferenceValue = sync.helper;
                }
                if (VRC_SceneDescriptor.Instance)
                {
                    serializedSync.FindProperty("respawn_height").floatValue = VRC_SceneDescriptor.Instance.RespawnHeightY;
                }
                serializedSync.ApplyModifiedProperties();
                SetupStates(sync);
                if (sync.printDebugMessages)
                    Debug.LogFormat("[SmartObjectSync] {0} Auto Setup complete:\n{1}\n{2}", sync.name, sync.pickup == null ? "No VRC_Pickup component found" : "VRC_Pickup component found", sync.rigid == null ? "No Rigidbody component found" : "Rigidbody component found");
            } else {
                Debug.LogFormat("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
            }
        }
        public static void SetupStates(SmartObjectSync sync)
        {
            if (sync)
            {
                SerializedObject serializedSync = new SerializedObject(sync);
                SmartObjectSyncState[] states = sync.GetComponents<SmartObjectSyncState>();
                serializedSync.FindProperty("states").ClearArray();
                SmartObjectSyncState[] defaultStates = new SmartObjectSyncState[SmartObjectSync.STATE_CUSTOM];
                List<SmartObjectSyncState> deleteList = new List<SmartObjectSyncState>();
                List<SmartObjectSyncState> customStateList = new List<SmartObjectSyncState>();
                foreach (SmartObjectSyncState state in states)
                {
                    if (state)
                    {
                        if (state.GetType().IsAssignableFrom(typeof(SleepState)) && !state.GetType().IsSubclassOf(typeof(SleepState)))
                        {
                            if (defaultStates[SmartObjectSync.STATE_SLEEPING] != null)
                            {
                                deleteList.Add(state);
                            }
                            else
                            {
                                defaultStates[SmartObjectSync.STATE_SLEEPING] = state;
                            }
                        }
                        else if (state.GetType().IsAssignableFrom(typeof(TeleportState)) && !state.GetType().IsSubclassOf(typeof(TeleportState)))
                        {
                            if (defaultStates[SmartObjectSync.STATE_TELEPORTING] != null)
                            {
                                deleteList.Add(state);
                            }
                            else
                            {
                                defaultStates[SmartObjectSync.STATE_TELEPORTING] = state;
                            }
                        }
                        else if (state.GetType().IsAssignableFrom(typeof(LerpState)) && !state.GetType().IsSubclassOf(typeof(LerpState)))
                        {
                            if (defaultStates[SmartObjectSync.STATE_LERPING] != null)
                            {
                                deleteList.Add(state);
                            }
                            else
                            {
                                defaultStates[SmartObjectSync.STATE_LERPING] = state;
                            }
                        }
                        else if (state.GetType().IsAssignableFrom(typeof(FallState)) && !state.GetType().IsSubclassOf(typeof(FallState)))
                        {
                            if (defaultStates[SmartObjectSync.STATE_FALLING] != null)
                            {
                                deleteList.Add(state);
                            }
                            else
                            {
                                defaultStates[SmartObjectSync.STATE_FALLING] = state;
                            }
                        }
                        else if (state.GetType().IsAssignableFrom(typeof(LeftHandHeldState)) && !state.GetType().IsSubclassOf(typeof(LeftHandHeldState)))
                        {
                            if (defaultStates[SmartObjectSync.STATE_LEFT_HAND_HELD] != null)
                            {
                                deleteList.Add(state);
                            }
                            else
                            {
                                defaultStates[SmartObjectSync.STATE_LEFT_HAND_HELD] = state;
                            }
                        }
                        else if (state.GetType().IsAssignableFrom(typeof(RightHandHeldState)) && !state.GetType().IsSubclassOf(typeof(RightHandHeldState)))
                        {
                            if (defaultStates[SmartObjectSync.STATE_RIGHT_HAND_HELD] != null)
                            {
                                deleteList.Add(state);
                            }
                            else
                            {
                                defaultStates[SmartObjectSync.STATE_RIGHT_HAND_HELD] = state;
                            }
                        }
                        else if (state.GetType().IsAssignableFrom(typeof(PlayspaceAttachmentState)) && !state.GetType().IsSubclassOf(typeof(PlayspaceAttachmentState)))
                        {
                            if (defaultStates[SmartObjectSync.STATE_ATTACHED_TO_PLAYSPACE] != null)
                            {
                                deleteList.Add(state);
                            }
                            else
                            {
                                defaultStates[SmartObjectSync.STATE_ATTACHED_TO_PLAYSPACE] = state;
                            }
                        }
                        else if (state.GetType().IsAssignableFrom(typeof(BoneAttachmentState)) && !state.GetType().IsSubclassOf(typeof(BoneAttachmentState)))
                        {
                            if (sync._bone_attached_state != state)
                            {
                                if (sync._bone_attached_state == null)
                                {
                                    sync._bone_attached_state = state;
                                    serializedSync.FindProperty("_bone_attached_state").objectReferenceValue = state;
                                    SerializedObject serializedState = new SerializedObject(state);
                                    serializedState.FindProperty("stateID").intValue = -1;
                                    serializedState.FindProperty("sync").objectReferenceValue = sync;
                                    serializedSync.ApplyModifiedProperties();
                                } else
                                {
                                    deleteList.Add(state);
                                }
                            }
                        }
                        else
                        {
                            customStateList.Add(state);
                        }
                    }
                }
                int deleteStateCounter = 0;
                foreach (SmartObjectSyncState state in deleteList)
                {
                    Component.DestroyImmediate(state);
                    deleteStateCounter++;
                }

                if (defaultStates[SmartObjectSync.STATE_SLEEPING] == null)
                {
                    defaultStates[SmartObjectSync.STATE_SLEEPING] = sync.AddSleepState();
                }
                if (defaultStates[SmartObjectSync.STATE_TELEPORTING] == null)
                {
                    defaultStates[SmartObjectSync.STATE_TELEPORTING] = sync.AddTeleportState();
                }
                if (defaultStates[SmartObjectSync.STATE_LERPING] == null)
                {
                    defaultStates[SmartObjectSync.STATE_LERPING] = sync.AddLerpState();
                }
                if (defaultStates[SmartObjectSync.STATE_FALLING] == null)
                {
                    defaultStates[SmartObjectSync.STATE_FALLING] = sync.AddFallState();
                }
                if (defaultStates[SmartObjectSync.STATE_LEFT_HAND_HELD] == null)
                {
                    defaultStates[SmartObjectSync.STATE_LEFT_HAND_HELD] = sync.AddLeftHandHeldState();
                }
                if (defaultStates[SmartObjectSync.STATE_RIGHT_HAND_HELD] == null)
                {
                    defaultStates[SmartObjectSync.STATE_RIGHT_HAND_HELD] = sync.AddRightHandHeldState();
                }
                if (defaultStates[SmartObjectSync.STATE_ATTACHED_TO_PLAYSPACE] == null)
                {
                    defaultStates[SmartObjectSync.STATE_ATTACHED_TO_PLAYSPACE] = sync.AddPlayspaceAttachmentState();
                }
                if (sync._bone_attached_state == null)
                {
                    BoneAttachmentState newState = sync.AddBoneAttachmentState();
                    sync._bone_attached_state = newState;
                    serializedSync.FindProperty("_bone_attached_state").objectReferenceValue = newState;
                    SerializedObject serializedState = new SerializedObject(newState);
                    serializedState.FindProperty("stateID").intValue = -1;
                    serializedState.FindProperty("sync").objectReferenceValue = sync;
                    serializedSync.ApplyModifiedProperties();
                }

                int stateCounter = 0;
                foreach (SmartObjectSyncState state in defaultStates)
                {
                    AddStateSerialized(ref sync, ref serializedSync, state, stateCounter);
                    stateCounter++;
                }
                foreach (SmartObjectSyncState state in customStateList)
                {
                    AddStateSerialized(ref sync, ref serializedSync, state, stateCounter);
                    stateCounter++;
                }
                serializedSync.ApplyModifiedProperties();
                if (sync.printDebugMessages)
                    Debug.LogFormat("[SmartObjectSync] {0}: {1} States Setup\n{2} States Removed", sync.name, stateCounter + 1, deleteStateCounter);//add one to the state counter to account for bone attachment
            }
            else
            {
                Debug.LogFormat("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
            }
        }

        public static void AddStateSerialized(ref SmartObjectSync sync, ref SerializedObject serializedSync, SmartObjectSyncState newState, int index)
        {
            if (newState != null && serializedSync != null)
            {
                SerializedObject serializedState = new SerializedObject(newState);
                serializedState.FindProperty("stateID").intValue = index;
                serializedState.FindProperty("sync").objectReferenceValue = sync;
                serializedState.ApplyModifiedProperties();
                serializedSync.FindProperty("states").InsertArrayElementAtIndex(index);
                serializedSync.FindProperty("states").GetArrayElementAtIndex(index).objectReferenceValue = newState;
            }
        }

        public static void SetupSelectedSmartObjectSyncs()
        {
            bool syncFound = false;
            foreach (SmartObjectSync sync in Selection.GetFiltered<SmartObjectSync>(SelectionMode.Editable))
            {
                syncFound = true;
                SetupSmartObjectSync(sync);
            }

            if (!syncFound)
            {
                Debug.LogFormat("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
            }
        }
        public override void OnInspectorGUI()
        {
            int syncCount = 0;
            int pickupSetupCount = 0;
            int rigidSetupCount = 0;
            int respawnYSetupCount = 0;
            int helperSetupCount = 0;
            int stateSetupCount = 0;
            foreach (SmartObjectSync sync in Selection.GetFiltered<SmartObjectSync>(SelectionMode.Editable))
            {
                if (sync)
                {
                    syncCount++;
                    if (sync.pickup != sync.GetComponent<VRC_Pickup>())
                    {
                        pickupSetupCount++;
                    }
                    if (sync.rigid != sync.GetComponent<Rigidbody>())
                    {
                        rigidSetupCount++;
                    }
                    if (VRC_SceneDescriptor.Instance != null && VRC_SceneDescriptor.Instance.RespawnHeightY != sync.respawn_height)
                    {
                        respawnYSetupCount++;
                    }
                    if (sync.helper == null)
                    {
                        helperSetupCount++;
                    }
                    if (sync.helper == null)
                    {
                        helperSetupCount++;
                    }

                    if (sync._bone_attached_state == null || sync.states.Length < SmartObjectSync.STATE_CUSTOM || sync.states.Length + 1 != sync.GetComponents<SmartObjectSyncState>().Length)//+ 1 because of bone attachment
                    {
                        stateSetupCount++;
                    } else
                    {
                        foreach (SmartObjectSyncState state in sync.states)
                        {
                            if (state == null)
                            {
                                stateSetupCount++;
                                break;
                            }
                        }
                    }
                }
            }
            if ((pickupSetupCount > 0 || rigidSetupCount > 0 || respawnYSetupCount > 0 || helperSetupCount > 0 || stateSetupCount > 0))
            {
                if (pickupSetupCount == 1)
                {
                    EditorGUILayout.HelpBox(@"Object has a VRC_Pickup component, but no VRC_Pickup was defined in SmartObjectSync", MessageType.Warning);
                }
                else if (pickupSetupCount > 1)
                {
                    EditorGUILayout.HelpBox(pickupSetupCount.ToString() + @" Objects have VRC_Pickup components, but no VRC_Pickup was defined in SmartObjectSync", MessageType.Warning);
                }
                if (rigidSetupCount == 1)
                {
                    EditorGUILayout.HelpBox(@"Object has a RigidBody component, but no Rigidbody was defined in SmartObjectSync", MessageType.Warning);
                }
                else if (rigidSetupCount > 1)
                {
                    EditorGUILayout.HelpBox(rigidSetupCount.ToString() + @" Objects have RigidBody components, but no RigidBody was defined in SmartObjectSync", MessageType.Warning);
                }
                if (respawnYSetupCount == 1)
                {
                    EditorGUILayout.HelpBox(@"Respawn Height is different from the scene's", MessageType.Info);
                }
                else if (respawnYSetupCount > 1)
                {
                    EditorGUILayout.HelpBox(respawnYSetupCount.ToString() + @" Objects have a Respawn Height that is different from the scene's", MessageType.Info);
                }
                if (helperSetupCount == 1)
                {
                    EditorGUILayout.HelpBox(@"Helper not set up", MessageType.Warning);
                }
                else if (helperSetupCount > 1)
                {
                    EditorGUILayout.HelpBox(helperSetupCount.ToString() + @" Helpers not set up", MessageType.Warning);
                }

                if (stateSetupCount == 1)
                {
                    EditorGUILayout.HelpBox(@"States misconfigured", MessageType.Warning);
                }
                else if (stateSetupCount > 1)
                {
                    EditorGUILayout.HelpBox(stateSetupCount.ToString() + @" SmartObjectSyncs with misconfigured States", MessageType.Warning);
                }
                if (GUILayout.Button(new GUIContent("Auto Setup")))
                {
                    SetupSelectedSmartObjectSyncs();
                }
            }
            if (EditorGUILayout.Toggle("Hide Helper Components", hideHelperComponents) != hideHelperComponents)
            {
                hideHelperComponents = !hideHelperComponents;
                if (target)
                {
                    foreach (SmartObjectSyncState state in (target as SmartObjectSync).GetComponents<SmartObjectSyncState>())
                    {
                        if (state)
                        {
                            state.hideFlags = hideHelperComponents ? HideFlags.HideInInspector : HideFlags.None;
                            EditorUtility.SetDirty(state);
                        }
                    }
                }
            }
            EditorGUILayout.Space();
            if (target && UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}

#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public partial class SmartObjectSync : UdonSharpBehaviour
    {
        public bool printDebugMessages = false;
        public bool takeOwnershipOfOtherObjectsOnCollision = true;
        public bool allowOthersToTakeOwnershipOnCollision = true;

        [HideInInspector]
        public SmartObjectSyncHelper helper;
        public float respawn_height = -1001f;
        [HideInInspector]
        public VRC_Pickup pickup;
        [HideInInspector]
        public Rigidbody rigid;
        public float lerpTime = 0.1f;

        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(pos))]
        public Vector3 _pos;
        
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(rot))]
        public Quaternion _rot;
        
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(vel))]
        public Vector3 _vel;
        
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(spin))]
        public Vector3 _spin;
        
        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(state))]
        public int _state = STATE_TELEPORTING;



        public const int STATE_SLEEPING = 0;
        //slowly lerp to final synced transform and velocity and then put rigidbody to sleep
        public const int STATE_TELEPORTING = 1;
        //instantly teleport to last synced transform and velocity
        public const int STATE_LERPING = 2;
        //just use a simple hermite spline to lerp it into place
        public const int STATE_FALLING = 3;
        //similar to STATE_LERPING except we don't take the final velocity into account when lerping
        //instead we assume the object is in projectile motion
        //we use a hermite spline with the ending velocity being the starting velocity + (gravity * lerpTime)
        //once we reach the destination, we set the velocity to the synced velocity
        //hopefully, suddenly changing the velocity makes it look like a bounce.
        public const int STATE_LEFT_HAND_HELD = 4;
        public const int STATE_RIGHT_HAND_HELD = 5;
        public const int STATE_ATTACHED_TO_PLAYSPACE = 6;
        public const int STATE_CUSTOM = 7;

        [HideInInspector]
        public SmartObjectSyncState[] states;

        [HideInInspector]
        public SmartObjectSyncState _bone_attached_state;

        public SmartObjectSyncState activeState{
            get => state < 0 ? _bone_attached_state : states[state];
        }

        [System.NonSerialized]
        public float interpolationStartTime = -1001f;
        public float interpolation
        {
            get => Mathf.Clamp01(interpolationStartTime <= 0 || lerpTime <= 0 || (Time.timeSinceLevelLoad - interpolationStartTime) >= lerpTime ? 1.0f : (Time.timeSinceLevelLoad - interpolationStartTime) / lerpTime);
        }
        [System.NonSerialized]
        public Vector3 posOnSync;
        [System.NonSerialized]
        public Quaternion rotOnSync;
        [System.NonSerialized]
        public Vector3 velOnSync;
        [System.NonSerialized]
        public Vector3 spinOnSync;
        public Vector3 pos
        {
            get => _pos;
            set
            {
                _pos = value;
                
                Synchronize();
            }
        }
        public Quaternion rot
        {
            get => _rot;
            set
            {
                _rot = value;
                Synchronize();
            }
        }
        public Vector3 vel
        {
            get => _vel;
            set
            {
                _vel = value;
                Synchronize();
            }
        }
        public Vector3 spin
        {
            get => _spin;
            set
            {
                _spin = value;
                Synchronize();
            }
        }

        [System.NonSerialized]
        public int lastState = 0;
        public int state
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    activeState.OnExitState();
                }
                
                lastState = _state;
                _state = value;
                
                if (_state != value)
                {
                    activeState.OnEnterState();
                }
                
                if (pickup != null && value != STATE_LEFT_HAND_HELD && value != STATE_RIGHT_HAND_HELD)
                {
                    pickup.Drop();
                }

                if (IsLocalOwner())
                {
                    RequestSerialization();
                }

                switch (value)
                {
                    case (STATE_SLEEPING):
                        {
                            _print("state: STATE_SLEEPING");
                            break;
                        }
                    case (STATE_TELEPORTING):
                        {
                            _print("state: STATE_TELEPORTING");
                            break;
                        }
                    case (STATE_LERPING):
                        {
                            _print("state: STATE_LERPING");
                            break;
                        }
                    case (STATE_FALLING):
                        {
                            _print("state: STATE_FALLING");
                            break;
                        }
                    case (STATE_LEFT_HAND_HELD):
                        {
                            _print("state: STATE_LEFT_HAND_HELD");
                            break;
                        }
                    case (STATE_RIGHT_HAND_HELD):
                        {
                            _print("state: STATE_RIGHT_HAND_HELD");
                            break;
                        }
                    case (STATE_ATTACHED_TO_PLAYSPACE):
                        {
                            _print("state: STATE_ATTACHED_TO_PLAYSPACE");
                            break;
                        }
                    default:
                        {
                            if (value < 0)
                            {
                                _print("state: " + ((HumanBodyBones)(-1 - value)).ToString());
                            } else if (activeState)
                            {
                                _print("state: Custom State " + (value - STATE_CUSTOM).ToString());
                            } else
                            {
                                _printErr("state: INVALID STATE");
                            }
                            break;
                        }
                }
            }
        }

        [System.NonSerialized, FieldChangeCallback(nameof(owner))]
        public VRCPlayerApi _owner;
        public VRCPlayerApi owner{
            get => _owner;
            set
            {
                if (_owner != value)
                {
                    _owner = value;
                    if (!IsLocalOwner())
                    {
                        if (pickup)
                        {
                            pickup.Drop();
                        }

                        if (IsAttachedToPlayer())
                        {
                            state = STATE_LERPING;
                        }
                    }
                }
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            SmartObjectSyncEditor.SetupSmartObjectSync(this);
        }
        public SleepState AddSleepState()
        {
            return UdonSharpComponentExtensions.AddUdonSharpComponent<SleepState>(gameObject);
        }
        public TeleportState AddTeleportState()
        {
            return UdonSharpComponentExtensions.AddUdonSharpComponent<TeleportState>(gameObject);
        }
        public LerpState AddLerpState()
        {
            return UdonSharpComponentExtensions.AddUdonSharpComponent<LerpState>(gameObject);
        }
        public FallState AddFallState()
        {
            return UdonSharpComponentExtensions.AddUdonSharpComponent<FallState>(gameObject);
        }
        public LeftHandHeldState AddLeftHandHeldState()
        {
            return UdonSharpComponentExtensions.AddUdonSharpComponent<LeftHandHeldState>(gameObject);
        }
        public RightHandHeldState AddRightHandHeldState()
        {
            return UdonSharpComponentExtensions.AddUdonSharpComponent<RightHandHeldState>(gameObject);
        }
        public PlayspaceAttachmentState AddPlayspaceAttachmentState()
        {
            return UdonSharpComponentExtensions.AddUdonSharpComponent<PlayspaceAttachmentState>(gameObject);
        }
        public BoneAttachmentState AddBoneAttachmentState()
        {
            return UdonSharpComponentExtensions.AddUdonSharpComponent<BoneAttachmentState>(gameObject);
        }
#endif

        public void _print(string message)
        {
            if (!printDebugMessages)
            {
                return;
            }
            Debug.LogFormat("<color=yellow>[SmartObjectSync] {0} </color>: {1}", name, message);
        }

        public void _printErr(string message)
        {
            if (!printDebugMessages)
            {
                return;
            }
            Debug.LogErrorFormat("<color=yellow>[SmartObjectSync] {0} </color>: {1}", name, message);
        }


        [System.NonSerialized] public bool hasBones;
        [System.NonSerialized] public Vector3 parentPosCache;
        [System.NonSerialized] public Quaternion parentRotCache;

        public void RecordSyncTransforms()
        {
            CalcParentTransform();
            posOnSync = CalcPos();
            rotOnSync = CalcRot();
            velOnSync = LastStateIsAttachedToPlayer() ? Vector3.zero : CalcVel();
            spinOnSync = LastStateIsAttachedToPlayer() ? Vector3.zero : CalcSpin();
        }


        public Vector3 CalcPos()
        {
            return Quaternion.Inverse(parentRotCache) * (transform.position - parentPosCache);
        }
        public Quaternion CalcRot()
        {
            return Quaternion.Inverse(parentRotCache) * transform.rotation;
        }
        public Vector3 CalcVel()
        {
            return rigid == null ? Vector3.zero : rigid.velocity;
        }
        public Vector3 CalcSpin()
        {
            return rigid == null ? Vector3.zero : rigid.angularVelocity;
        }


        public bool LerpDelayOver()
        {
            return interpolationStartTime + lerpTime < Time.timeSinceLevelLoad;
        }


        public override void OnPickup()
        {
            TakeOwnership(false);
            if (pickup)
            {
                if (pickup.currentHand == VRC_Pickup.PickupHand.Left)
                {
                    state = STATE_LEFT_HAND_HELD;
                } else if (pickup.currentHand == VRC_Pickup.PickupHand.Right)
                {
                    state = STATE_RIGHT_HAND_HELD;
                }
            }
        }
        public override void OnDrop()
        {
            if (IsLocalOwner())
            {
                return;
            }
            //if we're attaching it to us, leave it attached
            state = state == STATE_LEFT_HAND_HELD || state == STATE_RIGHT_HAND_HELD ? state : STATE_LERPING;
        }

        public void CalcParentTransform()
        {
            switch (state)
            {
                case (STATE_SLEEPING):
                    {
                        parentPosCache = Vector3.zero;
                        parentRotCache = Quaternion.identity;
                        //slowly lerp to the resting spot
                        break;
                    }
                case (STATE_TELEPORTING):
                    {
                        parentPosCache = Vector3.zero;
                        parentRotCache = Quaternion.identity;
                        //TeleportToSyncedTransform is being run in the smartobjectsync state setter
                        break;
                    }
                case (STATE_LERPING):
                    {
                        parentPosCache = Vector3.zero;
                        parentRotCache = Quaternion.identity;
                        break;
                    }
                case (STATE_LEFT_HAND_HELD):
                    {
                        if (Utilities.IsValid(owner))
                        {
                            parentPosCache = owner.GetBonePosition(HumanBodyBones.LeftHand);
                            parentRotCache = owner.GetBoneRotation(HumanBodyBones.LeftHand);
                            hasBones = parentPosCache != Vector3.zero;
                            parentPosCache = hasBones ? parentPosCache : owner.GetPosition();
                            parentRotCache = hasBones ? parentRotCache : owner.GetRotation();
                        }
                        break;
                    }
                case (STATE_RIGHT_HAND_HELD):
                    {
                        if (Utilities.IsValid(owner))
                        {
                            parentPosCache = owner.GetBonePosition(HumanBodyBones.RightHand);
                            parentRotCache = owner.GetBoneRotation(HumanBodyBones.RightHand);
                            hasBones = parentPosCache != Vector3.zero;
                            parentPosCache = hasBones ? parentPosCache : owner.GetPosition();
                            parentRotCache = hasBones ? parentRotCache : owner.GetRotation();
                        }
                        break;
                    }
                case (STATE_ATTACHED_TO_PLAYSPACE):
                    {
                        if (Utilities.IsValid(owner))
                        {
                            parentPosCache = owner.GetPosition();
                            parentRotCache = owner.GetRotation();
                        }
                        break;
                    }
                default:
                    {
                        if (Utilities.IsValid(owner) && state < STATE_SLEEPING)
                        {
                            parentPosCache = owner.GetBonePosition((HumanBodyBones)(-1 - state));
                            parentRotCache = owner.GetBoneRotation((HumanBodyBones)(-1 - state));
                            hasBones = parentPosCache != Vector3.zero;
                            parentPosCache = hasBones ? parentPosCache : owner.GetPosition();
                            parentRotCache = hasBones ? parentRotCache : owner.GetRotation();
                            if(!hasBones && owner.isLocal){
                                state = STATE_ATTACHED_TO_PLAYSPACE;
                            }
                        } else
                        {
                            parentPosCache = Vector3.zero;
                            parentRotCache = Quaternion.identity;
                        }
                        break;
                    }
            }
        }


        public bool ObjectMoved()
        {
            CalcParentTransform();
            return Vector3.Distance(pos, CalcPos()) > 0.001f || Quaternion.Dot(rot, CalcRot()) < 0.99;
        }
        
        public void MoveToSyncedTransform()
        {
            _print("MoveToSyncedTransform");
            if (state == STATE_SLEEPING)
            {
                //only update transform if it moved so that it'll hopefully fall asleep on its own
                if (ObjectMoved())
                {
                    transform.position = pos;
                    transform.rotation = rot;
                }
                else
                {
                    //The rigid body should eventually go to sleep here
                }
            } else if (IsAttachedToPlayer())
            {
                transform.position = parentPosCache + parentRotCache * pos;
                transform.rotation = parentRotCache * rot;
            } else
            {
                //no parent transform because we are not attached to a player
                transform.position = pos;
                transform.rotation = rot;
                if (rigid)
                {
                    rigid.velocity = vel;
                    rigid.angularVelocity = spin;
                }
            }
        }

        //We are using Hermite Splines which are like Bezier curves but with velocity
        //This video says that all we have to do to convert Hermite to Bezier is divide the tangents by 3 https://www.youtube.com/watch?v=jvPPXbo87ds
        float lerpCache;
        Vector3 posControl1;
        Vector3 posControl2;
        Vector3 startVel;
        Vector3 endVel;
        Quaternion rotControl1;
        Quaternion rotControl2;
        Vector3 startSpin;
        Vector3 endSpin;
        public void LerpToSyncedTransform()
        {
            if (interpolation >= 1)
            {
                MoveToSyncedTransform();
                return;
            }
            
            lerpCache = Mathf.Clamp01(interpolation);
            switch (state)
            {
                case (STATE_SLEEPING):
                case (STATE_TELEPORTING):
                case (STATE_LERPING):
                    {
                        startVel = velOnSync;
                        endVel = vel;
                        startSpin = spinOnSync;
                        endSpin = spin;
                        break;
                    }
                case (STATE_FALLING):
                    {
                        startVel = velOnSync;
                        endVel = (rigid != null && rigid.useGravity) ? velOnSync + Physics.gravity * lerpTime : velOnSync;
                        startSpin = spinOnSync;
                        endSpin = spinOnSync;
                        break;
                    }
                default:
                    {
                        startVel = Vector3.zero;
                        endVel = Vector3.zero;
                        startSpin = Vector3.zero;
                        endSpin = Vector3.zero;
                        break;
                    }
            }

            posControl1 = (parentPosCache + parentRotCache * posOnSync) + startVel * lerpTime * lerpCache / 3f;
            posControl2 = (parentPosCache + parentRotCache * pos) - endVel * lerpTime * (1 - lerpCache) / 3f;

            rotControl1 = (parentRotCache * rotOnSync) * Quaternion.Euler(startSpin * lerpTime * lerpCache / 3f);
            rotControl2 = (parentRotCache * rot) * Quaternion.Euler(-1 * endSpin * lerpTime * (1 - lerpCache) / 3f);

            transform.position = Vector3.Lerp(posControl1, posControl2, lerpCache);
            transform.rotation = Quaternion.Slerp(rotControl1, rotControl2, lerpCache);
        }







        //REFACTOR ------------------------------------------------------------------------------



        //Helpers
        public Vector3 HermiteInterpolatePosition(Vector3 startPos, Vector3 startVel, Vector3 endPos, Vector3 endVel, float interpolation)
        {
            posControl1 = startPos + startVel * lerpTime * interpolation / 3f;
            posControl2 = endPos - endVel * lerpTime * (1 - interpolation) / 3f;
            return Vector3.Lerp(posControl1, posControl2, interpolation);
        }
        public Quaternion HermiteInterpolateRotation(Quaternion startRot, Vector3 startSpin, Quaternion endRot, Vector3 endSpin, float interpolation)
        {
            rotControl1 = startRot * Quaternion.Euler(startSpin * lerpTime * interpolation / 3f);
            rotControl2 = endRot * Quaternion.Euler(-1 * endSpin * lerpTime * (1 - interpolation) / 3f);
            return Quaternion.Slerp(rotControl1, rotControl2, interpolation);
        }
        public bool IsLocalOwner()
        {
            return Utilities.IsValid(owner) && owner.isLocal;
        }
        public bool IsAttachedToPlayer()
        {
            return state < 0 || state == STATE_LEFT_HAND_HELD || state == STATE_RIGHT_HAND_HELD || state == STATE_ATTACHED_TO_PLAYSPACE;
        }
        public bool LastStateIsAttachedToPlayer()
        {
            return lastState < 0 || lastState == STATE_LEFT_HAND_HELD || lastState == STATE_RIGHT_HAND_HELD || lastState == STATE_ATTACHED_TO_PLAYSPACE;
        }
        
        
        
        bool startRan;
        [System.NonSerialized]
        public Vector3 spawnPos;
        [System.NonSerialized]
        public Quaternion spawnRot;
        public void Start()
        {
            SetSpawn();
            startRan = true;
            OnEnable();
        }
        
        public void OnEnable()
        {
            if (!startRan)
            {
                return;
            }
            owner = Networking.GetOwner(gameObject);
            if (IsLocalOwner())
            {
                RequestSerialization();
            }
            //force no interpolation
            interpolationStartTime = -1001f;
            activeState.Interpolate(1.0f);
            activeState.OnInterpolationEnd();
        }

        public void SetSpawn()
        {
            spawnPos = transform.position;
            spawnRot = transform.rotation;
        }
        public void Respawn()
        {
            _print("Respawn");
            if (!IsLocalOwner())
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            TeleportTo(spawnPos, spawnRot, Vector3.zero, Vector3.zero);
        }

        public void TeleportTo(Vector3 newPos, Quaternion newRot, Vector3 newVel, Vector3 newSpin)
        {
            state = STATE_TELEPORTING;
            pos = newPos;
            rot = newRot;
            vel = newVel;
            spin = newSpin;
            Interpolate();
        }

        public void StartInterpolation()
        {
            interpolationStartTime = Time.timeSinceLevelLoad;
            activeState.OnInterpolationStart();
            helper.enabled = true;
        }

        public void Interpolate()
        {
            if (IsLocalOwner() && !activeState.InterpolateOnOwner)
            {
                helper.enabled = false;
                return;
            }
            activeState.Interpolate(interpolation);
            
            if (interpolation < 1.0)
            {
                return;
            }
            activeState.OnInterpolationEnd();
            //decide if we keep running the helper
            helper.enabled = activeState.InterpolateAfterInterpolationPeriod;
        }

        
        //Serialization

        public override void OnPreSerialization()
        {
            _print("OnPreSerialization");
            //we set the last sync time here and in ondeserialization
            interpolationStartTime = Time.timeSinceLevelLoad;
            activeState.OnSmartObjectSerialize();
        }

        public override void OnDeserialization()
        {
            _print("OnDeserialization");
            interpolationStartTime = Time.timeSinceLevelLoad;
            activeState.OnInterpolationStart();
            activeState.Interpolate(0.0f);
        }


        float resyncDelay = 0f;
        float lastSyncFail = -1001f;

        public override void OnPostSerialization(SerializationResult result)
        {
            _print("OnPostSerialization");
            if (!result.success)
            {
                OnSerializationFailure();
            }
            else
            {
                resyncDelay = 0;
            }
        }

        public void OnSerializationFailure()
        {
            //this gets called only when there's a problem with syncing
            //we have to be careful about how we move forward
            _printErr("OnSerializationFailure");
            if (resyncDelay > 0 && lastSyncFail + resyncDelay > Time.timeSinceLevelLoad + 0.1f)
            {
                _printErr("sync failure came too soon after last sync failure");
                return;
            }
            //we double the last delay in an attempt to recreate exponential backoff
            resyncDelay = resyncDelay == 0 ? 0.1f : (resyncDelay * 2);
            lastSyncFail = Time.timeSinceLevelLoad;
            SendCustomEventDelayedSeconds(nameof(Synchronize), resyncDelay);
        }

        public void Synchronize()
        {
            if (!IsLocalOwner())
            {
                return;
            }

            if (Networking.IsClogged)
            {
                OnSerializationFailure();
            } else
            {
                RequestSerialization();
            }
        }

        //Collision Events

        SmartObjectSync otherSync;
        public void OnCollisionEnter(Collision other)
        {
            if (rigid == null || rigid.isKinematic)
            {
                return;
            }

            if (IsLocalOwner())
            {
                //check if we're in a state where physics matters
                if (!IsAttachedToPlayer() && state < STATE_CUSTOM)
                {
                    state = STATE_FALLING;
                }
            } else
            {
                //we may have been knocked out of sync, restart interpolation to get us back in line
                StartInterpolation();
            }


            //decide if we need to take ownership of the object we collided with
            if (!takeOwnershipOfOtherObjectsOnCollision || other == null || other.collider == null)
            {
                return;
            }
            otherSync = other.collider.GetComponent<SmartObjectSync>();
            if (otherSync && otherSync.allowOthersToTakeOwnershipOnCollision && !otherSync.IsAttachedToPlayer() && otherSync.rigid && (IsAttachedToPlayer() || otherSync.state == STATE_SLEEPING || !otherSync.takeOwnershipOfOtherObjectsOnCollision || otherSync.rigid.velocity.sqrMagnitude < rigid.velocity.sqrMagnitude))
            {
                otherSync.TakeOwnership(true);
                otherSync.Synchronize();
            }
        }
        public void OnCollisionExit(Collision other)
        {
            if (rigid == null || rigid.isKinematic)
            {
                return;
            }

            if (IsLocalOwner())
            {
                //check if we're in a state where physics matters
                if (!IsAttachedToPlayer() && state < STATE_CUSTOM)
                {
                    // state = STATE_FALLING;
                    state = STATE_LERPING;
                }
            }
            else
            {
                //we may have been knocked out of sync, restart interpolation to get us back in line
                StartInterpolation();
            }
        }
        
        
        //Pickup Events
        
        

        //Ownership Events
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            _print("OnOwnershipTransferred");
            owner = player;
        }

        public void TakeOwnership(bool checkIfClogged)
        {
            if (checkIfClogged && Networking.IsClogged)
            {
                //Let them cook
                return;
            }
            _print("TakeOwnership");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
    }
}