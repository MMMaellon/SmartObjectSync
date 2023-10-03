
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
    [CustomEditor(typeof(SmartObjectSync), true), CanEditMultipleObjects]

    public class SmartObjectSyncEditor : Editor
    {
        public static bool foldoutOpen = false;

        //Advanced settings
        SerializedProperty m_printDebugMessages;
        SerializedProperty m_performanceModeCollisions;
        SerializedProperty m_forceContinuousSpeculative;
        SerializedProperty m_kinematicWhileHeld;
        SerializedProperty m_nonKinematicPickupJitterPreventionTime;
        SerializedProperty m_reduceJitterDuringSleep;
        SerializedProperty m_worldSpaceTeleport;
        SerializedProperty m_worldSpaceSleep;
        SerializedProperty m_preventStealWhileAttachedToPlayer;
        SerializedProperty m_startingState;
        void OnEnable()
        {
            m_printDebugMessages = serializedObject.FindProperty("printDebugMessages");
            m_performanceModeCollisions = serializedObject.FindProperty("performanceModeCollisions");
            m_forceContinuousSpeculative = serializedObject.FindProperty("forceContinuousSpeculative");
            m_kinematicWhileHeld = serializedObject.FindProperty("kinematicWhileHeld");
            m_nonKinematicPickupJitterPreventionTime = serializedObject.FindProperty("nonKinematicPickupJitterPreventionTime");
            m_reduceJitterDuringSleep = serializedObject.FindProperty("reduceJitterDuringSleep");
            m_worldSpaceTeleport = serializedObject.FindProperty("worldSpaceTeleport");
            m_worldSpaceSleep = serializedObject.FindProperty("worldSpaceSleep");
            m_preventStealWhileAttachedToPlayer = serializedObject.FindProperty("preventStealWhileAttachedToPlayer");
            m_startingState = serializedObject.FindProperty("startingState");
        }

        public static void _print(SmartObjectSync sync, string message)
        {
            if (sync && sync.printDebugMessages)
            {
                Debug.LogFormat(sync, "[SmartObjectSync] {0}: {1}", sync.name, message);
            }
        }

        public static void SetupSmartObjectSync(SmartObjectSync sync)
        {
            if (sync)
            {
                _print(sync, "SetupSmartObjectSync");
                SerializedObject serializedSync = new SerializedObject(sync);
                serializedSync.FindProperty("pickup").objectReferenceValue = sync.GetComponent<VRC_Pickup>();
                serializedSync.FindProperty("rigid").objectReferenceValue = sync.GetComponent<Rigidbody>();
                if (VRC_SceneDescriptor.Instance)
                {
                    serializedSync.FindProperty("respawnHeight").floatValue = VRC_SceneDescriptor.Instance.RespawnHeightY;
                }
                serializedSync.ApplyModifiedProperties();
                SetupStates(sync);
                if (sync.printDebugMessages)
                    _print(sync, "Auto Setup Complete!\n" + (sync.pickup == null ? "No VRC_Pickup component found" : "VRC_Pickup component found\n" + (sync.rigid == null ? "No Rigidbody component found" : "Rigidbody component found")));
            }
            else
            {
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
                SmartObjectSyncState[] states = sync.GetComponents<SmartObjectSyncState>();
                int stateCounter = 0;
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
            int syncCount = 0;
            int pickupSetupCount = 0;
            int rigidSetupCount = 0;
            int respawnYSetupCount = 0;
            int stateSetupCount = 0;
            foreach (SmartObjectSync sync in Selection.GetFiltered<SmartObjectSync>(SelectionMode.Editable))
            {
                if (Utilities.IsValid(sync))
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
                    if (Utilities.IsValid(VRC_SceneDescriptor.Instance) && !Mathf.Approximately(VRC_SceneDescriptor.Instance.RespawnHeightY, sync.respawnHeight))
                    {
                        respawnYSetupCount++;
                    }
                    if (!Utilities.IsValid(sync.states))
                    {
                        sync.states = new SmartObjectSyncState[0];
                    }

                    SmartObjectSyncState[] stateComponents = sync.GetComponents<SmartObjectSyncState>();
                    if (sync.states.Length != stateComponents.Length)//+ 1 because of bone attachment
                    {
                        stateSetupCount++;
                    }
                    else
                    {
                        bool errorFound = false;
                        foreach (SmartObjectSyncState state in sync.states)
                        {
                            if (state == null || state.sync != sync || state.stateID < 0 || state.stateID >= sync.states.Length || sync.states[state.stateID] != state)
                            {
                                errorFound = true;
                                break;
                            }
                        }
                        if (!errorFound)
                        {
                            foreach (SmartObjectSyncState state in stateComponents)
                            {
                                if (state != null && (state.sync != sync || state.stateID < 0 || state.stateID >= sync.states.Length || sync.states[state.stateID] != state))
                                {
                                    errorFound = true;
                                    break;
                                }
                            }
                        }
                        if (errorFound)
                        {
                            stateSetupCount++;
                        }
                    }
                }
            }
            if ((pickupSetupCount > 0 || rigidSetupCount > 0 || respawnYSetupCount > 0 || stateSetupCount > 0))
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
            if (target && UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            foldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutOpen, "Advanced Settings");
            if (foldoutOpen)
            {
                if (GUILayout.Button(new GUIContent("Force Setup")))
                {
                    SetupSelectedSmartObjectSyncs();
                }
                EditorGUILayout.PropertyField(m_printDebugMessages);
                EditorGUILayout.PropertyField(m_performanceModeCollisions);
                EditorGUILayout.PropertyField(m_forceContinuousSpeculative);
                EditorGUILayout.PropertyField(m_kinematicWhileHeld);
                EditorGUILayout.PropertyField(m_nonKinematicPickupJitterPreventionTime);
                EditorGUILayout.PropertyField(m_reduceJitterDuringSleep);
                EditorGUILayout.PropertyField(m_worldSpaceTeleport);
                EditorGUILayout.PropertyField(m_worldSpaceSleep);
                EditorGUILayout.PropertyField(m_preventStealWhileAttachedToPlayer);
                EditorGUILayout.PropertyField(m_startingState);
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}

#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [RequireComponent(typeof(Rigidbody))]
    public partial class SmartObjectSync : UdonSharpBehaviour
    {
        public bool takeOwnershipOfOtherObjectsOnCollision = true;
        public bool allowOthersToTakeOwnershipOnCollision = true;
        public bool allowTheftFromSelf = true;
        public bool syncParticleCollisions = false;
        public float respawnHeight = -1001f;

        [HideInInspector]
        public bool printDebugMessages = false;
        [HideInInspector]
        public bool performanceModeCollisions = true;
        [HideInInspector, Tooltip("By default, calling respawn will make the object reset it's local object space transforms.")]
        public bool worldSpaceTeleport = false;
        [HideInInspector, Tooltip("By default, everything is in world space. Uncheck if the relationship between this object and it's parent is more important than it and the world.")]
        public bool worldSpaceSleep = true;
        [HideInInspector, Tooltip("VRCObjectSync will set rigidbodies' collision detection mode to ContinuousSpeculative. This recreates that feature.")]
        public bool forceContinuousSpeculative = true;
        [HideInInspector, Tooltip("Turns the object kinematic when held. Will result in dampened collisions because that's just how Unity is.")]
        public bool kinematicWhileHeld = true;

        [HideInInspector, Tooltip("Forces the local pickup object into the right position after a certain amount of time. Disabled if set to 0 or negative. You should only enable this if you need the collisions to be accurate, otherwise reduce jitter by enabling kinematic while held instead.")]
        public float nonKinematicPickupJitterPreventionTime = 0;

        [HideInInspector, Tooltip("If the rigidbody is unable to fall asleep we hold it in place. Makes object sync more accurate, at the cost of more CPU usage for non-owners in some edge cases where your physics are unstable.")]
        public bool reduceJitterDuringSleep = true;
        [HideInInspector, Tooltip("Only applies when we're attached to a player's playspace or avatar. Prevents other players from taking it.")]
        public bool preventStealWhileAttachedToPlayer = true;
        [HideInInspector]
        public bool respawnIntoStartingState = true;
        [HideInInspector]
        public SmartObjectSyncState startingState = null;
        [HideInInspector]
        public VRC_Pickup pickup;
        [HideInInspector]
        public Rigidbody rigid;

        [System.NonSerialized, UdonSynced(UdonSyncMode.None)]
        public Vector3 pos;

        [System.NonSerialized, UdonSynced(UdonSyncMode.None)]
        public Quaternion rot;

        [System.NonSerialized, UdonSynced(UdonSyncMode.None)]
        public Vector3 vel;

        [System.NonSerialized, UdonSynced(UdonSyncMode.None)]
        public Vector3 spin;

        [System.NonSerialized, FieldChangeCallback(nameof(state))]
        public int _state = STATE_TELEPORTING;

        [System.NonSerialized, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(stateData))]
        public int _stateData;
        [System.NonSerialized]
        public int _stateChangeCounter = 0;
        public const int STATE_SLEEPING = 0;
        //slowly lerp to final synced transform and velocity and then put rigidbody to sleep
        public const int STATE_TELEPORTING = 1;
        //instantly teleport to last synced transform and velocity
        public const int STATE_INTERPOLATING = 2;
        //just use a simple hermite spline to lerp it into place
        public const int STATE_FALLING = 3;
        //similar to STATE_INTERPOLATING except we don't take the final velocity into account when interpolating
        //instead we assume the object is in projectile motion
        //we use a hermite spline with the ending velocity being the starting velocity + (gravity * lerpTime)
        //once we reach the destination, we set the velocity to the synced velocity
        //hopefully, suddenly changing the velocity makes it look like a bounce.
        public const int STATE_LEFT_HAND_HELD = 4;
        public const int STATE_RIGHT_HAND_HELD = 5;
        public const int STATE_NO_HAND_HELD = 6;//when an avatar has no hands and the position needs to follow their head instead
        public const int STATE_ATTACHED_TO_PLAYSPACE = 7;
        public const int STATE_WORLD_LOCK = 8;
        public const int STATE_CUSTOM = 9;

        [HideInInspector]
        public SmartObjectSyncState[] states;

        public SmartObjectSyncState customState
        {
            get
            {
                if (state >= STATE_CUSTOM && state - STATE_CUSTOM < states.Length)
                {
                    return states[state - STATE_CUSTOM];
                }
                return null;
            }
        }

        [System.NonSerialized]
        public float interpolationStartTime = -1001f;
        [System.NonSerialized]
        public float interpolationEndTime = -1001f;
        [System.NonSerialized]
        public float fallSpeed = 0f;
#if !UNITY_EDITOR
        public float lagTime
        {
            get => Time.realtimeSinceStartup - Networking.SimulationTime(gameObject);
        }
#else
        public float lagTime
        {
            get => 0.25f;
        }
#endif
        public float interpolation
        {
            get
            {
                return lagTime <= 0 ? 1 : Mathf.Lerp(0, 1, (Time.timeSinceLevelLoad - interpolationStartTime) / lagTime);
            }
        }
        public SmartObjectSyncListener[] listeners = new SmartObjectSyncListener[0];

        [System.NonSerialized]
        public Vector3 posOnSync;
        [System.NonSerialized]
        public Quaternion rotOnSync;
        [System.NonSerialized]
        public Vector3 velOnSync;
        [System.NonSerialized]
        public Vector3 spinOnSync;
        [System.NonSerialized]
        public int lastState;
        [System.NonSerialized]
        public int stateChangeCounter = 0;//counts how many times we change state
        bool teleportFlagSet = false;
        public int state
        {
            get => _state;
            set
            {
                OnExitState();
                lastState = _state;
                _state = value;
                OnEnterState();

                if (pickup && pickup.IsHeld && !IsHeld())
                {
                    //no ownership check because there's another pickup.Drop() when ownership is transferred
                    pickup.Drop();
                }
                if (IsLocalOwner())
                {
                    stateChangeCounter++;
                    //we call OnPreSerialization here to make it snappier for the local owner
                    //if any transforms and stuff happen in OnInterpolationStart, they happen here
                    // OnPreSerialization();
                    //we don't actually call onpreserialization so that serializationnotification works properly
                    OnSmartObjectSerialize();
                    StartInterpolation();
                    //we put in the request for actual serialization
                    Serialize();
                }

                _print("STATE: " + StateToString(value));

                if (Utilities.IsValid(listeners))
                {
                    foreach (SmartObjectSyncListener listener in listeners)
                    {
                        if (Utilities.IsValid(listener))
                        {
                            listener.OnChangeState(this, lastState, value);
                        }
                    }
                }
            }
        }

        public int stateData
        {
            get
            {
                if (IsLocalOwner())
                {
                    return _state >= 0 ? (_state * 10) + (stateChangeCounter % 10) : (_state * 10) - (stateChangeCounter % 10);
                }
                return _stateData;
            }
            set
            {
                if (_stateData == value)
                {
                    return;
                }
                _stateData = value;
                if (!IsLocalOwner())
                {
                    state = value / 10;
                    stateChangeCounter = (Mathf.Abs(value) % 10);
                }
                _print("stateData received " + value);
            }
        }

        [System.NonSerialized, FieldChangeCallback(nameof(owner))]
        public VRCPlayerApi _owner;
        public VRCPlayerApi owner
        {
            get => _owner;
            set
            {
                if (_owner != value)
                {
                    foreach (SmartObjectSyncListener listener in listeners)
                    {
                        if (Utilities.IsValid(listener))
                        {
                            listener.OnChangeOwner(this, _owner, value);
                        }
                    }
                    _owner = value;
                    if (IsLocalOwner())
                    {
                        //if the it was attached to the previous owner
                        if (IsAttachedToPlayer() && (pickup == null || !pickup.IsHeld))
                        {
                            state = STATE_INTERPOLATING;
                        }
                    }
                    else
                    {
                        serializeRequested = false;
                        if (pickup)
                        {
                            pickup.Drop();
                        }
                    }
                }
            }
        }

        [HideInInspector]
        public bool SetupRan = false;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            if (!SetupRan)
            {
                SmartObjectSyncEditor.SetupSmartObjectSync(this);
            }
            SetupRan = true;
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
            posControl1 = startPos + startVel * lagTime * interpolation / 3f;
            posControl2 = endPos - endVel * lagTime * (1.0f - interpolation) / 3f;
            return Vector3.Lerp(posControl1, posControl2, interpolation);
        }
        public Quaternion HermiteInterpolateRotation(Quaternion startRot, Vector3 startSpin, Quaternion endRot, Vector3 endSpin, float interpolation)
        {
            rotControl1 = startRot * Quaternion.Euler(startSpin * lagTime * interpolation / 3f);
            rotControl2 = endRot * Quaternion.Euler(-1.0f * endSpin * lagTime * (1.0f - interpolation) / 3f);
            return Quaternion.Slerp(rotControl1, rotControl2, interpolation);
        }
        public bool IsLocalOwner()
        {
            return Utilities.IsValid(owner) && owner.isLocal;
        }
        public bool IsOwnerLocal()
        {
            return Utilities.IsValid(owner) && owner.isLocal;
        }
        public bool IsHeld()
        {
            return state == STATE_LEFT_HAND_HELD || state == STATE_RIGHT_HAND_HELD || state == STATE_NO_HAND_HELD;
        }
        public bool IsAttachedToPlayer()
        {
            return state < 0 || state == STATE_ATTACHED_TO_PLAYSPACE || IsHeld();
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
                case (STATE_INTERPOLATING):
                    {
                        return "STATE_INTERPOLATING";
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
                case (STATE_NO_HAND_HELD):
                    {
                        return "STATE_HEAD_HELD";
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
                        else if (value >= STATE_CUSTOM && (value - STATE_CUSTOM) < states.Length)
                        {
                            return "Custom State " + (value - STATE_CUSTOM).ToString() + " " + (value - STATE_CUSTOM >= 0 && value - STATE_CUSTOM < states.Length && states[value - STATE_CUSTOM] != null ? states[value - STATE_CUSTOM].GetType().ToString() : "NULL");
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
        int randomSeed = 0;
        int randomCount = 0;
        public void Start()
        {
            _pickupable = Utilities.IsValid(pickup) && pickup.pickupable;
            randomSeed = Random.Range(0, 10);
            if (forceContinuousSpeculative)
            {
                rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
            SetSpawn();
            if (!IsAttachedToPlayer() && state < STATE_CUSTOM)
            {
                //set starting synced values
                pos = transform.position;
                rot = transform.rotation;
                if (!rigid.isKinematic)
                {
                    vel = rigid.velocity;
                    spin = rigid.angularVelocity;
                }
            }

            //speed we gain from gravity in a free fall over the lerp time
            //used to decide if we simulate a bounce in the falling state
            fallSpeed = lagTime <= 0 ? 0 : Physics.gravity.magnitude * lagTime;
            startRan = true;
            if (Utilities.IsValid(startingState)){
                if (startingState.sync == this)
                {
                    if (IsLocalOwner())
                    {
                        startingState.EnterState();
                    }
                }
                else
                {
                    startingState = null;
                }
            }
        }
        [System.NonSerialized]
        public bool _loop = false;
        bool loopRequested = false;
        public bool loop
        {
            get => _loop;
            set
            {
                if (value)
                {
                    disableRequested = false;
                    _loop = true;
                    if (!loopRequested)
                    {
                        loopRequested = true;
                        SendCustomEventDelayedFrames(nameof(UpdateLoop), 0);
                    }
                }
                else if (serializeRequested)
                {
                    disableRequested = true;
                }
                else
                {
                    _loop = false;
                }
            }
        }
        [System.NonSerialized]
        public bool disableRequested = false;
        [System.NonSerialized]
        public bool serializeRequested = false;
        public void UpdateLoop()
        {
            loopRequested = false;
            if (!_loop)
            {
                return;
            }
            if (!Utilities.IsValid(owner))
            {
                //we use underscore here
                _loop = false;
                return;
            }

            if (interpolationStartTime < 0)
            {
                //if we haven't received data yet, do nothing. Otherwise this will move the object to the origin
                //we use underscore here
                _loop = false;
                return;
            }
            loopRequested = true;
            SendCustomEventDelayedFrames(nameof(UpdateLoop), 0);

            if (!disableRequested)
            {
                Interpolate();
            }
            if (serializeRequested && !IsNetworkAtRisk())
            {
                if (state <= STATE_SLEEPING || state > STATE_FALLING)
                {
                    //prioritize non-collision serializations because they will settle our world faster
                    RequestSerialization();
                }
                else
                {
                    //randomize it, so we stagger the synchronizations
                    randomCount = (randomCount + 1) % 10;
                    if (randomCount == randomSeed)
                    {
                        RequestSerialization();
                    }
                }
            }
        }

        public void OnSerializationFailure()
        {
            _printErr("OnSerializationFailure");
            serializeRequested = true;
            loop = true;//we're just going to wait around for the network to allow us to synchronize again
        }
        public void OnSerializationSuccess()
        {
            serializeRequested = false;
        }

        public void OnEnable()
        {
            _print("OnEnable");
            owner = Networking.GetOwner(gameObject);
            if (IsLocalOwner())
            {
                if (startRan)
                {
                    //force a reset
                    state = state;
                }
            }
            else
            {
                if (interpolationStartTime > 0)
                {
                    //only do this after we've received some data from the owner to prevent being sucked into spawn
                    _print("onenable start interpolate");
                    StartInterpolation();
                    //force no interpolation
                    interpolationStartTime = -1001f;
                    Interpolate();
                }
                else
                {
                    RequestResync();
                }
            }
        }
        float lastResyncRequest = -1001f;
        public void RequestResync()
        {
            lastResyncRequest = Time.timeSinceLevelLoad;
            //stagger requests randomly to prevent network clogging
            SendCustomEventDelayedSeconds(nameof(RequestResyncCallback), (2 * lagTime) + Random.Range(0.0f, 1.0f));
        }
        public void RequestResyncCallback()
        {
            if (IsLocalOwner() || lastSuccessfulNetworkSync > lastResyncRequest || lastResyncRequest < 0)
            {
                return;
            }
            if (IsNetworkAtRisk())
            {
                //just wait it out;
                RequestResync();
                return;
            }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Serialize));
        }

        public void SetSpawn()
        {
            if (worldSpaceTeleport)
            {
                spawnPos = transform.position;
                spawnRot = transform.rotation;
            }
            else
            {
                spawnPos = transform.localPosition;
                spawnRot = transform.localRotation;
            }
        }
        public void Respawn()
        {
            _print("Respawn");
            TakeOwnership(false);

            if (Utilities.IsValid(startingState))
            {
                startingState.EnterState();
            }
            else
            {
                TeleportTo(spawnPos, spawnRot, Vector3.zero, Vector3.zero);
            }
        }

        public void TeleportTo(Vector3 newPos, Quaternion newRot, Vector3 newVel, Vector3 newSpin)
        {
            if (!IsLocalOwner())
            {
                TakeOwnership(false);
            }
            pos = newPos;
            rot = newRot;
            vel = newVel;
            spin = newSpin;
            state = STATE_TELEPORTING;
        }
        public void TeleportToWorldSpace(Vector3 newPos, Quaternion newRot, Vector3 newVel, Vector3 newSpin)
        {
            if (!IsLocalOwner())
            {
                TakeOwnership(false);
            }
            if (worldSpaceTeleport)
            {
                pos = newPos;
                rot = newRot;
                vel = newVel;
                spin = newSpin;
            }
            else
            {
                pos = Quaternion.Inverse(parentRot) * (newPos - parentPos);
                rot = Quaternion.Inverse(parentRot) * newRot;
                vel = transform.InverseTransformVector(newVel);
                spin = transform.InverseTransformVector(newSpin);
            }
            state = STATE_TELEPORTING;
        }

        public void TeleportToLocalSpace(Vector3 newPos, Quaternion newRot, Vector3 newVel, Vector3 newSpin)
        {
            if (!IsLocalOwner())
            {
                TakeOwnership(false);
            }
            if (worldSpaceTeleport)
            {
                pos = parentPos + parentRot * newPos;
                rot = parentRot * newRot;
                vel = transform.TransformPoint(newVel);
                spin = transform.TransformPoint(newSpin);
            }
            else
            {
                pos = newPos;
                rot = newRot;
                vel = newVel;
                spin = newSpin;
            }
            state = STATE_TELEPORTING;
        }

        public void StartInterpolation()
        {
            loop = true;
            OnInterpolationStart();
        }

        public void Interpolate()
        {
            OnInterpolate(interpolation);

            if (interpolation < 1.0)
            {
                return;
            }
            //decide if we keep running the helper
            if (!OnInterpolationEnd())
            {
                loop = false;
            }
        }

        bool transformRecorded = false;
        public void RecordLastTransform()
        {
            lastPos = transform.position;
            lastRot = transform.rotation;
            transformRecorded = true;
        }

        public void SetVelocityFromLastTransform()
        {
            if (transformRecorded)
            {
                rigid.velocity = CalcVel();
                rigid.angularVelocity = CalcSpin();
            }
        }


        //Serialization
        [System.NonSerialized]
        public float lastSerializeRequest = -1001f;
        public bool IsNetworkAtRisk()
        {
            return Networking.IsClogged;
        }
        public void Serialize()
        {
            if (IsNetworkAtRisk())
            {
                OnSerializationFailure();
            }
            else
            {
                RequestSerialization();
            }
            lastSerializeRequest = Time.timeSinceLevelLoad;
        }

        public override void OnPreSerialization()
        {
            // _print("OnPreSerialization");
            OnSmartObjectSerialize();
            StartInterpolation();
            stateData = stateData;
        }

        float lastSuccessfulNetworkSync = -1001f;
        public override void OnDeserialization()
        {
            // _print("OnDeserialization");
            StartInterpolation();
            lastSuccessfulNetworkSync = Time.timeSinceLevelLoad;
        }

        public override void OnPostSerialization(VRC.Udon.Common.SerializationResult result)
        {
            if (result.success)
            {
                OnSerializationSuccess();
                lastSuccessfulNetworkSync = Time.timeSinceLevelLoad;
            }
            else
            {
                //it sets a flag in the helper which will try to synchronize everything once the network gets less congested
                OnSerializationFailure();
            }
        }


        public void AttachToBone(HumanBodyBones bone)
        {
            if (!IsLocalOwner())
            {
                TakeOwnership(false);
            }
            state = (-1 - (int)bone);
        }

        //Collision Events
        SmartObjectSync otherSync;
        float lastCollision = -1001;
        public void OnCollisionEnter(Collision other)
        {
            if (rigid.isKinematic || (performanceModeCollisions && lastCollision + Time.deltaTime > Time.timeSinceLevelLoad))
            {
                return;
            }

            lastCollision = Time.timeSinceLevelLoad;

            if (IsLocalOwner())
            {
                //check if we're in a state where physics matters
                if (!IsAttachedToPlayer() && state < STATE_CUSTOM && TeleportSynced())
                {
                    state = STATE_INTERPOLATING;
                }
                //decide if we need to take ownership of the object we collided with
                if (takeOwnershipOfOtherObjectsOnCollision && Utilities.IsValid(other) && Utilities.IsValid(other.collider))
                {
                    otherSync = other.collider.GetComponent<SmartObjectSync>();
                    if (otherSync && !otherSync.IsLocalOwner() && otherSync.allowOthersToTakeOwnershipOnCollision && !otherSync.IsAttachedToPlayer() && (IsAttachedToPlayer() || otherSync.state == STATE_SLEEPING || !otherSync.takeOwnershipOfOtherObjectsOnCollision || otherSync.rigid.velocity.sqrMagnitude < rigid.velocity.sqrMagnitude))
                    {
                        otherSync.TakeOwnership(true);
                        otherSync.Serialize();
                    }
                }
            }
            else if (state == STATE_SLEEPING && interpolationEndTime + Time.deltaTime < Time.timeSinceLevelLoad)//we ignore the very first frame after interpolation to give rigidbodies time to settle down
            {
                //we may have been knocked out of sync, restart interpolation to get us back in line
                StartInterpolation();
            }
        }

        public bool TeleportSynced()
        {
            return (state != STATE_TELEPORTING || !teleportFlagSet);
        }

        float lastCollisionExit = -1001;
        public void OnCollisionExit(Collision other)
        {
            if (rigid.isKinematic || (performanceModeCollisions && lastCollisionExit + Time.deltaTime > Time.timeSinceLevelLoad))
            {
                return;
            }

            lastCollisionExit = Time.timeSinceLevelLoad;

            if (IsLocalOwner())
            {
                //check if we're in a state where physics matters
                if (!IsAttachedToPlayer() && state < STATE_CUSTOM && TeleportSynced())
                {
                    state = STATE_FALLING;
                }
            }
            else if (state == STATE_SLEEPING && interpolationEndTime + Time.deltaTime < Time.timeSinceLevelLoad)
            {
                //we may have been knocked out of sync, restart interpolation to get us back in line
                StartInterpolation();
            }
        }

        public void OnParticleCollision(GameObject other)
        {
            if (!syncParticleCollisions || !Utilities.IsValid(other) || rigid.isKinematic)
            {
                return;
            }
            if (IsLocalOwner())
            {
                //check if we're in a state where physics matters
                if (!IsAttachedToPlayer() && state < STATE_CUSTOM && TeleportSynced())
                {
                    state = STATE_INTERPOLATING;
                }
            }
            else
            {
                //decide if we need to take ownership of the object we collided with
                if (allowOthersToTakeOwnershipOnCollision && Utilities.IsValid(other.GetComponent<ParticleSystem>()) && !IsAttachedToPlayer())
                {
                    otherSync = other.GetComponent<SmartObjectSync>();
                    if (!Utilities.IsValid(otherSync) && Utilities.IsValid(other.transform.parent))
                    {
                        otherSync = other.GetComponentInParent<SmartObjectSync>();
                    }
                    if (Utilities.IsValid(otherSync) && otherSync.IsLocalOwner() && otherSync.takeOwnershipOfOtherObjectsOnCollision)
                    {
                        TakeOwnership(true);
                        if (!IsAttachedToPlayer() && state < STATE_CUSTOM && TeleportSynced())
                        {
                            state = STATE_INTERPOLATING;
                        }
                        Serialize();
                    }
                }
            }
        }


        //Pickup Events
        [System.NonSerialized] public float lastPickup;
        public override void OnPickup()
        {
            lastPickup = Time.timeSinceLevelLoad;
            TakeOwnership(false);
            if (pickup)
            {
                if (pickup.currentHand == VRC_Pickup.PickupHand.Left)
                {
                    Vector3 leftHandPos = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftHand);
                    if (leftHandPos != Vector3.zero)
                    {
                        state = STATE_LEFT_HAND_HELD;
                    }
                    else //couldn't find left hand bone
                    {
                        state = STATE_NO_HAND_HELD;
                    }
                }
                else if (pickup.currentHand == VRC_Pickup.PickupHand.Right)
                {
                    Vector3 rightHandPos = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.RightHand);
                    if (rightHandPos != Vector3.zero)
                    {
                        state = STATE_RIGHT_HAND_HELD;
                    }
                    else //couldn't find right hand bone
                    {
                        state = STATE_NO_HAND_HELD;
                    }
                }
            }
        }
        public override void OnDrop()
        {
            _print("OnDrop");
            //it takes 1 frame for VRChat to give the pickup the correct velocity, so let's wait 1 frame
            //many different states will want to change the state and in doing so force us to drop the object
            //To know if this is a player initiated drop, we check that the state is still one of being held
            //we need to set the state to interpolating next frame so we also set the lastState variable to
            //remind us to do that
            //The reason we need the lastState variable is because switching hands will cause the state to
            //go from a held state to another held state, and if, one frame from now, we set the state to
            //interpolating that will make transferring objects from one hand to another impossible
            //transferring one object from your hand to your same hand is already impossible, so if lastState
            //is equal to current state we know that it was a genuine drop
            if (IsHeld())
            {
                lastState = state;
                if (kinematicWhileHeld && rigid.isKinematic)
                {
                    rigid.isKinematic = lastKinematic;
                }
                SendCustomEventDelayedFrames(nameof(OnDropDelayed), 1);
            }
        }

        public void OnDropDelayed()
        {
            if (!IsLocalOwner() || lastState != state || !IsHeld())
            {
                return;
            }
            state = STATE_INTERPOLATING;
        }

        //Ownership Events
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            _print("OnOwnershipTransferred");
            owner = player;
        }

        //bug fix found by BoatFloater
        //bug canny here: https://vrchat.canny.io/udon-networking-update/p/1258-onownershiptransferred-does-not-fire-at-onplayerleft-if-last-owner-is-passi
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            _print("OnPlayerLeft");
            if (player == owner)
            {
                owner = Networking.GetOwner(gameObject);
            }
        }

        public void TakeOwnership(bool checkIfClogged)
        {
            if ((checkIfClogged && IsNetworkAtRisk()) || IsLocalOwner())
            {
                //Let them cook
                return;
            }
            _print("TakeOwnership");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        //STATES
        //If Udon supported custom classes that aren't subclasses of UdonBehaviour, these would be in separate files
        //Look at the 2.0 Prerelease on the github if you want to see what I mean
        //Instead, I'm going to copy all the functions in those files here
        [System.NonSerialized]
        public Vector3 startPos;
        [System.NonSerialized]
        public Quaternion startRot;
        [System.NonSerialized]
        public Vector3 startVel;
        [System.NonSerialized]
        public Vector3 startSpin;
        [System.NonSerialized]
        public Vector3 lastPos;
        [System.NonSerialized]
        public Quaternion lastRot;

        public void OnEnterState()
        {
            switch (state)
            {
                case (STATE_SLEEPING):
                    {
                        //do nothing
                        return;
                    }
                case (STATE_TELEPORTING):
                    {
                        teleport_OnEnterState();
                        return;
                    }
                case (STATE_INTERPOLATING):
                    {
                        //do nothing
                        return;
                    }
                case (STATE_FALLING):
                    {
                        //do nothing
                        return;
                    }
                case (STATE_LEFT_HAND_HELD):
                    {
                        left_OnEnterState();
                        return;
                    }
                case (STATE_RIGHT_HAND_HELD):
                    {
                        right_OnEnterState();
                        return;
                    }
                case (STATE_NO_HAND_HELD):
                    {
                        genericHand_OnEnterState();
                        return;
                    }
                case (STATE_ATTACHED_TO_PLAYSPACE):
                    {
                        //do nothing
                        if (preventStealWhileAttachedToPlayer && Utilities.IsValid(pickup))
                        {
                            pickup.pickupable = IsLocalOwner() && pickupable;
                        }
                        return;
                    }
                case (STATE_WORLD_LOCK):
                    {
                        //do nothing
                        return;
                    }
                default:
                    {
                        if (state < 0)
                        {
                            bone_OnEnterState();
                        }
                        else if (customState)
                        {
                            customState.OnEnterState();
                        }
                        return;
                    }
            }

        }
        public void OnExitState()
        {
            switch (state)
            {
                case (STATE_SLEEPING):
                    {
                        //do nothing
                        return;
                    }
                case (STATE_TELEPORTING):
                    {
                        //do nothing
                        return;
                    }
                case (STATE_INTERPOLATING):
                    {
                        //do nothing
                        return;
                    }
                case (STATE_FALLING):
                    {
                        //do nothing
                        return;
                    }
                case (STATE_LEFT_HAND_HELD):
                    {
                        genericHand_OnExitState();
                        return;
                    }
                case (STATE_RIGHT_HAND_HELD):
                    {
                        genericHand_OnExitState();
                        return;
                    }
                case (STATE_NO_HAND_HELD):
                    {
                        genericHand_OnExitState();
                        return;
                    }
                case (STATE_ATTACHED_TO_PLAYSPACE):
                    {
                        //do nothing
                        if (preventStealWhileAttachedToPlayer && Utilities.IsValid(pickup))
                        {
                            pickup.pickupable = pickupable;
                        }
                        return;
                    }
                case (STATE_WORLD_LOCK):
                    {
                        //do nothing
                        return;
                    }
                default:
                    {
                        if (state < 0)
                        {
                            SetVelocityFromLastTransform();
                            if (preventStealWhileAttachedToPlayer && Utilities.IsValid(pickup))
                            {
                                pickup.pickupable = pickupable;
                            }
                        }
                        else if (customState)
                        {
                            customState.OnExitState();
                        }
                        return;
                    }
            }

        }
        public void OnSmartObjectSerialize()
        {
            switch (state)
            {
                case (STATE_SLEEPING):
                    {
                        sleep_OnSmartObjectSerialize();
                        return;
                    }
                case (STATE_TELEPORTING):
                    {
                        teleport_OnSmartObjectSerialize();
                        return;
                    }
                case (STATE_INTERPOLATING):
                    {
                        interpolate_OnSmartObjectSerialize();
                        return;
                    }
                case (STATE_FALLING):
                    {
                        falling_OnSmartObjectSerialize();
                        return;
                    }
                case (STATE_LEFT_HAND_HELD):
                    {
                        bone_CalcParentTransform();
                        generic_OnSmartObjectSerialize();
                        return;
                    }
                case (STATE_RIGHT_HAND_HELD):
                    {
                        bone_CalcParentTransform();
                        generic_OnSmartObjectSerialize();
                        return;
                    }
                case (STATE_NO_HAND_HELD):
                    {
                        noHand_CalcParentTransform();
                        generic_OnSmartObjectSerialize();
                        return;
                    }
                case (STATE_ATTACHED_TO_PLAYSPACE):
                    {
                        playspace_CalcParentTransform();
                        generic_OnSmartObjectSerialize();
                        return;
                    }
                case (STATE_WORLD_LOCK):
                    {
                        world_OnSmartObjectSerialize();
                        return;
                    }
                default:
                    {
                        if (state < 0)
                        {
                            bone_CalcParentTransform();
                            generic_OnSmartObjectSerialize();
                        }
                        else if (customState)
                        {
                            customState.OnSmartObjectSerialize();
                        }
                        return;
                    }
            }
        }
        public void OnInterpolationStart()
        {
            interpolationStartTime = Time.timeSinceLevelLoad;
            switch (state)
            {
                case (STATE_SLEEPING):
                    {
                        sleep_OnInterpolationStart();
                        return;
                    }
                case (STATE_TELEPORTING):
                    {
                        teleport_OnInterpolationStart();
                        return;
                    }
                case (STATE_INTERPOLATING):
                    {
                        interpolate_OnInterpolationStart();
                        return;
                    }
                case (STATE_FALLING):
                    {
                        falling_OnInterpolationStart();
                        return;
                    }
                case (STATE_LEFT_HAND_HELD):
                    {
                        bone_OnInterpolationStart();
                        return;
                    }
                case (STATE_RIGHT_HAND_HELD):
                    {
                        bone_OnInterpolationStart();
                        return;
                    }
                case (STATE_NO_HAND_HELD):
                    {
                        noHand_OnInterpolationStart();
                        return;
                    }
                case (STATE_ATTACHED_TO_PLAYSPACE):
                    {
                        playspace_CalcParentTransform();
                        generic_OnInterpolationStart();
                        return;
                    }
                case (STATE_WORLD_LOCK):
                    {
                        world_OnInterpolationStart();
                        return;
                    }
                default:
                    {
                        if (state < 0)
                        {
                            bone_OnInterpolationStart();
                        }
                        else if (customState)
                        {
                            customState.OnInterpolationStart();
                        }
                        return;
                    }
            }
        }
        public void OnInterpolate(float interpolation)
        {
            transformRecorded = false;
            switch (state)
            {
                case (STATE_SLEEPING):
                    {
                        sleep_Interpolate(interpolation);
                        return;
                    }
                case (STATE_TELEPORTING):
                    {
                        //do nothing
                        return;
                    }
                case (STATE_INTERPOLATING):
                    {
                        interpolate_Interpolate(interpolation);
                        return;
                    }
                case (STATE_FALLING):
                    {
                        falling_Interpolate(interpolation);
                        return;
                    }
                case (STATE_LEFT_HAND_HELD):
                    {
                        left_Interpolate(interpolation);
                        return;
                    }
                case (STATE_RIGHT_HAND_HELD):
                    {
                        right_Interpolate(interpolation);
                        return;
                    }
                case (STATE_NO_HAND_HELD):
                    {
                        noHand_Interpolate(interpolation);
                        return;
                    }
                case (STATE_ATTACHED_TO_PLAYSPACE):
                    {
                        playspace_CalcParentTransform();
                        generic_Interpolate(interpolation);
                        return;
                    }
                case (STATE_WORLD_LOCK):
                    {
                        world_Interpolate(interpolation);
                        return;
                    }
                default:
                    {
                        if (state < 0)
                        {
                            bone_CalcParentTransform();
                            generic_Interpolate(interpolation);
                        }
                        else if (customState)
                        {
                            customState.Interpolate(interpolation);
                        }
                        return;
                    }
            }
        }
        public bool OnInterpolationEnd()
        {
            if (interpolationEndTime <= interpolationStartTime)
            {
                interpolationEndTime = Time.timeSinceLevelLoad;
            }
            switch (state)
            {
                case (STATE_SLEEPING):
                    {
                        return sleep_OnInterpolationEnd();
                    }
                case (STATE_TELEPORTING):
                    {
                        return false;//do nothing
                    }
                case (STATE_INTERPOLATING):
                    {
                        return interpolate_OnInterpolationEnd();
                    }
                case (STATE_FALLING):
                    {
                        return falling_OnInterpolationEnd();
                    }
                case (STATE_LEFT_HAND_HELD):
                    {
                        return genericHand_onInterpolationEnd();
                    }
                case (STATE_RIGHT_HAND_HELD):
                    {
                        return genericHand_onInterpolationEnd();
                    }
                case (STATE_NO_HAND_HELD):
                    {
                        return genericHand_onInterpolationEnd();
                    }
                case (STATE_ATTACHED_TO_PLAYSPACE):
                    {
                        return genericAttachment_OnInterpolationEnd();
                    }
                case (STATE_WORLD_LOCK):
                    {
                        return world_OnInterpolationEnd();
                    }
                default:
                    {
                        if (state < 0)
                        {
                            return genericAttachment_OnInterpolationEnd();
                        }
                        else if (customState)
                        {
                            return customState.OnInterpolationEnd();
                        }
                        return false;
                    }
            }
        }


        //Sleep State
        //This state interpolates objects to a position and then attempts to put their rigidbody to sleep
        //If the rigidbody can't sleep, like if it's floating in mid-air or something, then the just holds the position and rotation.
        //If the rigidbody does fall asleep in the right position and rotation, then the state disables the update loop for optimization
        [System.NonSerialized] public Vector3 sleepPos;
        [System.NonSerialized] public Quaternion sleepRot;
        [System.NonSerialized] public float lastSleep = -1001f;
        public void sleep_OnInterpolationStart()
        {
            if (worldSpaceSleep)
            {
                startPos = transform.position;
                startRot = transform.rotation;
                startVel = rigid.velocity;
                startSpin = rigid.angularVelocity;
            }
            else
            {
                startPos = transform.localPosition;
                startRot = transform.localRotation;
                startVel = transform.InverseTransformVector(rigid.velocity);
                startSpin = transform.InverseTransformVector(rigid.angularVelocity);
            }
        }
        public void sleep_Interpolate(float interpolation)
        {
            if (IsLocalOwner())
            {
                return;
            }
            if (interpolation < 1.0f)
            {
                if (worldSpaceSleep)
                {
                    transform.position = HermiteInterpolatePosition(startPos, startVel, pos, Vector3.zero, interpolation);
                    transform.rotation = HermiteInterpolateRotation(startRot, startSpin, rot, Vector3.zero, interpolation);
                }
                else
                {
                    transform.localPosition = HermiteInterpolatePosition(startPos, startVel, pos, Vector3.zero, interpolation);
                    transform.localRotation = HermiteInterpolateRotation(startRot, startSpin, rot, Vector3.zero, interpolation);
                }
                rigid.velocity = Vector3.zero;
                rigid.angularVelocity = Vector3.zero;
            }
            else if (!reduceJitterDuringSleep || ObjectMovedDuringSleep())
            {
                if (worldSpaceSleep)
                {
                    transform.position = pos;
                    transform.rotation = rot;
                }
                else
                {
                    transform.localPosition = pos;
                    transform.localRotation = rot;
                }
                rigid.velocity = Vector3.zero;
                rigid.angularVelocity = Vector3.zero;
                rigid.Sleep();
                lastSleep = Time.timeSinceLevelLoad;
            }
        }

        public bool sleep_OnInterpolationEnd()
        {
            if (IsLocalOwner() || rigid.isKinematic || (rigid.IsSleeping() && (!reduceJitterDuringSleep || lastSleep != Time.timeSinceLevelLoad)))
            {
                _print("successfully slept");
                return false;
            }
            return true;
        }

        public void sleep_OnSmartObjectSerialize()
        {
            if (worldSpaceSleep)
            {
                pos = transform.position;
                rot = transform.rotation;
            }
            else
            {
                pos = transform.localPosition;
                rot = transform.localRotation;
            }
            vel = Vector3.zero;
            spin = Vector3.zero;
        }

        public bool ObjectMovedDuringSleep()
        {
            return transform.position != pos || transform.rotation != rot;
        }

        //Teleport State
        //This state has no interpolation. It simply places sets the transforms and velocity then disables the update loop, letting physics take over
        //For the owner this has to happen when we enter the state before we serialize everything for the first time
        //non-owners see this happen in OnInterpolationStart like normal
        public void teleport_OnEnterState()
        {
            if (IsLocalOwner())
            {
                teleportFlagSet = true;
                if (worldSpaceTeleport)
                {
                    transform.position = pos;
                    transform.rotation = rot;
                    if (!rigid.isKinematic)
                    {
                        rigid.velocity = vel;
                        rigid.angularVelocity = spin;
                    }
                }
                else
                {
                    transform.localPosition = pos;
                    transform.localRotation = rot;
                    if (!rigid.isKinematic)
                    {
                        rigid.velocity = transform.TransformVector(vel);
                        rigid.angularVelocity = transform.TransformVector(spin);
                    }
                }
            }
        }
        public void teleport_OnInterpolationStart()
        {
            if (!IsLocalOwner())
            {
                if (worldSpaceTeleport)
                {
                    transform.position = pos;
                    transform.rotation = rot;
                    if (!rigid.isKinematic)
                    {
                        rigid.velocity = vel;
                        rigid.angularVelocity = spin;
                    }
                }
                else
                {
                    transform.localPosition = pos;
                    transform.localRotation = rot;
                    if (!rigid.isKinematic)
                    {
                        rigid.velocity = transform.TransformVector(vel);
                        rigid.angularVelocity = transform.TransformVector(spin);
                    }
                }
            }
            loop = false;//turn off immediately for max optimization
        }
        public void teleport_OnSmartObjectSerialize()
        {
            teleportFlagSet = false;
            if (worldSpaceTeleport)
            {
                pos = transform.position;
                rot = transform.rotation;
                if (!rigid.isKinematic)
                {
                    vel = rigid.velocity;
                    spin = rigid.angularVelocity;
                }
            }
            else
            {
                pos = transform.localPosition;
                rot = transform.localRotation;
                if (!rigid.isKinematic)
                {
                    vel = transform.InverseTransformVector(rigid.velocity);
                    spin = transform.InverseTransformVector(rigid.angularVelocity);
                }
            }
        }

        //Interpolate State
        //This state interpolates objects into place using Hermite interpolation which makes sure that all changes in velocity are gradual and smooth
        //At the end of the state we set the velocity and then disable the update loop for optimization and to allow the physics engine to take over
        public void interpolate_OnInterpolationStart()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            startVel = rigid.velocity;
            startSpin = rigid.angularVelocity;
        }
        public void interpolate_Interpolate(float interpolation)
        {
            if (IsLocalOwner())
            {
                if (transform.position.y <= respawnHeight)
                {
                    Respawn();
                }
                return;
            }
            transform.position = HermiteInterpolatePosition(startPos, startVel, pos, vel, interpolation);
            transform.rotation = HermiteInterpolateRotation(startRot, startSpin, rot, spin, interpolation);
        }

        public bool interpolate_OnInterpolationEnd()
        {
            if (IsLocalOwner())
            {
                if (rigid.isKinematic || rigid.IsSleeping())
                {
                    //wait around for the rigidbody to fall asleep
                    state = STATE_SLEEPING;
                }
                // else if (NonGravitationalAcceleration())
                // {
                //     //some force other than gravity is acting upon our object
                //     if (lastResync + lerpTime < Time.timeSinceLevelLoad)
                //     {
                //         Synchronize();
                //     }
                // }
                //returning true means we extend the interpolation period
                return true;
            }

            rigid.velocity = vel;
            rigid.angularVelocity = spin;
            //let physics take over by returning false
            return false;
        }

        public void interpolate_OnSmartObjectSerialize()
        {
            pos = transform.position;
            rot = transform.rotation;
            if (!rigid.isKinematic)
            {
                vel = rigid.velocity;
                spin = rigid.angularVelocity;
            }
            else
            {
                vel = Vector3.zero;
                spin = Vector3.zero;
            }
        }

        [System.NonSerialized]
        Vector3 changeInVelocity;
        public bool NonGravitationalAcceleration()
        {
            if (rigid.isKinematic)
            {
                return false;
            }
            //returns true of object's velocity changed along an axis other than gravity's
            //this will let us know if the object stayed in projectile motion or if another force acted upon it
            changeInVelocity = rigid.velocity - vel;

            if (changeInVelocity.magnitude < 0.001f)
            {
                //too small to care
                return false;
            }

            if (!rigid.useGravity)
            {
                return true;
            }

            if (Vector3.Angle(changeInVelocity, Physics.gravity) > 90)
            {
                //This means that the object was moving against the force of gravity
                //there is definitely a non-gravitational velocity change
                return true;
            }

            //we know that the object acelerated along the gravity vector,
            //but if it also acelerated on another axis then another force acted upon it
            //here we remove the influence of gravity and compare the velocity with the last synced velocity
            return Vector3.ProjectOnPlane(changeInVelocity, Physics.gravity).magnitude > 0.001f;
        }

        //Falling State
        //This state also uses Hermite interpolation, but assumes that the object is in projectile motion and intentionally creates a sudden change in velocity at the end of the interpolation to mimic a bounce.
        //To mimic projectile motion, we assume that the velocity at the end of the interpolation is the start velocity plus the change in velocity gravity would have caused over the same time period
        //At the end of the interpolation, we change the velocity to match what was sent by the owner, putting us back in sync with the owner.
        //Then we disable to update loop for optimization and to allow the physics engine to take over
        bool simulateBounce = false;
        public void falling_OnInterpolationStart()
        {
            startPos = transform.position;
            startRot = transform.rotation;
            startVel = rigid.velocity;
            startSpin = rigid.angularVelocity;
            simulateBounce = Vector3.Distance(startVel, vel) > fallSpeed;
        }
        public void falling_Interpolate(float interpolation)
        {
            if (!simulateBounce)
            {
                //change in velocity wasn't great enough to be a bounce, so we just interpolate normally instead
                interpolate_Interpolate(interpolation);
                return;
            }

            if (IsLocalOwner())
            {
                if (transform.position.y <= respawnHeight)
                {
                    Respawn();
                }
                return;
            }

            if (!rigid.isKinematic && rigid.useGravity)
            {
                transform.position = HermiteInterpolatePosition(startPos, startVel, pos, startVel + Physics.gravity * lagTime, interpolation);
                transform.rotation = HermiteInterpolateRotation(startRot, startSpin, rot, startSpin, interpolation);
            }
            else
            {
                transform.position = HermiteInterpolatePosition(startPos, startVel, pos, startVel, interpolation);
                transform.rotation = HermiteInterpolateRotation(startRot, startSpin, rot, startSpin, interpolation);
            }
        }
        public bool falling_OnInterpolationEnd()
        {
            if (IsLocalOwner())
            {
                if (rigid.isKinematic || rigid.IsSleeping())
                {
                    //wait around for the rigidbody to fall asleep
                    state = STATE_SLEEPING;
                }
                // else if (NonGravitationalAcceleration())
                // {
                //     //some force other than gravity is acting upon our object
                //     if (lastResync + lerpTime < Time.timeSinceLevelLoad)
                //     {
                //         Synchronize();
                //     }
                // }
                //returning true means we extend the interpolation period
                return true;
            }
            rigid.velocity = vel;
            rigid.angularVelocity = spin;

            return false;
        }

        public void falling_OnSmartObjectSerialize()
        {
            pos = transform.position;
            rot = transform.rotation;
            if (!rigid.isKinematic)
            {
                vel = rigid.velocity;
                spin = rigid.angularVelocity;
            }
            else
            {
                vel = Vector3.zero;
                spin = Vector3.zero;
            }
        }

        //Generic Attachment State
        //We can't actually set the state to this state. This is just a helper that we use in other states when objects need to be attached to things
        [System.NonSerialized]
        public Vector3 parentPos;
        [System.NonSerialized]
        public Quaternion parentRot;

        //these values are arbitrary, but they work pretty good for most pickups
        [System.NonSerialized]
        public float positionResyncThreshold = 0.015f;
        [System.NonSerialized]
        public float rotationResyncThreshold = 0.995f;
        [System.NonSerialized]
        public float lastResync = -1001f;
        public void generic_OnInterpolationStart()
        {
            // CalcParentTransform();
            startPos = CalcPos();
            startRot = CalcRot();
        }
        public void generic_Interpolate(float interpolation)
        {
            RecordLastTransform();
            transform.position = HermiteInterpolatePosition(parentPos + parentRot * startPos, Vector3.zero, parentPos + parentRot * pos, Vector3.zero, interpolation);
            transform.rotation = HermiteInterpolateRotation(parentRot * startRot, Vector3.zero, parentRot * rot, Vector3.zero, interpolation);
            rigid.velocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
        public bool genericAttachment_OnInterpolationEnd()
        {
            if (IsLocalOwner())
            {
                if (generic_ObjectMoved())
                {
                    if (lastResync + lagTime < Time.timeSinceLevelLoad)
                    {
                        lastResync = Time.timeSinceLevelLoad;
                        Serialize();
                    }
                }
                else
                {
                    lastResync = Time.timeSinceLevelLoad;
                }
            }
            return true;
        }

        public void generic_OnSmartObjectSerialize()
        {
            // CalcParentTransform();
            pos = CalcPos();
            rot = CalcRot();
            vel = CalcVel();
            spin = CalcSpin();
            lastResync = Time.timeSinceLevelLoad;
        }

        public Vector3 CalcPos()
        {
            return Quaternion.Inverse(parentRot) * (transform.position - parentPos);
        }
        public Quaternion CalcRot()
        {
            return Quaternion.Inverse(parentRot) * transform.rotation;
        }
        public Vector3 CalcVel()
        {
            return (transform.position - lastPos) / Time.deltaTime;
        }

        float angle;
        Vector3 axis;
        public Vector3 CalcSpin()
        {
            //angular velocity is normalized rotation axis * angle in radians: https://answers.unity.com/questions/49082/rotation-quaternion-to-angular-velocity.html
            Quaternion.Normalize(Quaternion.Inverse(lastRot) * transform.rotation).ToAngleAxis(out angle, out axis);

            //Make sure we are using the smallest angle of rotation. I.E. -90 degrees instead of 270 degrees wherever possible
            if (angle < -180)
            {
                angle += 360;
            }
            else if (angle > 180)
            {
                angle -= 360;
            }

            return transform.rotation * axis * angle * Mathf.Deg2Rad / Time.deltaTime;
        }

        public bool generic_ObjectMoved()
        {
            return Vector3.Distance(CalcPos(), pos) > positionResyncThreshold || Quaternion.Dot(CalcRot(), rot) < rotationResyncThreshold;//arbitrary values to account for pickups wiggling a little in your hand
        }

        [System.NonSerialized] public bool _pickupable = true;
        public bool pickupable
        {
            get => _pickupable;
            set
            {
                _pickupable = value;
                if (Utilities.IsValid(pickup))
                {
                    if (IsHeld())
                    {
                        if (IsLocalOwner())
                        {
                            pickup.pickupable = value && allowTheftFromSelf;
                        }
                        else
                        {
                            pickup.pickupable = value && !pickup.DisallowTheft;
                        }
                    }
                    else if (IsAttachedToPlayer())
                    {
                        pickup.pickupable = value && (!preventStealWhileAttachedToPlayer || IsLocalOwner());
                    }
                    else
                    {
                        pickup.pickupable = value;
                    }
                }
            }
        }
        [System.NonSerialized] public bool lastKinematic = false;
        public void preventPickupJitter()
        {
            transform.position = parentPos + parentRot * pos;
            transform.rotation = parentRot * rot;
        }
        //Left Hand Held State
        //This state syncs the transforms relative to the left hand of the owner
        //This state is useful for when someone is holding the object in their left hand
        //We do not disable the update loop because the left hand is going to move around and we need to continually update the object transforms to match.
        //Falls back to Playspace Attachment State if no left hand bone is found
        public void left_OnEnterState()
        {
            bone = HumanBodyBones.LeftHand;
            genericHand_OnEnterState();
        }
        public void left_Interpolate(float interpolation)
        {
            //let the VRC_pickup script handle transforms for the local owner
            //only reposition it for non-owners
            bone_CalcParentTransform();
            if (!IsLocalOwner())
            {
                generic_Interpolate(interpolation);
            }
            else if (nonKinematicPickupJitterPreventionTime > 0 && lastState == state && Time.timeSinceLevelLoad - lastPickup > nonKinematicPickupJitterPreventionTime)
            {
                preventPickupJitter();
            }
        }

        //Right Hand Held State
        //Same as the Left Hand Held State, but for right hands
        public void right_OnEnterState()
        {
            bone = HumanBodyBones.RightHand;
            genericHand_OnEnterState();
        }
        public void right_Interpolate(float interpolation)
        {
            //let the VRC_pickup script handle transforms for the local owner
            //only reposition it for non-owners
            //we need to keep the parent transform up to date though
            bone_CalcParentTransform();
            if (!IsLocalOwner())
            {
                generic_Interpolate(interpolation);
            }
            else if (nonKinematicPickupJitterPreventionTime > 0 && lastState == state && Time.timeSinceLevelLoad - lastPickup > nonKinematicPickupJitterPreventionTime)
            {
                preventPickupJitter();
            }
        }
        //Right Hand Held State
        //Same as the Left Hand Held State, but for right hands
        public void genericHand_OnEnterState()
        {
            if (Utilities.IsValid(pickup))
            {
                if (IsLocalOwner())
                {
                    pickup.pickupable = pickupable && allowTheftFromSelf;
                }
                else
                {
                    pickup.pickupable = pickupable && !pickup.DisallowTheft;
                }
            }

            if (kinematicWhileHeld)
            {
                lastKinematic = rigid.isKinematic;
                rigid.isKinematic = true;
            }
        }

        public bool genericHand_onInterpolationEnd()
        {
            if (IsLocalOwner() && (pickup.orientation != VRC_Pickup.PickupOrientation.Any || owner.IsUserInVR()) && (rigid.isKinematic || nonKinematicPickupJitterPreventionTime > 0) && lastResync + lagTime < Time.timeSinceLevelLoad && interpolationStartTime + 0.5f < Time.timeSinceLevelLoad)//hard coded 0.5f because VRChat is like that
            {
                if (generic_ObjectMoved())
                {
                    lastResync = Time.timeSinceLevelLoad;
                    Serialize();
                }
                else
                {
                    lastResync = Time.timeSinceLevelLoad;
                    return false;
                }
            }
            return genericAttachment_OnInterpolationEnd();
        }

        public void genericHand_OnExitState()
        {
            if (Utilities.IsValid(pickup))
            {
                pickup.pickupable = pickupable;
            }
            if (kinematicWhileHeld && rigid.isKinematic)
            {
                rigid.isKinematic = lastKinematic;
            }
        }
        public void noHand_OnInterpolationStart()
        {
            //if the avatar we're wearing doesn't have the bones required, fallback to attach to playspace
            noHand_CalcParentTransform();
            generic_OnInterpolationStart();
        }
        public void noHand_Interpolate(float interpolation)
        {
            //let the VRC_pickup script handle transforms for the local owner
            //only reposition it for non-owners
            //we need to keep the parent transform up to date though
            noHand_CalcParentTransform();
            if (!IsLocalOwner())
            {
                generic_Interpolate(interpolation);
            }
            else if (nonKinematicPickupJitterPreventionTime > 0 && lastState == state && Time.timeSinceLevelLoad - lastPickup > nonKinematicPickupJitterPreventionTime)
            {
                preventPickupJitter();
            }
        }
        public void noHand_CalcParentTransform()
        {
            if (Utilities.IsValid(owner))
            {
                parentPos = owner.GetPosition();
                parentRot = owner.GetRotation();
            }
        }

        //Bone Attachment State
        //Same as the left and right hand held states, but can be applied to any bone defined in HumanBodyBones
        [System.NonSerialized]
        public bool hasBones = false;
        [System.NonSerialized]
        public HumanBodyBones bone;

        public void bone_OnEnterState()
        {
            bone = (HumanBodyBones)(-1 - state);

            if (preventStealWhileAttachedToPlayer && Utilities.IsValid(pickup))
            {
                pickup.pickupable = IsLocalOwner() && pickupable;
            }
        }
        public void bone_OnInterpolationStart()
        {
            //if the avatar we're wearing doesn't have the bones required, fallback to attach to playspace
            bone_CalcParentTransform();
            if (IsLocalOwner())
            {
                if (!hasBones)
                {
                    _printErr("Avatar is missing the correct bone. Falling back to playspace attachment.");
                    state = STATE_ATTACHED_TO_PLAYSPACE;
                    return;
                }
            }
            generic_OnInterpolationStart();
        }
        public void bone_CalcParentTransform()
        {
            if (Utilities.IsValid(owner))
            {
                parentPos = owner.GetBonePosition(bone);
                parentRot = owner.GetBoneRotation(bone);
                hasBones = parentPos != Vector3.zero;
                parentPos = hasBones ? parentPos : owner.GetPosition();
                parentRot = hasBones ? parentRot : owner.GetRotation();
            }
        }

        //Playspace Attachment State
        //Same as Bone Attachment state, but uses the player's transform instead of one of their bone's transforms.
        //Useful as a fallback when the avatar is missing certain bones
        public void playspace_CalcParentTransform()
        {
            if (Utilities.IsValid(owner))
            {
                parentPos = owner.GetPosition();
                parentRot = owner.GetRotation();
            }
        }

        //Worldspace Attachment State
        //Locks an object's transform in world space
        //Useful as intermediate state when transferring ownership.
        //For example, if request to transfer ownership of an object comes before the new owner can update the state and transform of that object,
        //there will be a few frames when the owner is correct, but the state and transforms are wrong.
        //If the old state was left hand held, but it's meant to be right hand held, then you'll see a few confusing frames where the object teleports between hands.
        //It's often better in this scenario to just lock the object to world space before transferring ownership so there's no teleporting in these intermediate frames.
        public void world_OnInterpolationStart()
        {
            startPos = transform.position;
            startRot = transform.rotation;
        }
        public void world_Interpolate(float interpolation)
        {
            transform.position = HermiteInterpolatePosition(startPos, Vector3.zero, pos, Vector3.zero, interpolation);
            transform.rotation = HermiteInterpolateRotation(startRot, Vector3.zero, rot, Vector3.zero, interpolation);
            rigid.velocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
        public bool world_OnInterpolationEnd()
        {
            return true;
        }

        public void world_OnSmartObjectSerialize()
        {
            pos = transform.position;
            rot = transform.rotation;
            vel = Vector3.zero;
            spin = Vector3.zero;
        }
        public void AddListener(SmartObjectSyncListener newListener)
        {
            foreach (SmartObjectSyncListener l in listeners)
            {
                if (l == newListener)
                {
                    //it's already in there, don't do anything
                    return;
                }
            }

            SmartObjectSyncListener[] newListeners = new SmartObjectSyncListener[listeners.Length + 1];
            listeners.CopyTo(newListeners, 0);
            newListeners[newListeners.Length - 1] = newListener;
            listeners = newListeners;
        }
        public void RemoveListener(SmartObjectSyncListener oldListener)
        {
            if (listeners.Length <= 0)
            {
                //no listeners
                return;
            }
            SmartObjectSyncListener[] newListeners = new SmartObjectSyncListener[listeners.Length - 1];

            int i = 0;
            foreach (SmartObjectSyncListener l in listeners)
            {
                if (l == oldListener)
                {
                    //this is the listener we're removing
                    break;
                }
                if (i >= newListeners.Length)
                {
                    //only happens when no match is found
                    return;
                }
                newListeners[i] = l;
                i++;
            }
            listeners = newListeners;
        }
        public void TogglePickupable()
        {
            pickupable = !pickupable;
        }

        public void DisablePickupable()
        {
            pickupable = false;
        }

        public void EnablePickupable()
        {
            pickupable = true;
        }
    }
}