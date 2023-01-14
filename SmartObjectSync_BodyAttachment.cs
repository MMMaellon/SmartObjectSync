
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

        void OnEnable()
        {
            // Fetch the objects from the MyScript script to display in the inspector
            _allowedBones = serializedObject.FindProperty("_allowedBones");
            allowedBones = serializedObject.FindProperty("allowedBones");
            allowAttachOnPickupUseDown = serializedObject.FindProperty("allowAttachOnPickupUseDown");
            allowAttachOnRightStickDown = serializedObject.FindProperty("allowAttachOnRightStickDown");
            SyncAllowedBones();
        }
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
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
        public int bone = -1001;

        public int[] allowedBones = { 0 };

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        //For displaying in the editor only
        public HumanBodyBones[] _allowedBones = { 0 };
#endif
        VRCPlayerApi localPlayer;
        public bool beingHeld;
        SmartObjectSync sync;
        void Start()
        {
            localPlayer = Networking.LocalPlayer;
            sync = GetComponent<SmartObjectSync>();
            enabled = false;
        }

        public override void OnPickup()
        {
            base.OnPickup();
            beingHeld = true;
            bone = -1001;
            enabled = true;
        }

        public override void OnPickupUseDown()
        {
            base.OnPickupUseDown();
            if (allowAttachOnPickupUseDown)
            {
                bone = GetClosestBone(localPlayer);
            }
        }

        public override void OnDrop()
        {
            base.OnDrop();
            beingHeld = false;
            if (bone >= 0)
            {
                sync.state = -1 - bone;
            }
            bone = -1001;
            enabled = false;
        }

        public int GetClosestBone(VRCPlayerApi player)
        {
            if (player == null || !player.IsValid())
            {
                return -1001;
            }
            int closestBone = -1001;
            float closestDistance = -1001f;
            foreach(int i in allowedBones)
            {
                Vector3 bonePos = FindBoneCenter((HumanBodyBones) i, player);
                float boneDist = Vector3.Distance(bonePos, transform.position);
                if (bonePos != Vector3.zero && (closestDistance < 0 || closestDistance > boneDist))
                {
                    closestBone = i;
                    closestDistance = boneDist;
                }
            }
            return closestBone;
        }

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            base.InputLookVertical(value, args);
            if (allowAttachOnRightStickDown && beingHeld && localPlayer.IsUserInVR() && value < -0.95f)
            {
                bone = GetClosestBone(localPlayer);
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
    }
}
