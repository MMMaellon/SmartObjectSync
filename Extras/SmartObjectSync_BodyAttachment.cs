
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
    [CustomEditor(typeof(SmartObjectSync_BodyAttachment))]

    public class SmartObjectSync_BodyAttachmentEditor : Editor
    {
        SerializedProperty _allowedBones;
        SerializedProperty allowedBones;
        SerializedProperty allowAttachOnPickupUseDown;
        SerializedProperty allowAttachOnRightStickDown;
        SerializedProperty allowAttachToSelf;
        SerializedProperty allowAttachToOthers;

        void OnEnable()
        {
            // Fetch the objects from the MyScript script to display in the inspector
            _allowedBones = serializedObject.FindProperty("_allowedBones");
            allowedBones = serializedObject.FindProperty("allowedBones");
            allowAttachOnPickupUseDown = serializedObject.FindProperty("allowAttachOnPickupUseDown");
            allowAttachOnRightStickDown = serializedObject.FindProperty("allowAttachOnRightStickDown");
            allowAttachToSelf = serializedObject.FindProperty("allowAttachToSelf");
            allowAttachToOthers = serializedObject.FindProperty("allowAttachToOthers");
            SyncAllowedBones();
        }
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(allowAttachToSelf, true);
            EditorGUILayout.PropertyField(allowAttachToOthers, true);
            EditorGUILayout.PropertyField(allowAttachOnPickupUseDown, true);
            EditorGUILayout.PropertyField(allowAttachOnRightStickDown, true);
            EditorGUILayout.PropertyField(_allowedBones, true);
            if (EditorGUI.EndChangeCheck())
            {
                SyncAllowedBones();
            }

            
            // EditorGUILayout.Space();
            // base.OnInspectorGUI();
        }

        public void SyncAllowedBones()
        {
            allowedBones.ClearArray();
            for (int i = 0; i < _allowedBones.arraySize; i++)
            {
                allowedBones.InsertArrayElementAtIndex(i);
                allowedBones.GetArrayElementAtIndex(i).intValue = _allowedBones.GetArrayElementAtIndex(i).enumValueIndex;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif

namespace MMMaellon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SmartObjectSync_BodyAttachment : UdonSharpBehaviour
    {

        public bool allowAttachOnPickupUseDown = true;
        public bool allowAttachOnRightStickDown = false;
        
        [UdonSynced(UdonSyncMode.None)]
        public int playerId = 0;

        [UdonSynced(UdonSyncMode.None)]
        public int bone = -1001;

        [UdonSynced(UdonSyncMode.None)]
        public Vector3 localPosition = Vector3.zero;

        [UdonSynced(UdonSyncMode.None)]
        public Quaternion localRotation = Quaternion.identity;

        public bool allowAttachToSelf = true;
        public bool allowAttachToOthers = true;
        public int[] allowedBones = { 0 };

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        //For displaying in the editor only
        public HumanBodyBones[] _allowedBones = { 0 };
#endif
        VRCPlayerApi localPlayer;
        SmartObjectSync sync;
        void Start()
        {
            localPlayer = Networking.LocalPlayer;
            sync = GetComponent<SmartObjectSync>();
        }

        public override void OnPickup()
        {
            base.OnPickup();
            bone = -1001;
            playerId = -1001;
        }

        public override void OnPickupUseDown()
        {
            base.OnPickupUseDown();
            if (allowAttachOnPickupUseDown)
            {
                Attach();
            }
        }

        public void Attach()
        {
            sync._print("Attach");
            if (!sync.IsLocalOwner())
            {
                Networking.SetOwner(localPlayer, gameObject);
            }

            if (allowAttachToOthers)
            {
                VRCPlayerApi[] nearbyPlayers = GetNearbyPlayers(3);//arbitrarily pick the closest 3 players to compare against
                VRCPlayerApi closestPlayer = null;
                bone = GetClosestBoneInGroup(nearbyPlayers, ref closestPlayer);
                playerId = closestPlayer.playerId;

                Vector3 bonePos = closestPlayer.GetBonePosition((HumanBodyBones) (bone));
                Quaternion boneRot = closestPlayer.GetBoneRotation((HumanBodyBones) (bone));
                localPosition = Quaternion.Inverse(boneRot) * (transform.position - bonePos);
                localRotation = Quaternion.Inverse(boneRot) * (transform.rotation);

                if (playerId == localPlayer.playerId)
                {
                    ApplyAttachment();
                } else
                {
                    sync.state = SmartObjectSync.STATE_WORLD_LOCK;
                    RequestSerialization();
                }
            } else if (allowAttachToSelf)
            {
                bone = GetClosestBone(localPlayer);
                ApplyAttachment();
            }
        }

        public VRCPlayerApi[] GetNearbyPlayers(int count)
        {
            VRCPlayerApi[] nearby = new VRCPlayerApi[count];
            float[] nearbyDist = new float[count];
            VRCPlayerApi[] allPlayers = new VRCPlayerApi[82];
            VRCPlayerApi.GetPlayers(allPlayers);
            float dist;
            foreach (VRCPlayerApi player in allPlayers)
            {
                if (!Utilities.IsValid(player) || (player.isLocal && !allowAttachToSelf)) {
                    continue;
                }
                dist = Vector3.Distance(transform.position, player.GetPosition());
                if (nearby[0] == null || nearbyDist[0] == 0 || nearbyDist[0] > dist)
                {
                    nearby[2] = nearby[1];
                    nearby[1] = nearby[0];
                    nearby[0] = player;
                    nearbyDist[2] = nearbyDist[1];
                    nearbyDist[1] = nearbyDist[0];
                    nearbyDist[0] = dist;
                }
                else if (nearby[1] == null || nearbyDist[1] == 0 || nearbyDist[1] > dist)
                {
                    nearby[2] = nearby[1];
                    nearby[1] = player;
                    nearbyDist[2] = nearbyDist[1];
                    nearbyDist[1] = dist;
                }
                else if (nearby[2] == null || nearbyDist[2] == 0 || nearbyDist[2] > dist)
                {
                    nearby[2] = player;
                    nearbyDist[2] = dist;
                }
            }
            return nearby;
        }

        public void ApplyAttachment()
        {
            sync._print("ApplyAttachment bone is " + bone);
            Vector3 bonePos = localPlayer.GetBonePosition((HumanBodyBones)(bone));
            Quaternion boneRot = localPlayer.GetBoneRotation((HumanBodyBones)(bone));
            transform.position = bonePos + boneRot * localPosition;
            transform.rotation = boneRot * localRotation;
            if (bone >= 0)
            {
                sync.state = -1 - bone;
            }
            bone = -1001;
            playerId = -1001;
            RequestSerialization();
        }

        public int GetClosestBone(VRCPlayerApi player)
        {
            if (player == null || !player.IsValid())
            {
                return -1001;
            }
            int closestBone = -1001;
            float closestDistance = -1001f;
            foreach (int i in allowedBones)
            {
                Vector3 bonePos = FindBoneCenter((HumanBodyBones)i, player);
                float boneDist = Vector3.Distance(bonePos, transform.position);
                if (bonePos != Vector3.zero && (closestDistance < 0 || closestDistance > boneDist))
                {
                    closestBone = i;
                    closestDistance = boneDist;
                }
            }
            return closestBone;
        }
        public int GetClosestBoneInGroup(VRCPlayerApi[] players, ref VRCPlayerApi closestPlayer)
        {
            int closestBone = -1001;
            float closestDistance = -1001f;
            foreach (VRCPlayerApi player in players)
            {
                if (!Utilities.IsValid(player)){
                    continue;
                }
                foreach (int i in allowedBones)
                {
                    Vector3 bonePos = FindBoneCenter((HumanBodyBones)i, player);
                    float boneDist = Vector3.Distance(bonePos, transform.position);
                    if (bonePos != Vector3.zero && (closestDistance < 0 || closestDistance > boneDist))
                    {
                        closestBone = i;
                        closestDistance = boneDist;
                        closestPlayer = player;
                    }
                }
            }
            return closestBone;
        }

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            base.InputLookVertical(value, args);
            if (allowAttachOnRightStickDown && sync && sync.pickup && sync.pickup.IsHeld && localPlayer.IsUserInVR() && value < -0.95f)
            {
                Attach();
            }
        }

        public Vector3 FindBoneCenter(HumanBodyBones humanBodyBone, VRCPlayerApi player)
        {
            Vector3 bonePos = player.GetBonePosition(humanBodyBone);
            switch (humanBodyBone)
            {
                case (HumanBodyBones.LeftUpperLeg):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.LeftLowerLeg), 0.5f);
                        break;
                    }
                case (HumanBodyBones.RightUpperLeg):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.RightLowerLeg), 0.5f);
                        break;
                    }
                case (HumanBodyBones.LeftLowerLeg):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.LeftFoot), 0.5f);
                        break;
                    }
                case (HumanBodyBones.RightLowerLeg):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.RightFoot), 0.5f);
                        break;
                    }
                case (HumanBodyBones.LeftUpperArm):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.LeftLowerArm), 0.5f);
                        break;
                    }
                case (HumanBodyBones.RightUpperArm):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.RightLowerArm), 0.5f);
                        break;
                    }
                case (HumanBodyBones.LeftLowerArm):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.LeftHand), 0.5f);
                        break;
                    }
                case (HumanBodyBones.RightLowerArm):
                    {
                        bonePos = Vector3.Lerp(bonePos, player.GetBonePosition(HumanBodyBones.RightHand), 0.5f);
                        break;
                    }
            }
            return bonePos;
        }

        public override void OnDeserialization()
        {
            if (playerId == localPlayer.playerId)
            {
                sync._print("body attachment deserialization");
                Networking.SetOwner(localPlayer, gameObject);
                ApplyAttachment();
            }
        }
    }
}
