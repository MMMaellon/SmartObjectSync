
namespace VRC.SDKBase.Validation
{
    public static partial class AvatarValidation
    {
        public static readonly string[] ComponentTypeWhiteListCommon = new string[]
        {
            #if UNITY_STANDALONE
            #if VRC_CLIENT
            "DynamicBone", // Deprecated, whitelisted in the client only for backwards compatibility
            "DynamicBoneCollider", // Deprecated, whitelisted in the client only for backwards compatibility
            #endif // VRC_CLIENT
            "RootMotion.FinalIK.IKExecutionOrder",
            "RootMotion.FinalIK.VRIK",
            "RootMotion.FinalIK.FullBodyBipedIK",
            "RootMotion.FinalIK.LimbIK",
            "RootMotion.FinalIK.AimIK",
            "RootMotion.FinalIK.BipedIK",
            "RootMotion.FinalIK.GrounderIK",
            "RootMotion.FinalIK.GrounderFBBIK",
            "RootMotion.FinalIK.GrounderVRIK",
            "RootMotion.FinalIK.GrounderQuadruped",
            "RootMotion.FinalIK.TwistRelaxer",
            "RootMotion.FinalIK.ShoulderRotator",
            "RootMotion.FinalIK.FBBIKArmBending",
            "RootMotion.FinalIK.FBBIKHeadEffector",
            "RootMotion.FinalIK.FABRIK",
            "RootMotion.FinalIK.FABRIKChain",
            "RootMotion.FinalIK.FABRIKRoot",
            "RootMotion.FinalIK.CCDIK",
            "RootMotion.FinalIK.RotationLimit",
            "RootMotion.FinalIK.RotationLimitHinge",
            "RootMotion.FinalIK.RotationLimitPolygonal",
            "RootMotion.FinalIK.RotationLimitSpline",
            "UnityEngine.Cloth",
            "UnityEngine.Light",
            "UnityEngine.BoxCollider",
            "UnityEngine.SphereCollider",
            "UnityEngine.CapsuleCollider",
            "UnityEngine.Rigidbody",
            "UnityEngine.Joint",
            "UnityEngine.Animations.AimConstraint",
            "UnityEngine.Animations.LookAtConstraint",
            "UnityEngine.Animations.ParentConstraint",
            "UnityEngine.Animations.PositionConstraint",
            "UnityEngine.Animations.RotationConstraint",
            "UnityEngine.Animations.ScaleConstraint",
            "UnityEngine.Camera",
            "UnityEngine.AudioSource",
            "ONSPAudioSource",
            #endif // UNITY_STANDALONE
            #if !VRC_CLIENT
            "VRC.Core.PipelineSaver",
            #endif
            "VRC.Core.PipelineManager",
            "UnityEngine.Transform",
            "UnityEngine.Animator",
            "UnityEngine.SkinnedMeshRenderer",
            "LimbIK", // our limbik based on Unity ik
            "LoadingAvatarTextureAnimation",
            "UnityEngine.MeshFilter",
            "UnityEngine.MeshRenderer",
            "UnityEngine.Animation",
            "UnityEngine.ParticleSystem",
            "UnityEngine.ParticleSystemRenderer",
            "UnityEngine.TrailRenderer",
            "UnityEngine.FlareLayer",
            "UnityEngine.GUILayer",
            "UnityEngine.LineRenderer",
            "RealisticEyeMovements.EyeAndHeadAnimator",
            "RealisticEyeMovements.LookTargetController",
        };
        
        public static readonly string[] ComponentTypeWhiteListSdk2 = new string[]
        {
            #if UNITY_STANDALONE
            "VRCSDK2.VRC_SpatialAudioSource",
            #endif
            "VRCSDK2.VRC_AvatarDescriptor",
            "VRCSDK2.VRC_AvatarVariations",
            "VRCSDK2.VRC_IKFollower",
            "VRCSDK2.VRC_Station",
        };

        public static readonly string[] ComponentTypeWhiteListSdk3 = new string[]
        {
            #if UNITY_STANDALONE
            "VRC.SDK3.Avatars.Components.VRCSpatialAudioSource",
            #endif
            "VRC.SDK3.VRCTestMarker",
            "VRC.SDK3.Avatars.Components.VRCAvatarDescriptor",
            "VRC.SDK3.Avatars.Components.VRCStation",
            "VRC.SDK3.Avatars.Components.VRCImpostorSettings",
            "VRC.SDK3.Avatars.Components.VRCImpostorEnvironment",
            "VRC.SDK3.Avatars.Components.VRCHeadChop",
            "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone",
            "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider",
            "VRC.SDK3.Dynamics.Constraint.Components.VRCAimConstraint",
            "VRC.SDK3.Dynamics.Constraint.Components.VRCLookAtConstraint",
            "VRC.SDK3.Dynamics.Constraint.Components.VRCParentConstraint",
            "VRC.SDK3.Dynamics.Constraint.Components.VRCPositionConstraint",
            "VRC.SDK3.Dynamics.Constraint.Components.VRCRotationConstraint",
            "VRC.SDK3.Dynamics.Constraint.Components.VRCScaleConstraint",
            "VRC.SDK3.Dynamics.Contact.Components.VRCContactSender",
            "VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver",
        };

        public static readonly string[] ShaderWhiteList = new string[]
        {
            "VRChat/Mobile/Standard Lite",
            "VRChat/Mobile/Diffuse",
            "VRChat/Mobile/Bumped Diffuse",
            "VRChat/Mobile/Bumped Mapped Specular",
            "VRChat/Mobile/Toon Lit",
            "VRChat/Mobile/MatCap Lit",

            "VRChat/Mobile/Particles/Additive",
            "VRChat/Mobile/Particles/Multiply",
        };

        public const int MAX_AVD_PHYSBONES_PER_AVATAR = 256;
        public const int MAX_AVD_COLLIDERS_PER_AVATAR = 256;
        public const int MAX_AVD_CONTACTS_PER_AVATAR = 256;
        public const int MAX_AVD_CONSTRAINTS_PER_AVATAR = 2000;
    }
}