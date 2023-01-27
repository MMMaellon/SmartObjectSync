
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
        [System.NonSerialized]
        public static bool hideHelperComponentsAndNoErrors = false;

        public static void _print(SmartObjectSync sync, string message)
        {
            if (sync && sync.printDebugMessages)
            {
                Debug.LogFormat("[SmartObjectSync] {0}: {1}", sync.name, message);
            }
        }

        public void OnEnable()
        {
            hideHelperComponentsAndNoErrors = false;
        }

        public static void SetupSmartObjectSync(SmartObjectSync sync)
        {
            if (sync)
            {
                _print(sync, "SetupSmartObjectSync");
                SerializedObject serializedSync = new SerializedObject(sync);
                serializedSync.FindProperty("pickup").objectReferenceValue = sync.GetComponent<VRC_Pickup>();
                serializedSync.FindProperty("rigid").objectReferenceValue = sync.GetComponent<Rigidbody>();

                if (!sync.helper || sync.helper.sync != sync)
                {
                    _print(sync, "adding helper");
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
                    _print(sync, "Auto Setup Complete!\n" + sync.pickup == null ? "No VRC_Pickup component found" : "VRC_Pickup component found\n" + sync.rigid == null ? "No Rigidbody component found" : "Rigidbody component found");
            } else {
                Debug.LogWarning("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
            }
        }
        public static void SetupStates(SmartObjectSync sync)
        {
            if (sync)
            {
                _print(sync, "SetupStates");
                SerializedObject serializedSync = new SerializedObject(sync);
                serializedSync.FindProperty("states").ClearArray();
                foreach (SleepState state in sync.GetComponents<SleepState>())
                {
                    if(!state.GetType().IsSubclassOf(typeof(SleepState)))
                    {
                        Component.DestroyImmediate(state);
                    }
                }
                foreach (TeleportState state in sync.GetComponents<TeleportState>())
                {
                    if (!state.GetType().IsSubclassOf(typeof(TeleportState)))
                    {
                        Component.DestroyImmediate(state);
                    }
                }
                foreach (LerpState state in sync.GetComponents<LerpState>())
                {
                    if(!state.GetType().IsSubclassOf(typeof(LerpState)))
                    {
                        Component.DestroyImmediate(state);
                    }
                }
                foreach (FallState state in sync.GetComponents<FallState>())
                {
                    if(!state.GetType().IsSubclassOf(typeof(FallState)))
                    {
                        Component.DestroyImmediate(state);
                    }
                }
                foreach (LeftHandHeldState state in sync.GetComponents<LeftHandHeldState>())
                {
                    if(!state.GetType().IsSubclassOf(typeof(LeftHandHeldState)))
                    {
                        Component.DestroyImmediate(state);
                    }
                }
                foreach (RightHandHeldState state in sync.GetComponents<RightHandHeldState>())
                {
                    if(!state.GetType().IsSubclassOf(typeof(RightHandHeldState)))
                    {
                        Component.DestroyImmediate(state);
                    }
                }
                foreach (PlayspaceAttachmentState state in sync.GetComponents<PlayspaceAttachmentState>())
                {
                    if (!state.GetType().IsSubclassOf(typeof(PlayspaceAttachmentState)))
                    {
                        Component.DestroyImmediate(state);
                    }
                }
                foreach (WorldLockState state in sync.GetComponents<WorldLockState>())
                {
                    if (!state.GetType().IsSubclassOf(typeof(WorldLockState)))
                    {
                        Component.DestroyImmediate(state);
                    }
                }
                foreach (BoneAttachmentState state in sync.GetComponents<BoneAttachmentState>())
                {
                    if(!state.GetType().IsSubclassOf(typeof(BoneAttachmentState)))
                    {
                        Component.DestroyImmediate(state);
                    }
                }
                SmartObjectSyncState[] states = sync.GetComponents<SmartObjectSyncState>();
                SmartObjectSyncState[] defaultStates = new SmartObjectSyncState[SmartObjectSync.STATE_CUSTOM];

                _print(sync, "adding sleeping state");
                defaultStates[SmartObjectSync.STATE_SLEEPING] = UdonSharpComponentExtensions.AddUdonSharpComponent<SleepState>(sync.gameObject);
                _print(sync, "adding teleporting state");
                defaultStates[SmartObjectSync.STATE_TELEPORTING] = UdonSharpComponentExtensions.AddUdonSharpComponent<TeleportState>(sync.gameObject);
                _print(sync, "adding lerping state");
                defaultStates[SmartObjectSync.STATE_LERPING] = UdonSharpComponentExtensions.AddUdonSharpComponent<LerpState>(sync.gameObject);
                _print(sync, "adding falling state");
                defaultStates[SmartObjectSync.STATE_FALLING] = UdonSharpComponentExtensions.AddUdonSharpComponent<FallState>(sync.gameObject);
                _print(sync, "adding left hand held state");
                defaultStates[SmartObjectSync.STATE_LEFT_HAND_HELD] = UdonSharpComponentExtensions.AddUdonSharpComponent<LeftHandHeldState>(sync.gameObject);
                _print(sync, "adding right hand held state");
                defaultStates[SmartObjectSync.STATE_RIGHT_HAND_HELD] = UdonSharpComponentExtensions.AddUdonSharpComponent<RightHandHeldState>(sync.gameObject);
                _print(sync, "adding attached to playspace state");
                defaultStates[SmartObjectSync.STATE_ATTACHED_TO_PLAYSPACE] = UdonSharpComponentExtensions.AddUdonSharpComponent<PlayspaceAttachmentState>(sync.gameObject);
                _print(sync, "adding world lock state");
                defaultStates[SmartObjectSync.STATE_WORLD_LOCK] = UdonSharpComponentExtensions.AddUdonSharpComponent<WorldLockState>(sync.gameObject);
                _print(sync, "adding bone attached state");
                sync._bone_attached_state = UdonSharpComponentExtensions.AddUdonSharpComponent<BoneAttachmentState>(sync.gameObject);
                serializedSync.FindProperty("_bone_attached_state").objectReferenceValue = sync._bone_attached_state;
                sync._bone_attached_state.stateID = -1;
                sync._bone_attached_state.sync = sync;
                SerializedObject serializedState = new SerializedObject(sync._bone_attached_state);
                serializedState.FindProperty("stateID").intValue = -1;
                serializedState.FindProperty("sync").objectReferenceValue = sync;
                serializedSync.ApplyModifiedProperties();
              

                int stateCounter = 0;
                _print(sync, "adding default states: " + defaultStates.Length);
                foreach (SmartObjectSyncState state in defaultStates)
                {
                    AddStateSerialized(ref sync, ref serializedSync, state, stateCounter);
                    stateCounter++;
                }
                _print(sync, "adding custom states: " + states.Length);
                foreach (SmartObjectSyncState state in states)
                {
                    AddStateSerialized(ref sync, ref serializedSync, state, stateCounter);
                    stateCounter++;
                }
                serializedSync.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarningFormat("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
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
                Debug.LogWarningFormat("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
            }
        }
        public override void OnInspectorGUI()
        {
            // foreach (SmartObjectSyncHelper sync in Selection.GetFiltered<SmartObjectSyncHelper>(SelectionMode.Unfiltered))
            // {
            //     sync.hideFlags = HideFlags.None;
            // }
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
                    EditorGUILayout.HelpBox(@"Object not set up for VRC_Pickup", MessageType.Warning);
                }
                else if (pickupSetupCount > 1)
                {
                    EditorGUILayout.HelpBox(pickupSetupCount.ToString() + @" Objects not set up for VRC_Pickup", MessageType.Warning);
                }
                if (rigidSetupCount == 1)
                {
                    EditorGUILayout.HelpBox(@"Object not set up for Rigidbody", MessageType.Warning);
                }
                else if (rigidSetupCount > 1)
                {
                    EditorGUILayout.HelpBox(rigidSetupCount.ToString() + @" Objects not set up for Rigidbody", MessageType.Warning);
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
            }
            if(hideHelperComponentsAndNoErrors != hideHelperComponents && stateSetupCount == 0){
                hideHelperComponentsAndNoErrors = hideHelperComponents && stateSetupCount == 0;
                if (target)
                {
                    foreach (SmartObjectSyncState state in (target as SmartObjectSync).GetComponents<SmartObjectSyncState>())
                    {
                        if (state)
                        {
                            state.hideFlags = hideHelperComponentsAndNoErrors ? HideFlags.HideInInspector : HideFlags.None;
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

        [System.NonSerialized, UdonSynced(UdonSyncMode.None)]
        public Vector3 pos;
        
        [System.NonSerialized, UdonSynced(UdonSyncMode.None)]
        public Quaternion rot;
        
        [System.NonSerialized, UdonSynced(UdonSyncMode.None)]
        public Vector3 vel;
        
        [System.NonSerialized, UdonSynced(UdonSyncMode.None)]
        public Vector3 spin;
        
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
        public const int STATE_WORLD_LOCK = 7;
        public const int STATE_CUSTOM = 8;

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
            get
            {
                if (interpolationStartTime <= 0 || lerpTime <= 0 || (Time.timeSinceLevelLoad - interpolationStartTime) >= lerpTime)
                {
                    return 1.0f;
                } else
                {
                    return (Time.timeSinceLevelLoad - interpolationStartTime) / lerpTime;
                }
            }
        }
        [System.NonSerialized]
        public Vector3 posOnSync;
        [System.NonSerialized]
        public Quaternion rotOnSync;
        [System.NonSerialized]
        public Vector3 velOnSync;
        [System.NonSerialized]
        public Vector3 spinOnSync;

        [System.NonSerialized]
        public int lastState = 0;
        public int state
        {
            get => _state;
            set
            {
                activeState.OnExitState();
                lastState = _state;
                _state = value;
                activeState.OnEnterState();


                if (pickup && pickup.IsHeld && value != STATE_LEFT_HAND_HELD && value != STATE_RIGHT_HAND_HELD)
                {
                    //no ownership check because there's another pickup.Drop() when ownership is transferred
                    pickup.Drop();
                }
                if (IsLocalOwner())
                {
                    //we start interpolation here to make it snappier for the local owner
                    //make sure to serialize all variables beforehand
                    activeState.OnSmartObjectSerialize();
                    StartInterpolation();
                    RequestSerialization();
                }

                _print("STATE: " + StateToString(value));
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
                    if (IsLocalOwner())
                    {
                        //if the it was attached to the previous owner
                        if (IsAttachedToPlayer() && (pickup == null || !pickup.IsHeld))
                        {
                            state = STATE_LERPING;
                        }
                    } else {
                        if (pickup)
                        {
                            pickup.Drop();
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
#endif

        public void _print(string message)
        {
            if (!printDebugMessages)
            {
                return;
            }
            Debug.LogFormat("<color=yellow>[SmartObjectSync] {0}:</color> {1}", name, message);
        }

        public void _printErr(string message)
        {
            if (!printDebugMessages)
            {
                return;
            }
            Debug.LogErrorFormat("<color=yellow>[SmartObjectSync] {0}:</color> {1}", name, message);
        }
        //Helpers
        Vector3 posControl1;
        Vector3 posControl2;
        Quaternion rotControl1;
        Quaternion rotControl2;
        public Vector3 HermiteInterpolatePosition(Vector3 startPos, Vector3 startVel, Vector3 endPos, Vector3 endVel, float interpolation)
        {
            posControl1 = startPos + startVel * lerpTime * interpolation / 3f;
            posControl2 = endPos - endVel * lerpTime * (1.0f - interpolation) / 3f;
            return Vector3.Lerp(posControl1, posControl2, interpolation);
        }
        public Quaternion HermiteInterpolateRotation(Quaternion startRot, Vector3 startSpin, Quaternion endRot, Vector3 endSpin, float interpolation)
        {
            rotControl1 = startRot * Quaternion.Euler(startSpin * lerpTime * interpolation / 3f);
            rotControl2 = endRot * Quaternion.Euler(-1.0f * endSpin * lerpTime * (1.0f - interpolation) / 3f);
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

        public string StateToString(int value)
        {
            switch (value)
            {
                case (STATE_SLEEPING):
                    {
                        return "STATE_SLEEPING";
                    }
                case (STATE_TELEPORTING):
                    {
                        return "STATE_TELEPORTING";
                    }
                case (STATE_LERPING):
                    {
                        return "STATE_LERPING";
                    }
                case (STATE_FALLING):
                    {
                        return "STATE_FALLING";
                    }
                case (STATE_LEFT_HAND_HELD):
                    {
                        return "STATE_LEFT_HAND_HELD";
                    }
                case (STATE_RIGHT_HAND_HELD):
                    {
                        return "STATE_RIGHT_HAND_HELD";
                    }
                case (STATE_ATTACHED_TO_PLAYSPACE):
                    {
                        return "STATE_ATTACHED_TO_PLAYSPACE";
                    }
                case (STATE_WORLD_LOCK):
                    {
                        return "STATE_WORLD_LOCK";
                    }
                default:
                    {
                        if (value < 0)
                        {
                            return ((HumanBodyBones)(-1 - value)).ToString();
                        }
                        else if (activeState)
                        {
                            return "Custom State " + (value - STATE_CUSTOM).ToString();
                        }

                        return "INVALID STATE";
                    }
            }
        }



        bool startRan;
        [System.NonSerialized]
        public Vector3 spawnPos;
        [System.NonSerialized]
        public Quaternion spawnRot;
        public void Start()
        {
            SetSpawn();
            if (!IsAttachedToPlayer() && state < STATE_CUSTOM)
            {
                //set starting synced values
                pos = transform.position;
                rot = transform.rotation;
                if (rigid && !rigid.isKinematic)
                {
                    vel = rigid.velocity;
                    spin = rigid.angularVelocity;
                }
            }
            startRan = true;
        }
        
        public void OnEnable()
        {
            owner = Networking.GetOwner(gameObject);
            if (IsLocalOwner())
            {
                if (startRan)
                {
                    //force a reset
                    state = state;
                }
            } else if (interpolationStartTime > 0)
            {
                //only do this after we've received some data from the owner to prevent being sucked into spawn
                _print("onenable start interpolate");
                StartInterpolation();
                //force no interpolation
                interpolationStartTime = -1001f;
                Interpolate();
            }
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
            pos = newPos;
            rot = newRot;
            vel = newVel;
            spin = newSpin;
            //remember to set state last as it triggers interpolation
            state = STATE_TELEPORTING;
            //this doesn't actually do anything except turn off the helper for the teleport state
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
            activeState.Interpolate(interpolation);
            
            if (interpolation < 1.0)
            {
                return;
            }
            //decide if we keep running the helper
            helper.enabled = activeState.OnInterpolationEnd();
        }

        
        //Serialization
        public override void OnPreSerialization()
        {
            // _print("OnPreSerialization");
            activeState.OnSmartObjectSerialize();
            StartInterpolation();
        }

        public override void OnDeserialization()
        {
            // _print("OnDeserialization");
            StartInterpolation();
        }


        float resyncDelay = 0f;
        float lastSyncFail = -1001f;

        public override void OnPostSerialization(SerializationResult result)
        {
            // _print("OnPostSerialization");
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
            SendCustomEventDelayedSeconds(nameof(LowPrioritySerialize), resyncDelay);
        }

        public void LowPrioritySerialize()
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
                    // state = STATE_FALLING;
                    state = STATE_LERPING;
                }
            } else if (state == STATE_SLEEPING)
            {
                //we may have been knocked out of sync, restart interpolation to get us back in line
                _print("local collision woke up rigidbody");
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
                otherSync.LowPrioritySerialize();
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
                    state = STATE_FALLING;
                    // state = STATE_LERPING;
                }
            }
            else if (state == STATE_SLEEPING)
            {
                //we may have been knocked out of sync, restart interpolation to get us back in line
                _print("local collision woke up rigidbody");
                StartInterpolation();
            }
        }


        //Pickup Events

        public override void OnPickup()
        {
            TakeOwnership(false);
            if (pickup)
            {
                if (pickup.currentHand == VRC_Pickup.PickupHand.Left)
                {
                    state = STATE_LEFT_HAND_HELD;
                }
                else if (pickup.currentHand == VRC_Pickup.PickupHand.Right)
                {
                    state = STATE_RIGHT_HAND_HELD;
                }
            }
        }
        public override void OnDrop()
        {
            _print("OnDrop");
            //it takes 1 frame for VRChat to give the pickup the correct velocity, so let's wait 1 frame
            SendCustomEventDelayedFrames(nameof(OnDropDelayed), 1);
        }

        public void OnDropDelayed(){
            if (!IsLocalOwner())
            {
                return;
            }
            _print("we are local owner");
            //if we're attaching it to us, leave it attached

            state = state == STATE_LEFT_HAND_HELD || state == STATE_RIGHT_HAND_HELD ? STATE_LERPING : state;
            _print("set state to " + state);
        }


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