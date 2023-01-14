
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

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
        public static void SetupSelectedSmartObjectSync(SmartObjectSync sync)
        {
            if (sync)
            {
                SerializedObject serializedSync = new SerializedObject(sync);
                serializedSync.FindProperty("pickup").objectReferenceValue = sync.GetComponent<VRC_Pickup>();
                serializedSync.FindProperty("rigid").objectReferenceValue = sync.GetComponent<Rigidbody>();
                foreach (SmartObjectSyncHelper tempHelper in sync.GetComponents<SmartObjectSyncHelper>())
                {
                    if (tempHelper)
                    {
                        DestroyImmediate(tempHelper);
                    }
                }
                //we need to create the helper
                sync.helper = UdonSharpComponentExtensions.AddUdonSharpComponent<SmartObjectSyncHelper>(sync.gameObject);
                SerializedObject serializedHelper = new SerializedObject(sync.helper);
                serializedHelper.FindProperty("sync").objectReferenceValue = sync;
                serializedHelper.ApplyModifiedProperties();
                serializedSync.FindProperty("helper").objectReferenceValue = sync.helper;
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
        public override void OnInspectorGUI()
        {
            SmartObjectSync sync = (SmartObjectSync)target;
            bool setupNeeded = false;
            if (sync)
            {
                if (sync.pickup == null && sync.GetComponent<VRC_Pickup>() != null)
                {
                    setupNeeded = true;
                    EditorGUILayout.HelpBox(@"Object has a VRC_Pickup component, but no VRC_Pickup was defined in SmartObjectSync", MessageType.Warning);
                }
                if (sync.rigid == null && sync.GetComponent<Rigidbody>() != null)
                {
                    setupNeeded = true;
                    EditorGUILayout.HelpBox(@"Object has a RigidBody component, but no Rigidbody was defined in SmartObjectSync", MessageType.Warning);
                }
                if (VRC_SceneDescriptor.Instance != null && VRC_SceneDescriptor.Instance.RespawnHeightY != sync.respawn_height)
                {
                    setupNeeded = true;
                    EditorGUILayout.HelpBox(@"Respawn Height is different from the scene's", MessageType.Info);
                }
                if (sync.helper == null)
                {
                    setupNeeded = true;
                    EditorGUILayout.HelpBox(@"Helper not set up", MessageType.Warning);
                }
            }
            if (setupNeeded && GUILayout.Button(new GUIContent("Auto Setup")))
            {
                SetupSelectedSmartObjectSync(sync);
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
        public bool takeOwnershipOnCollision = true;

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
        public int _state;



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
        //negative for attached to a human body bone where the absolute value is the int value of the HumanBodyBone enum
        
        
        
        [System.NonSerialized, FieldChangeCallback(nameof(lastSync))]
        public float _lastSync = -1001f;
        [System.NonSerialized]
        public float syncProgress = 0f;
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
                RequestSerialization();
            }
        }
        public Quaternion rot
        {
            get => _rot;
            set
            {
                _rot = value;
                RequestSerialization();
            }
        }
        public Vector3 vel
        {
            get => _vel;
            set
            {
                _vel = value;
                RequestSerialization();
            }
        }
        public Vector3 spin
        {
            get => _spin;
            set
            {
                _spin = value;
                RequestSerialization();
            }
        }
        public int state
        {
            get => _state;
            set
            {
                if (pickup != null && value != STATE_LEFT_HAND_HELD && value != STATE_RIGHT_HAND_HELD)
                {
                    pickup.Drop();
                }
                _state = value;

                if (owner != null && owner.isLocal)
                {
                    RequestSerialization();
                    CalcParentTransform();
                    if (state != STATE_TELEPORTING)
                    {
                        //if we're teleporting we set the transforms before we requested serialization
                        _pos = CalcPos();
                        _rot = CalcRot();
                        _vel = CalcVel();
                        _spin = CalcSpin();
                    }
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
                            _print("state: " + ((HumanBodyBones) (-value)).ToString());
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
                    _print("new owner " + (owner == null ? "null" : owner.displayName));
                    _owner = value;
                    if (_owner == null || !_owner.isLocal)
                    {
                        if (pickup)
                        {
                            pickup.Drop();
                        }
                    }
                    if (helper)
                    {
                        helper.enabled = true;
                    }
                }
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void Reset()
        {
            SmartObjectSyncEditor.SetupSelectedSmartObjectSync(this);
        }
#endif
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
            _owner = Networking.GetOwner(gameObject);
            if (owner != null && owner.isLocal)
            {
                state = STATE_SLEEPING;
                pos = transform.position;
                rot = transform.rotation;
            }
            if (helper)
            {
                helper.enabled = true;
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
            if (setSpawnFromLocalTransform && transform.parent != null)
            {
                TeleportTo(transform.parent.TransformPoint(spawnPos), transform.parent.rotation * spawnRot, Vector3.zero, Vector3.zero);
            }
            else
            {
                TeleportTo(spawnPos, spawnRot, Vector3.zero, Vector3.zero);
            }
        }

        public void TeleportTo(Vector3 newPos, Quaternion newRot, Vector3 newVel, Vector3 newSpin)
        {
            state = STATE_TELEPORTING;
            pos = newPos;
            rot = newRot;
            vel = newVel;
            spin = newSpin;
            CalcParentTransform();
            MoveToSyncedTransform();
        }

        [System.NonSerialized] public bool hasBones;
        [System.NonSerialized] public Vector3 parentPosCache;
        [System.NonSerialized] public Quaternion parentRotCache;
        [System.NonSerialized] public VRCPlayerApi.TrackingData trackingCache;
        public override void OnPreSerialization()
        {
            base.OnPreSerialization();
            _print("OnPreSerialization");
            //we set the last sync time here and in ondeserialization
            lastSync = Time.timeSinceLevelLoad;
            CalcParentTransform();
            if (state != STATE_TELEPORTING)
            {
                //if we're teleporting we set the transforms before we requested serialization
                _pos = CalcPos();
                _rot = CalcRot();
                _vel = CalcVel();
                _spin = CalcSpin();
            }
        }

        public override void OnDeserialization()
        {
            base.OnDeserialization();
            _print("OnDeserialization");
            CalcParentTransform();
            posOnSync = CalcPos();
            rotOnSync = CalcRot();
            velOnSync = CalcVel();
            spinOnSync = CalcSpin();
            lastSync = Time.timeSinceLevelLoad;
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

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            base.OnOwnershipTransferred(player);
            owner = player;
            if (IsAttachedToPlayer() && owner != null && owner.isLocal)
            {
                state = STATE_LERPING;
            }
        }

        public override void OnPickup()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            owner = Networking.LocalPlayer;
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
            if (owner == null || !owner.isLocal)
            {
                return;
            }
            
            //it takes a few frames for the rigid to take on a velocity, we'll hold off if it's not kinematic and wait for a velocity
            if (rigid)
            {
                rigid.WakeUp();
            }
            //if we're attaching it to us, leave it attached;
            state = state < 0 || state == STATE_ATTACHED_TO_PLAYSPACE ? state : STATE_LERPING;
        }

        public bool IsAttachedToPlayer()
        {
            return !(state >= STATE_SLEEPING && state <= STATE_FALLING);
        }



        public void CalcSyncProgress()
        {
            if (lastSync <= 0 || lerpTime <= 0 || (Time.timeSinceLevelLoad - lastSync) >= lerpTime || state == STATE_TELEPORTING)
            {
                syncProgress = 1.0f;
            }
            else
            {
                syncProgress = (Time.timeSinceLevelLoad - lastSync) / lerpTime;
            }
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
                        if (syncProgress >= 1)
                        {
                            //we have lerped all the way to the sleeping position
                            if (rigid)
                            {
                                rigid.velocity = Vector3.zero;
                                rigid.angularVelocity = Vector3.zero;
                                rigid.Sleep();
                            }
                        }
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
                        if (owner != null)
                        {
                            parentPosCache = owner.GetBonePosition(HumanBodyBones.LeftHand);
                            parentRotCache = owner.GetBoneRotation(HumanBodyBones.LeftHand);
                            trackingCache = owner.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
                            hasBones = parentPosCache != Vector3.zero;
                            parentPosCache = hasBones ? parentPosCache : trackingCache.position;
                            parentRotCache = hasBones ? parentRotCache : trackingCache.rotation;
                        }
                        break;
                    }
                case (STATE_RIGHT_HAND_HELD):
                    {
                        if (owner != null)
                        {
                            parentPosCache = owner.GetBonePosition(HumanBodyBones.RightHand);
                            parentRotCache = owner.GetBoneRotation(HumanBodyBones.RightHand);
                            trackingCache = owner.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
                            hasBones = parentPosCache != Vector3.zero;
                            parentPosCache = hasBones ? parentPosCache : trackingCache.position;
                            parentRotCache = hasBones ? parentRotCache : trackingCache.rotation;
                        }
                        break;
                    }
                case (STATE_ATTACHED_TO_PLAYSPACE):
                    {
                        if (owner != null)
                        {
                            parentPosCache = owner.GetPosition();
                            parentRotCache = owner.GetRotation();
                        }
                        break;
                    }
                default:
                    {
                        if (owner != null && state < STATE_SLEEPING)
                        {
                            parentPosCache = owner.GetBonePosition((HumanBodyBones)(-state));
                            parentRotCache = owner.GetBoneRotation((HumanBodyBones)(-state));
                        }
                        break;
                    }
            }
        }
        public void MoveToSyncedTransform()
        {
            transform.position = !IsAttachedToPlayer() ? pos : parentPosCache + parentRotCache * pos;
            transform.rotation = !IsAttachedToPlayer() ? rot : parentRotCache * rot;
            if (rigid)
            {
                rigid.WakeUp();
                rigid.velocity = vel;
                rigid.angularVelocity = spin;
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
            lerpCache = Mathf.Clamp01(syncProgress);
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
                            endVel = velOnSync + Physics.gravity * lerpTime;
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
                
                posControl1 = (parentPosCache + parentRotCache * posOnSync) + startVel * lerpTime * syncProgress / 3f;
                posControl2 = (parentPosCache + parentRotCache * pos) - endVel * lerpTime * (1 - syncProgress) / 3f;

                rotControl1 = (parentRotCache * rotOnSync) * Quaternion.Euler(startSpin * lerpTime * syncProgress / 3f);
                rotControl2 = (parentRotCache * rot) * Quaternion.Euler(-1 * endSpin * lerpTime * (1 - syncProgress) / 3f);

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
            if (owner == null || !owner.isLocal || rigid == null)
            {
                if (state == STATE_SLEEPING && helper && rigid)
                {
                    //restart the lerp to the sleeping position to ensure we don't get knocked out of sync with what's been synced
                    CalcParentTransform();
                    posOnSync = CalcPos();
                    rotOnSync = CalcRot();
                    velOnSync = CalcVel();
                    spinOnSync = CalcSpin();
                    lastSync = Time.timeSinceLevelLoad;
                }
                return;
            }
            
            if (!IsAttachedToPlayer())
            {
                state = STATE_LERPING;
            }

            //check if we need to take ownership
            if (!takeOwnershipOnCollision || other == null || other.collider == null)
            {
                return;
            }

            otherSync = other.collider.GetComponent<SmartObjectSync>();
            if (otherSync == null || otherSync.IsAttachedToPlayer() || otherSync.rigid == null || otherSync.rigid.velocity.sqrMagnitude > rigid.velocity.sqrMagnitude)
            {
                return;
            }
            Networking.SetOwner(owner, otherSync.gameObject);
        }
        public void OnCollisionExit(Collision other)
        {
            if (owner == null || !owner.isLocal || rigid == null)
            {
                if (state == STATE_SLEEPING && helper && rigid)
                {
                    //restart the lerp to the sleeping position to ensure we don't get knocked out of sync with what's been synced
                    CalcParentTransform();
                    posOnSync = CalcPos();
                    rotOnSync = CalcRot();
                    velOnSync = CalcVel();
                    spinOnSync = CalcSpin();
                    lastSync = Time.timeSinceLevelLoad;
                }
                return;
            }
            if (!IsAttachedToPlayer())
            {
                state = STATE_FALLING;
            }
        }
    }
}