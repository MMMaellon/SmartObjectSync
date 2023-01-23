
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using VRC.SDKBase.Editor.BuildPipeline;
using UnityEditor;
using UdonSharpEditor;
using System.Collections.Immutable;


namespace MMMaellon
{
    [CustomEditor(typeof(SmartObjectSync)), CanEditMultipleObjects]

    public class SmartObjectSyncEditor : Editor
    {
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
                if (sync.printDebugMessages)
                    Debug.LogFormat("[SmartObjectSync] {0} Auto Setup complete:\n{1}\n{2}", sync.name, sync.pickup == null ? "No VRC_Pickup component found" : "VRC_Pickup component found", sync.rigid == null ? "No Rigidbody component found" : "Rigidbody component found");
            } else {
                Debug.LogFormat("[SmartObjectSync] Auto Setup failed: No SmartObjectSync selected");
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

            if (syncFound)
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
            foreach (SmartObjectSync sync in Selection.GetFiltered<SmartObjectSync>(SelectionMode.Editable))
            {
                if (sync)
                {
                    syncCount++;
                    if (sync.pickup == null && sync.GetComponent<VRC_Pickup>() != null)
                    {
                        pickupSetupCount++;
                    }
                    if (sync.rigid == null && sync.GetComponent<Rigidbody>() != null)
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
                }
            }
            if ((pickupSetupCount > 0 || rigidSetupCount > 0 || respawnYSetupCount > 0 || helperSetupCount > 0))
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
                if (GUILayout.Button(new GUIContent("Auto Setup")))
                {
                    SetupSelectedSmartObjectSyncs();
                }
            }
            EditorGUILayout.Space();
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}

#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SmartObjectSync : UdonSharpBehaviour
    {
        public bool printDebugMessages = false;
        public bool takeOwnershipOfOtherObjectsOnCollision = true;
        public bool allowOthersToTakeOwnershipOnCollision = true;

        [HideInInspector]
        public SmartObjectSyncHelper helper;
        public float respawn_height = -1001f;
        public VRC_Pickup pickup;
        public Rigidbody rigid;
        public bool setSpawnFromLocalTransform = true;
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
        //negative for attached to a human body bone where the absolute value is the int value of the HumanBodyBone enum + 1
        
        
        
        [System.NonSerialized, FieldChangeCallback(nameof(lastSync))]
        public float _lastSync = -1001f;
        [System.NonSerialized]
        public float lerpProgress = 0f;
        [System.NonSerialized]
        public Vector3 posOnSync;
        [System.NonSerialized]
        public Quaternion rotOnSync;
        [System.NonSerialized]
        public Vector3 velOnSync;
        [System.NonSerialized]
        public Vector3 spinOnSync;
        
        
        
        public float lastSync{
            get => _lastSync;
            set
            {
                _lastSync = value;
                if (helper)
                {
                    helper.enabled = true;
                }
            }
        }
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
                if (pickup != null && value != STATE_LEFT_HAND_HELD && value != STATE_RIGHT_HAND_HELD)
                {
                    pickup.Drop();
                }
                lastState = _state;
                _state = value;

                if (Utilities.IsValid(owner) && owner.isLocal)
                {
                    Synchronize();
                    // CalcParentTransform();
                    // pos = CalcPos();
                    // _print("set pos from state " + pos.y);
                    // rot = CalcRot();
                    // vel = CalcVel();
                    // spin = CalcSpin();
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
                            _print("state: " + ((HumanBodyBones) (-1 - value)).ToString());
                            break;
                        }
                }
            }
        }

        [System.NonSerialized, FieldChangeCallback(nameof(owner))]
        public VRCPlayerApi _owner;
        [System.NonSerialized]
        public Vector3 spawnPos;
        [System.NonSerialized]
        public Quaternion spawnRot;
        public VRCPlayerApi owner{
            get => _owner;
            set
            {
                if (_owner != value)
                {
                    _print("new owner: " + (!Utilities.IsValid(value) ? "invalid" : value.displayName));
                    // if (_owner != value)
                    // {
                    //     RecordSyncTransforms();
                    // }
                    _owner = value;
                    // lastSync = -1001f;
                    lastOwnershipChange = Time.timeSinceLevelLoad;
                    if (!Utilities.IsValid(_owner) || !_owner.isLocal)
                    {
                        if (pickup)
                        {
                            pickup.Drop();
                        }
                    }
                    else
                    {
                        WakeUp();
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
        bool startRan;
        public void Start()
        {
            if (!helper)
            {
                //we need to create the helper
                helper = GetComponent<SmartObjectSyncHelper>();
                if (helper && (helper.sync == this || helper.sync == null))
                {
                    helper.sync = this;
                }
                else
                {
                    _printErr("No helper found");
                    enabled = false;
                    return;
                }
            }
            spawnPos = setSpawnFromLocalTransform ? transform.localPosition : transform.position;
            spawnRot = setSpawnFromLocalTransform ? transform.localRotation : transform.rotation;
            owner = Networking.GetOwner(gameObject);
            //setting owner also turns on the helper
            pos = transform.position;
            _print("set pos from start " + pos.y);
            rot = transform.rotation;
            if (Utilities.IsValid(owner) && owner.isLocal)
            {
                RequestSerialization();
            }
            startRan = true;
        }

        public void OnEnable()
        {
            if (startRan)
            {
                if (helper)
                {
                    helper.enabled = true;
                }
                posOnSync = pos;
                rotOnSync = rot;
                velOnSync = vel;
                spinOnSync = spin;
                CalcParentTransform();
                MoveToSyncedTransform();


                _print("OnEnable - trying to find owner");
                owner = Networking.GetOwner(gameObject);
                _print("owner is null " + (!Utilities.IsValid(owner)));
                if (Utilities.IsValid(owner))
                {
                    _print("owner: " + owner.displayName);
                }
            }
        }

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

        public void Respawn()
        {
            if (!Utilities.IsValid(owner) || !owner.isLocal)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            if (setSpawnFromLocalTransform && transform.parent != null)
            {
                TeleportTo(transform.parent.TransformPoint(spawnPos), transform.parent.rotation * spawnRot, Vector3.zero, Vector3.zero);
            }
            else
            {
                TeleportTo(spawnPos, spawnRot, Vector3.zero, Vector3.zero);
            }
        }
        // public void Respawn()
        // {
        //     if (!Utilities.IsValid(owner) || !owner.isLocal)
        //     {
        //         return;
        //     }
        //     if (setSpawnFromLocalTransform && transform.parent != null)
        //     {
        //         TeleportTo(transform.parent.TransformPoint(spawnPos), transform.parent.rotation * spawnRot, Vector3.zero, Vector3.zero);
        //     }
        //     else
        //     {
        //         TeleportTo(spawnPos, spawnRot, Vector3.zero, Vector3.zero);
        //     }
        // }

        public void TeleportTo(Vector3 newPos, Quaternion newRot, Vector3 newVel, Vector3 newSpin)
        {
            state = STATE_TELEPORTING;
            pos = newPos;
            _print("set pos from teleport " + pos.y);
            rot = newRot;
            vel = newVel;
            spin = newSpin;
            CalcParentTransform();
            MoveToSyncedTransform();
        }

        [System.NonSerialized] public bool hasBones;
        [System.NonSerialized] public Vector3 parentPosCache;
        [System.NonSerialized] public Quaternion parentRotCache;
        public override void OnPreSerialization()
        {
            base.OnPreSerialization();
            _print("OnPreSerialization");
            //we set the last sync time here and in ondeserialization
            lastSync = Time.timeSinceLevelLoad;
            CalcParentTransform();
            pos = CalcPos();
            _print("set pos from OnPreSerialization " + pos.y);
            rot = CalcRot();
            vel = IsAttachedToPlayer() ? Vector3.zero : CalcVel();
            spin = IsAttachedToPlayer() ? Vector3.zero : CalcSpin();

            _print("pos at end of OnPreSerialization: " + pos.y);
        }

        public override void OnDeserialization()
        {
            base.OnDeserialization();
            _print("OnDeserialization");
            RecordSyncTransforms();
            lastSync = Time.timeSinceLevelLoad;
            _print("pos at end of OnDeserialization: " + pos.y);
        }

        public void RecordSyncTransforms()
        {
            CalcParentTransform();
            posOnSync = CalcPos();
            rotOnSync = CalcRot();
            velOnSync = LastStateIsAttachedToPlayer() ? Vector3.zero : CalcVel();
            spinOnSync = LastStateIsAttachedToPlayer() ? Vector3.zero : CalcSpin();
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
            if (resyncDelay > 0 && lastSyncFail + resyncDelay > Time.timeSinceLevelLoad)
            {
                _printErr("sync failure came too soon after last sync failure");
                //means that we already retried a failed sync and are just waiting for the Request to go through
                return;
            }
            //we double the last delay in an attempt to recreate exponential backoff
            resyncDelay = resyncDelay == 0 ? 0.1f : (resyncDelay * 2);
            lastSyncFail = Time.timeSinceLevelLoad;
            SendCustomEventDelayedSeconds(nameof(Synchronize), resyncDelay);
        }

        public void Synchronize()
        {
            if (!Utilities.IsValid(owner) || !owner.isLocal)
            {
                return;
            }
            if (resyncDelay > 0 && lastSyncFail + resyncDelay > Time.timeSinceLevelLoad + 0.1f)//0.1 is for floating point errors
            {
                _printErr("sync request came too soon after last sync failure");
                _printErr("lastSyncFail: " + lastSyncFail);
                _printErr("resyncDelay: " + resyncDelay);
                _printErr("Time.timeSinceLevelLoad: " + Time.timeSinceLevelLoad);
                return;
            }

            RequestSerialization();
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
            return lastSync + lerpTime < Time.timeSinceLevelLoad;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            _print("OnOwnershipTransferred");
            _print("new player is null: " + (player == null));
            if (player != null)
            {
                _print("player name: " + player.displayName);
            }
            owner = player;
            // if (LerpDelayOver())
            // {
                
            // }

            if (IsAttachedToPlayer() && Utilities.IsValid(owner) && owner.isLocal)
            {
                state = STATE_LERPING;
            }
            // base.OnOwnershipTransferred(player);
        }

        public void Sleep()
        {
            _print("sleep");
            if (rigid)
            {
                // rigid.velocity = Vector3.zero;
                // rigid.angularVelocity = Vector3.zero;
                // rigid.constraints = RigidbodyConstraints.FreezeAll;
                // rigid.Sleep();
                // rigid.drag = 1000f;
            }
        }

        public void WakeUp()
        {
            _print("wake");
            if (rigid)
            {
                // rigid.constraints = RigidbodyConstraints.None;
                // rigid.WakeUp();
                // rigid.drag = 0f;
            }
        }

        float lastOwnershipChange = -1001f;
        public void TakeOwnership()
        {
            if (Networking.IsClogged)
            {
                //Let them cook
                return;
            }
            _print("TakeOwnership");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public override void OnPickup()
        {
            TakeOwnership();
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
            if (!Utilities.IsValid(owner) || !owner.isLocal)
            {
                return;
            }
            //if we're attaching it to us, leave it attached
            state = state < 0 || state == STATE_ATTACHED_TO_PLAYSPACE ? state : STATE_LERPING;
        }

        public bool IsAttachedToPlayer()
        {
            return !(state >= STATE_SLEEPING && state <= STATE_FALLING);
        }
        public bool LastStateIsAttachedToPlayer()
        {
            return !(state >= STATE_SLEEPING && state <= STATE_FALLING);
        }



        public void CalcLerpProgress()
        {
            if (lastSync <= 0 || lerpTime <= 0 || (Time.timeSinceLevelLoad - lastSync) >= lerpTime || state == STATE_TELEPORTING)
            {
                lerpProgress = 1.0f;
            }
            else
            {
                lerpProgress = (Time.timeSinceLevelLoad - lastSync) / lerpTime;
            }
        }

        bool trackingMissing = false;
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
            lerpCache = Mathf.Clamp01(lerpProgress);
            if (lerpCache < 1.0)
            {
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
                            endVel = (rigid != null && rigid.useGravity) ? velOnSync + Physics.gravity * lerpTime : startVel;
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
                
                posControl1 = (parentPosCache + parentRotCache * posOnSync) + startVel * lerpTime * lerpProgress / 3f;
                posControl2 = (parentPosCache + parentRotCache * pos) - endVel * lerpTime * (1 - lerpProgress) / 3f;

                rotControl1 = (parentRotCache * rotOnSync) * Quaternion.Euler(startSpin * lerpTime * lerpProgress / 3f);
                rotControl2 = (parentRotCache * rot) * Quaternion.Euler(-1 * endSpin * lerpTime * (1 - lerpProgress) / 3f);

                transform.position = Vector3.Lerp(posControl1, posControl2, lerpCache);
                transform.rotation = Quaternion.Slerp(rotControl1, rotControl2, lerpCache);
            }
            else
            {
                MoveToSyncedTransform();
            }
        }

        SmartObjectSync otherSync;
        public void OnCollisionEnter(Collision other)
        {
            if (!Utilities.IsValid(owner) || rigid == null || rigid.isKinematic)
            {
                //we are bound to get collision events before the first network event
                //we want local physics to take over in this situation so we return early
                return;
            }
            // if (!owner.isLocal)
            // {
            //     if (lastSync < 0)
            //     {
            //         return;
            //     }
            //     if (state == STATE_SLEEPING && helper && rigid && rigid.IsSleeping())
            //     {
            //         //restart the lerp to the sleeping position to ensure we don't get knocked out of sync with what's been synced
            //         CalcParentTransform();
            //         posOnSync = CalcPos();
            //         rotOnSync = CalcRot();
            //         velOnSync = CalcVel();
            //         spinOnSync = CalcSpin();
            //         lastSync = Time.timeSinceLevelLoad;
            //         // if (Vector3.Distance(posOnSync, pos) > 0.1f)//0.1 is an arbitrary small distance
            //         // {
            //         //     _print("desync detected - requesting resync");
            //         //     //object got knocked out of place
            //         //     SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(Synchronize));
            //         // }
            //     }
            //     return;
            // }

            if (!owner.isLocal)
            {
                return;
            }
            if (!IsAttachedToPlayer())
            {
                state = STATE_LERPING;
            }


            if (!takeOwnershipOfOtherObjectsOnCollision || other == null || other.collider == null)
            {
                return;
            }
            otherSync = other.collider.GetComponent<SmartObjectSync>();
            if (otherSync && otherSync.allowOthersToTakeOwnershipOnCollision && !otherSync.IsAttachedToPlayer() && otherSync.rigid && (IsAttachedToPlayer() || otherSync.state == STATE_SLEEPING || !otherSync.takeOwnershipOfOtherObjectsOnCollision || otherSync.rigid.velocity.sqrMagnitude < rigid.velocity.sqrMagnitude))
            {
                _printErr("taking ownership of " + otherSync.name);
                otherSync.TakeOwnership();
                otherSync.RequestSerialization();
                // otherSync.owner = owner;
                // otherSync.state = STATE_LERPING;
                // otherSync.WakeUp();
            }
        }
        public void OnCollisionExit(Collision other)
        {
            if (!Utilities.IsValid(owner) || rigid == null || rigid.isKinematic)
            {
                //we are bound to get collision events before the first network event
                //we want local physics to take over in this situation so we return early
                return;
            }
            // if (!owner.isLocal)
            // {
            //     if (lastSync < 0)
            //     {
            //         return;
            //     }
            //     if (state == STATE_SLEEPING && helper && rigid)
            //     {
            //         //restart the lerp to the sleeping position to ensure we don't get knocked out of sync with what's been synced
            //         CalcParentTransform();
            //         posOnSync = CalcPos();
            //         rotOnSync = CalcRot();
            //         velOnSync = CalcVel();
            //         spinOnSync = CalcSpin();
            //         lastSync = Time.timeSinceLevelLoad;
            //     }
            //     return;
            // }
            if (!IsAttachedToPlayer())
            {
                state = STATE_FALLING;
            }
        }
    }
}