using UnityEditor;
using UnityEngine;
using VRC.Core;

/// This file handles the links inside VRChat SDK top bar menu

namespace VRC.SDKBase.Editor
{
    public static class VRCSdkControlPanelHelp
    {
        public const string AVATAR_OPTIMIZATION_TIPS_URL = "https://creators.vrchat.com/avatars/avatar-optimizing-tips";
        public const string AVATAR_RIG_REQUIREMENTS_URL = "https://creators.vrchat.com/avatars/rig-requirements";
        public const string AVATAR_WRITE_DEFAULTS_ON_STATES_URL = "https://creators.vrchat.com/avatars/#write-defaults-on-states";
        public const string AVATAR_CUSTOM_HEAD_CHOP_URL = "https://creators.vrchat.com/avatars/avatar-dynamics/vrc-headchop";
        
        [MenuItem("VRChat SDK/Help/Developer FAQ")]
        public static void ShowDeveloperFAQ()
        {
            if (!ConfigManager.RemoteConfig.IsInitialized())
            {
                ConfigManager.RemoteConfig.Init(() => ShowDeveloperFAQ());
                return;
            }

            Application.OpenURL(ConfigManager.RemoteConfig.GetString("sdkDeveloperFaqUrl"));
        }

        [MenuItem("VRChat SDK/Help/VRChat Discord")]
        public static void ShowVRChatDiscord()
        {
            if (!ConfigManager.RemoteConfig.IsInitialized())
            {
                ConfigManager.RemoteConfig.Init(() => ShowVRChatDiscord());
                return;
            }

            Application.OpenURL(ConfigManager.RemoteConfig.GetString("sdkDiscordUrl"));
        }

        [MenuItem("VRChat SDK/Help/Avatar Optimization Tips")]
        public static void ShowAvatarOptimizationTips()
        {
            if (!ConfigManager.RemoteConfig.IsInitialized())
            {
                ConfigManager.RemoteConfig.Init(() => ShowAvatarOptimizationTips());
                return;
            }

            Application.OpenURL(AVATAR_OPTIMIZATION_TIPS_URL);
        }

        [MenuItem("VRChat SDK/Help/Avatar Rig Requirements")]
        public static void ShowAvatarRigRequirements()
        {
            if (!ConfigManager.RemoteConfig.IsInitialized())
            {
                ConfigManager.RemoteConfig.Init(() => ShowAvatarRigRequirements());
                return;
            }

            Application.OpenURL(AVATAR_RIG_REQUIREMENTS_URL);
        }
    }
} 
