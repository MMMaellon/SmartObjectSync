#define ENV_SET_INCLUDED_SHADERS

using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering;
using VRC.SDKBase.Validation.Performance.Stats;
using Object = UnityEngine.Object;

namespace VRC.Editor
{
    /// <summary>
    /// Setup up SDK env on editor launch
    /// </summary>
    [InitializeOnLoad]
    public class EnvConfig
    {
        private static readonly BuildTarget[] relevantBuildTargets =
        {
            BuildTarget.Android,
            BuildTarget.iOS,
            BuildTarget.StandaloneLinux64,
            BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneOSX
        };

        #if !VRC_CLIENT
    private static readonly BuildTarget[] allowedBuildtargets = {
        BuildTarget.StandaloneWindows64,
        BuildTarget.Android,
        BuildTarget.iOS,
    };
        #endif

        private static readonly Dictionary<BuildTarget, GraphicsDeviceType[]> allowedGraphicsAPIs = new Dictionary<BuildTarget, GraphicsDeviceType[]>()
        {
            { BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3, /* GraphicsDeviceType.Vulkan */ } },
            { BuildTarget.iOS, new[] { GraphicsDeviceType.Metal } },
            { BuildTarget.StandaloneLinux64, null },
            { BuildTarget.StandaloneWindows, new[] { GraphicsDeviceType.Direct3D11 } },
            { BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 } },
            { BuildTarget.StandaloneOSX, new[] { GraphicsDeviceType.Metal } }
        };

        #if ENV_SET_INCLUDED_SHADERS && VRC_CLIENT
        private static readonly string[] ensureTheseShadersAreAvailable =
        {
            "Hidden/CubeBlend",
            "Hidden/CubeBlur",
            "Hidden/CubeCopy",
            "Hidden/VideoDecode",
            "Legacy Shaders/Bumped Diffuse",
            "Legacy Shaders/Bumped Specular",
            "Legacy Shaders/Decal",
            "Legacy Shaders/Diffuse Detail",
            "Legacy Shaders/Diffuse Fast",
            "Legacy Shaders/Diffuse",
            "Legacy Shaders/Diffuse",
            "Legacy Shaders/Lightmapped/Diffuse",
            "Legacy Shaders/Lightmapped/Specular",
            "Legacy Shaders/Lightmapped/VertexLit",
            "Legacy Shaders/Parallax Diffuse",
            "Legacy Shaders/Parallax Specular",
            "Legacy Shaders/Reflective/Bumped Diffuse",
            "Legacy Shaders/Reflective/Bumped Specular",
            "Legacy Shaders/Reflective/Bumped Unlit",
            "Legacy Shaders/Reflective/Bumped VertexLit",
            "Legacy Shaders/Reflective/Diffuse",
            "Legacy Shaders/Reflective/Parallax Diffuse",
            "Legacy Shaders/Reflective/Parallax Specular",
            "Legacy Shaders/Reflective/Specular",
            "Legacy Shaders/Reflective/VertexLit",
            "Legacy Shaders/Self-Illumin/Bumped Diffuse",
            "Legacy Shaders/Self-Illumin/Bumped Specular",
            "Legacy Shaders/Self-Illumin/Diffuse",
            "Legacy Shaders/Self-Illumin/Parallax Diffuse",
            "Legacy Shaders/Self-Illumin/Parallax Specular",
            "Legacy Shaders/Self-Illumin/Specular",
            "Legacy Shaders/Self-Illumin/VertexLit",
            "Legacy Shaders/Specular",
            "Legacy Shaders/Transparent/Bumped Diffuse",
            "Legacy Shaders/Transparent/Bumped Specular",
            "Legacy Shaders/Transparent/Cutout/Bumped Diffuse",
            "Legacy Shaders/Transparent/Cutout/Bumped Specular",
            "Legacy Shaders/Transparent/Cutout/Diffuse",
            "Legacy Shaders/Transparent/Cutout/Soft Edge Unlit",
            "Legacy Shaders/Transparent/Cutout/Specular",
            "Legacy Shaders/Transparent/Cutout/VertexLit",
            "Legacy Shaders/Transparent/Diffuse",
            "Legacy Shaders/Transparent/Parallax Diffuse",
            "Legacy Shaders/Transparent/Parallax Specular",
            "Legacy Shaders/Transparent/Specular",
            "Legacy Shaders/Transparent/VertexLit",
            "Legacy Shaders/VertexLit",
            "Legacy Shaders/Particles/Additive",
            "Legacy Shaders/Particles/~Additive-Multiply",
            "Legacy Shaders/Particles/Additive (Soft)",
            "Legacy Shaders/Particles/Alpha Blended",
            "Legacy Shaders/Particles/Anim Alpha Blended",
            "Legacy Shaders/Particles/Multiply",
            "Legacy Shaders/Particles/Multiply (Double)",
            "Legacy Shaders/Particles/Alpha Blended Premultiply",
            "Legacy Shaders/Particles/VertexLit Blended",
            "Mobile/Particles/Additive",
            "Mobile/Particles/Alpha Blended",
            "Mobile/Particles/Multiply",
            "Mobile/Particles/VertexLit Blended",
            "Mobile/Skybox",
            "Nature/Terrain/Diffuse",
            "Nature/Terrain/Specular",
            "Nature/Terrain/Standard",
            "Particles/Additive (Soft)",
            "Particles/Additive",
            "Particles/Alpha Blended Premultiply",
            "Particles/Alpha Blended",
            "Particles/Anim Alpha Blended",
            "Particles/Multiply (Double)",
            "Particles/Multiply",
            "Particles/VertexLit Blended",
            "Particles/~Additive-Multiply",
            "Skybox/Cubemap",
            "Skybox/Procedural",
            "Skybox/6 Sided",
            "Sprites/Default",
            "Sprites/Diffuse",
            "UI/Default",
            "VRChat/UI/Unlit/WebPanelTransparent",
            "Toon/Lit",
            "Toon/Lit (Double)",
            "Toon/Lit Cutout",
            "Toon/Lit Cutout (Double)",
            "Toon/Lit Outline",
            "VRChat/Mobile/Diffuse",
            "Video/RealtimeEmissiveGamma",
            "VRChat/PC/Toon Lit",
            "VRChat/PC/Toon Lit (Double)",
            "VRChat/PC/Toon Lit Cutout",
            "VRChat/PC/Toon Lit Cutout (Double)",
            "Unlit/Color",
            "Unlit/Transparent",
            "Unlit/Transparent Cutout",
            "Unlit/Texture",
            "MatCap/Vertex/Textured Lit",
            "VRChat/Mobile/Bumped Uniform Diffuse",
            "VRChat/Mobile/Bumped Uniform Specular",
            "VRChat/Mobile/Toon Lit",
            "VRChat/Mobile/MatCap Lit",
            "VRChat/Mobile/Skybox",
            "VRChat/Mobile/Lightmapped",
            "VRChat/Mobile/Bumped Mapped Specular",
            "VRChat/Mobile/Diffuse",
            "VRChat/Mobile/Particles/Additive",
            "VRChat/Mobile/Particles/Multiply",
            "VRChat/Mobile/Standard Lite",
            "TextMeshPro/Distance Field (Surface)",
            "TextMeshPro/Mobile/Distance Field (No ZTest)",
            "TextMeshPro/Distance Field Overlay",
            "TextMeshPro/Sprite",
            "TextMeshPro/Mobile/Distance Field - Masking",
            "TextMeshPro/Mobile/Distance Field Overlay",
            "TextMeshPro/Mobile/Distance Field (Surface)",
            "TextMeshPro/Mobile/Distance Field",
            "TextMeshPro/Distance Field",
            "TextMeshPro/Bitmap Custom Atlas",
            "VRChat/UI/TextMeshPro/Mobile/Distance Field",
            "TextMeshPro/Mobile/Bitmap",
            "TextMeshPro/Bitmap",
            "TextMeshPro/Mobile/Distance Field - Masking (NoZTest)"
        };
        #endif

        private static bool _requestConfigureSettings = true;

        static EnvConfig()
        {
            EditorApplication.update += EditorUpdate;
        }

        private static void EditorUpdate()
        {
            try
            {
                if(!_requestConfigureSettings || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    _requestConfigureSettings = false;
                    return;
                }

                if(ConfigureSettings())
                {
                    _requestConfigureSettings = false;
                }
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                _requestConfigureSettings = false;
            }
        }

        private static void RequestConfigureSettings()
        {
            _requestConfigureSettings = true;
        }

        [UnityEditor.Callbacks.DidReloadScripts(int.MaxValue)]
        private static void DidReloadScripts()
        {
            RequestConfigureSettings();
        }

        private static bool ConfigureSettings()
        {
            CheckForFirstInit();

            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isUpdating)
            {
                return false;
            }

            ConfigurePlayerSettings();

            if(!VRC.Core.ConfigManager.RemoteConfig.IsInitialized())
            {
                VRC.Core.API.SetOnlineMode(true, "vrchat");
                VRC.Core.ConfigManager.RemoteConfig.Init();
            }

            LoadEditorResources();

            return true;
        }

        #if !VRC_CLIENT
    private static void SetDLLPlatforms(string dllName, bool active)
    {
        string[] assetGuids = AssetDatabase.FindAssets(dllName);

        foreach(string guid in assetGuids)
        {
            string dllPath = AssetDatabase.GUIDToAssetPath(guid);
            if(string.IsNullOrEmpty(dllPath) || dllPath.ToLower().EndsWith(".dll") == false)
            {
                return;
            }

            PluginImporter importer = AssetImporter.GetAtPath(dllPath) as PluginImporter;
            if(importer == null)
            {
                return;
            }

            bool allCorrect = true;
            if(importer.GetCompatibleWithAnyPlatform() != active)
            {
                allCorrect = false;
            }
            else
            {
                if(importer.GetCompatibleWithAnyPlatform())
                {
                    if(importer.GetExcludeEditorFromAnyPlatform() != !active ||
                       importer.GetExcludeFromAnyPlatform(BuildTarget.StandaloneWindows) != !active)
                    {
                        allCorrect = false;
                    }
                }
                else
                {
                    if(importer.GetCompatibleWithEditor() != active ||
                       importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows) != active)
                    {
                        allCorrect = false;
                    }
                }
            }

            if(allCorrect)
            {
                continue;
            }

            if(active)
            {
                importer.SetCompatibleWithAnyPlatform(true);
                importer.SetExcludeEditorFromAnyPlatform(false);
                importer.SetExcludeFromAnyPlatform(BuildTarget.Android, false);
                importer.SetExcludeFromAnyPlatform(BuildTarget.iOS, false);
                importer.SetExcludeFromAnyPlatform(BuildTarget.StandaloneWindows, false);
                importer.SetExcludeFromAnyPlatform(BuildTarget.StandaloneWindows64, false);
                importer.SetExcludeFromAnyPlatform(BuildTarget.StandaloneLinux64, false);
            }
            else
            {
                importer.SetCompatibleWithAnyPlatform(false);
                importer.SetCompatibleWithEditor(false);
                importer.SetCompatibleWithPlatform(BuildTarget.Android, false);
                importer.SetCompatibleWithPlatform(BuildTarget.iOS, false);
                importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
                importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
                importer.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, false);
            }

            importer.SaveAndReimport();
        }
    }
        #endif

        [MenuItem("VRChat SDK/Utilities/Force Configure Player Settings")]
        public static void ConfigurePlayerSettings()
        {
            VRC.Core.Logger.Log("Setting required PlayerSettings...", VRC.Core.DebugLevel.All);

            SetBuildTarget();

            // Needed for Microsoft.CSharp namespace in DLLMaker
            // Doesn't seem to work though
            if(PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup) != ApiCompatibilityLevel.NET_4_6)
            {
                PlayerSettings.SetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup, ApiCompatibilityLevel.NET_4_6);
            }

            if(!PlayerSettings.runInBackground)
            {
                PlayerSettings.runInBackground = true;
            }

            #if !VRC_CLIENT
        SetDLLPlatforms("VRCCore-Standalone", false);
        SetDLLPlatforms("VRCCore-Editor", true);
            #endif

            SetDefaultGraphicsAPIs();
            SetGraphicsSettings();
            SetQualitySettings();
            SetAudioSettings();
            SetPlayerSettings();

            #if VRC_CLIENT
                #if VRC_DISABLE_XR_MANAGEMENT
            PlatformSwitcher.RefreshRequiredPackages(EditorUserBuildSettings.selectedBuildTargetGroup, true);
                #else
            PlatformSwitcher.RefreshRequiredPackages(EditorUserBuildSettings.selectedBuildTargetGroup, false);

                    #if VRC_VR_OCULUS_QUEST
            VrChatBuildSettings.BuildConfiguration xrBuildConfiguration = VrChatBuildSettings.BuildConfiguration.AndroidOculusQuest;
            VrChatXRSettingsHelper.ConfigureXRSettings(EditorUserBuildSettings.selectedBuildTargetGroup, xrBuildConfiguration);
                    #elif VRC_MOBILE
            VrChatBuildSettings.BuildConfiguration xrBuildConfiguration = VrChatBuildSettings.BuildConfiguration.AndroidMonoscopic;
            VrChatXRSettingsHelper.ConfigureXRSettings(EditorUserBuildSettings.selectedBuildTargetGroup, xrBuildConfiguration);
                    #endif
                #endif
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            #else
            // SDK

            // default to steam runtime in sdk (shouldn't matter)
            SetVRSDKs(EditorUserBuildSettings.selectedBuildTargetGroup, new string[] { "None", "OpenVR", "Oculus" });

            VRC.Core.AnalyticsSDK.Initialize(VRC.Tools.SdkVersion);
            #endif

            #if VRC_CLIENT
            // VRCLog should handle disk writing
            PlayerSettings.usePlayerLog = false;
            foreach(LogType logType in Enum.GetValues(typeof(LogType)).Cast<LogType>())
            {
                switch(logType)
                {
                    case LogType.Assert:
                    case LogType.Exception:
                    {
                        PlayerSettings.SetStackTraceLogType(logType, StackTraceLogType.ScriptOnly);
                        break;
                    }
                    case LogType.Error:
                    case LogType.Warning:
                    case LogType.Log:
                    {
                        #if UNITY_EDITOR
                        PlayerSettings.SetStackTraceLogType(logType, StackTraceLogType.ScriptOnly);
                        #else
                    PlayerSettings.SetStackTraceLogType(logType, StackTraceLogType.None);
                        #endif
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }
            #endif
        }

        internal static void EnableBatching(bool enable)
        {
            PlayerSettings[] playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if(playerSettings == null)
            {
                return;
            }

            SerializedObject playerSettingsSerializedObject = new SerializedObject(playerSettings.Cast<Object>().ToArray());
            SerializedProperty batchingSettings = playerSettingsSerializedObject.FindProperty("m_BuildTargetBatching");
            if(batchingSettings == null)
            {
                return;
            }

            for(int i = 0; i < batchingSettings.arraySize; i++)
            {
                SerializedProperty batchingArrayValue = batchingSettings.GetArrayElementAtIndex(i);

                IEnumerator batchingEnumerator = batchingArrayValue?.GetEnumerator();
                if(batchingEnumerator == null)
                {
                    continue;
                }

                while(batchingEnumerator.MoveNext())
                {
                    SerializedProperty property = (SerializedProperty)batchingEnumerator.Current;

                    if(property != null && property.name == "m_BuildTarget")
                    {
                        // only change setting on "Standalone" entry
                        if(property.stringValue != "Standalone")
                        {
                            break;
                        }
                    }

                    if(property != null && property.name == "m_StaticBatching")
                    {
                        property.boolValue = enable;
                    }

                    if(property != null && property.name == "m_DynamicBatching")
                    {
                        property.boolValue = enable;
                    }
                }
            }

            playerSettingsSerializedObject.ApplyModifiedProperties();
        }

        public static void SetVRSDKs(BuildTargetGroup buildTargetGroup, string[] sdkNames)
        {
            VRC.Core.Logger.Log("Setting virtual reality SDKs in PlayerSettings: ", VRC.Core.DebugLevel.All);
            if(sdkNames != null)
            {
                foreach(string s in sdkNames)
                {
                    VRC.Core.Logger.Log("- " + s, VRC.Core.DebugLevel.All);
                }
            }

            if(!EditorApplication.isPlaying)
            {
#pragma warning disable 618
                PlayerSettings.SetVirtualRealitySDKs(buildTargetGroup, sdkNames);
#pragma warning restore 618
            }
        }

        private static void CheckForFirstInit()
        {
            bool firstLaunch = SessionState.GetBool("EnvConfigFirstLaunch", true);
            if(firstLaunch)
            {
                SessionState.SetBool("EnvConfigFirstLaunch", false);
            }
        }

        private static void SetDefaultGraphicsAPIs()
        {
            VRC.Core.Logger.Log("Setting Graphics APIs", VRC.Core.DebugLevel.All);
            foreach(BuildTarget target in relevantBuildTargets)
            {
                GraphicsDeviceType[] apis = allowedGraphicsAPIs[target];
                if(apis == null)
                {
                    SetGraphicsAPIs(target, true);
                }
                else
                {
                    SetGraphicsAPIs(target, false, apis);
                }
            }
        }

        private static void SetGraphicsAPIs(BuildTarget platform, bool auto, GraphicsDeviceType[] allowedTypes = null)
        {
            try
            {
                if(auto != PlayerSettings.GetUseDefaultGraphicsAPIs(platform))
                {
                    PlayerSettings.SetUseDefaultGraphicsAPIs(platform, auto);
                }
            }
            catch
            {
                // ignored
            }

            try
            {
                if(allowedTypes == null || allowedTypes.Length == 0)
                {
                    return;
                }

                GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(platform);
                if(graphicsAPIs == null || graphicsAPIs.Length == 0)
                {
                    return;
                }

                if(allowedTypes.SequenceEqual(graphicsAPIs))
                {
                    return;
                }

                PlayerSettings.SetGraphicsAPIs(platform, allowedTypes);
            }
            catch
            {
                // ignored
            }
        }

        internal static void SetQualitySettings()
        {
            VRC.Core.Logger.Log("Setting Graphics Settings", VRC.Core.DebugLevel.All);
            const string qualitySettingsAssetPath = "ProjectSettings/QualitySettings.asset";
            SerializedObject qualitySettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(qualitySettingsAssetPath)[0]);

            SerializedProperty qualitySettingsPresets = qualitySettings.FindProperty("m_QualitySettings");
            qualitySettingsPresets.arraySize = _graphicsPresets.Length;

            bool changedProperty = false;
            for(int index = 0; index < _graphicsPresets.Length; index++)
            {
                SerializedProperty currentQualityLevel = qualitySettingsPresets.GetArrayElementAtIndex(index);
                Dictionary<string, object> graphicsPreset = _graphicsPresets[index];
                foreach(KeyValuePair<string, object> setting in graphicsPreset)
                {
                    SerializedProperty property = currentQualityLevel.FindPropertyRelative(setting.Key);
                    if(property == null)
                    {
                        Debug.LogWarning($"Serialized property for quality setting '{setting.Key}' could not be found.");
                        continue;
                    }

                    object settingValue = setting.Value;
                    #if !VRC_CLIENT
                if(setting.Key == "name")
                {
                    settingValue = $"VRC {setting.Value}";
                }
                    #endif

                    switch(settingValue)
                    {
                        case null:
                        {
                            if(property.objectReferenceValue == setting.Value as Object)
                            {
                                continue;
                            }

                            property.objectReferenceValue = null;
                            break;
                        }
                        case string settingAsString:
                        {
                            if(property.stringValue == settingAsString)
                            {
                                continue;
                            }

                            property.stringValue = settingAsString;
                            break;
                        }
                        case bool settingAsBool:
                        {
                            if(property.boolValue == settingAsBool)
                            {
                                continue;
                            }

                            property.boolValue = settingAsBool;
                            break;
                        }
                        case int settingAsInt:
                        {
                            if(property.intValue == settingAsInt)
                            {
                                continue;
                            }

                            property.intValue = settingAsInt;
                            break;
                        }
                        case float settingAsFloat:
                        {
                            if(Mathf.Approximately(property.floatValue, settingAsFloat))
                            {
                                continue;
                            }

                            property.floatValue = settingAsFloat;
                            break;
                        }
                        case double settingAsDouble:
                        {
                            if(Mathf.Approximately((float)property.doubleValue, (float)settingAsDouble))
                            {
                                continue;
                            }

                            property.doubleValue = settingAsDouble;
                            break;
                        }
                        case Vector3 settingAsVector3:
                        {
                            if(property.vector3Value == settingAsVector3)
                            {
                                continue;
                            }

                            property.vector3Value = settingAsVector3;
                            break;
                        }
                        case string[] settingAsStringArray:
                        {
                            property.arraySize = settingAsStringArray.Length;

                            bool changedArrayEntry = false;
                            for(int settingIndex = 0; settingIndex < settingAsStringArray.Length; settingIndex++)
                            {
                                SerializedProperty entry = property.GetArrayElementAtIndex(settingIndex);
                                if(entry.stringValue == settingAsStringArray[settingIndex])
                                {
                                    continue;
                                }

                                entry.stringValue = settingAsStringArray[settingIndex];
                                changedArrayEntry = true;
                            }

                            if(!changedArrayEntry)
                            {
                                continue;
                            }

                            break;
                        }
                    }

                    #if !VRC_CLIENT
                string levelName = _graphicsPresets[index]["name"] as string;
                if(Application.isMobilePlatform)
                {
                    if(levelName == "Mobile")
                    {
                        Debug.Log($"Set incorrect quality setting '{setting.Key}' in level '{levelName}' to value '{setting.Value}'.");
                    }
                }
                else
                {
                    if(levelName != "Mobile")
                    {
                        Debug.Log($"Set incorrect quality setting '{setting.Key}' in level '{levelName}' to value '{setting.Value}'.");
                    }
                }

                    #endif
                    changedProperty = true;
                }
            }
            
            int defaultQuality = 
#if UNITY_ANDROID || UNITY_IOS 
                3;
#else
                2;
#endif
            
            SerializedProperty currentGraphicsQuality = qualitySettings.FindProperty("m_CurrentQuality");
            
            if(currentGraphicsQuality.intValue != defaultQuality)
            {
                currentGraphicsQuality.intValue = defaultQuality;
                changedProperty = true;
            }
            
            if(!changedProperty)
            {
                return;
            }

            #if !VRC_CLIENT
                Debug.Log($"A quality setting was changed resetting to the default quality: {_graphicsPresets[defaultQuality]["name"]}.");
            #endif

            qualitySettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        internal static void SetGraphicsSettings()
        {
            VRC.Core.Logger.Log("Setting Graphics Settings", VRC.Core.DebugLevel.All);

            const string graphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";
            SerializedObject graphicsManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(graphicsSettingsAssetPath)[0]);

            // We'll use this flag to determine if we need to save the asset.
            bool isDirty = false;

            //get the current value of the property
                //- don't touch it if it matches the new value
                //- otherwise set the property's value to the new value
                //- set isDirty

            SerializedProperty deferred = graphicsManager.FindProperty("m_Deferred.m_Mode");
            if (deferred.enumValueIndex != 1)
            {
                deferred.enumValueIndex = 1;
                isDirty = true;
            }

            SerializedProperty deferredReflections = graphicsManager.FindProperty("m_DeferredReflections.m_Mode");
            if (deferredReflections.enumValueIndex != 1)
            {
                deferredReflections.enumValueIndex = 1;
                isDirty = true;
            }

            SerializedProperty screenSpaceShadows = graphicsManager.FindProperty("m_ScreenSpaceShadows.m_Mode");
            if (screenSpaceShadows.enumValueIndex != 1)
            {
                screenSpaceShadows.enumValueIndex = 1;
                isDirty = true;
            }

            SerializedProperty legacyDeferred = graphicsManager.FindProperty("m_LegacyDeferred.m_Mode");
            if (legacyDeferred.enumValueIndex != 1)
            {
                legacyDeferred.enumValueIndex = 1;
                isDirty = true;
            }

            SerializedProperty depthNormals = graphicsManager.FindProperty("m_DepthNormals.m_Mode");
            if (depthNormals.enumValueIndex != 1)
            {
                depthNormals.enumValueIndex = 1;
                isDirty = true;
            }

            SerializedProperty motionVectors = graphicsManager.FindProperty("m_MotionVectors.m_Mode");
            if (motionVectors.enumValueIndex != 1)
            {
                motionVectors.enumValueIndex = 1;
                isDirty = true;
            }

            SerializedProperty lightHalo = graphicsManager.FindProperty("m_LightHalo.m_Mode");
            if (lightHalo.enumValueIndex != 1)
            {
                lightHalo.enumValueIndex = 1;
                isDirty = true;
            }

            SerializedProperty lensFlare = graphicsManager.FindProperty("m_LensFlare.m_Mode");
            if (lensFlare.enumValueIndex != 1)
            {
                lensFlare.enumValueIndex = 1;
                isDirty = true;
            }

            #if ENV_SET_INCLUDED_SHADERS && VRC_CLIENT
            // clear GraphicsSettings->Always Included Shaders - these cause a +5s app startup time increase on Quest.
            // include Shader objects as resources instead
            SerializedProperty alwaysIncluded = graphicsManager.FindProperty("m_AlwaysIncludedShaders");
            alwaysIncluded.arraySize = 0;

            #if ENV_SEARCH_FOR_SHADERS
            Resources.LoadAll("", typeof(Shader));
            System.Collections.Generic.List<Shader> foundShaders = Resources.FindObjectsOfTypeAll<Shader>()
                .Where(s => { string name = s.name.ToLower(); return 0 == (s.hideFlags & HideFlags.DontSave); })
                .GroupBy(s => s.name)
                .Select(g => g.First())
                .ToList();
            #else
            List<Shader> foundShaders = new List<Shader>();
            #endif

            foreach(string shader in ensureTheseShadersAreAvailable.OrderBy(s => s, StringComparer.Ordinal))
            {
                if(foundShaders.Any(s => s.name == shader))
                {
                    continue;
                }

                Shader namedShader = Shader.Find(shader);
                if(namedShader != null)
                {
                    foundShaders.Add(namedShader);
                }
            }

            foundShaders.Sort((s1, s2) => string.Compare(s1.name, s2.name, StringComparison.Ordinal));

            // populate Resources list of "always included shaders"
            ShaderAssetList alwaysIncludedShaders = AssetDatabase.LoadAssetAtPath<ShaderAssetList>("Assets/Resources/AlwaysIncludedShaders.asset");
            if(alwaysIncludedShaders == null)
            {
                alwaysIncludedShaders = ScriptableObject.CreateInstance<ShaderAssetList>();
                AssetDatabase.CreateAsset(alwaysIncludedShaders, "Assets/Resources/AlwaysIncludedShaders.asset");
            }

            bool anyMissing = false;
            HashSet<Shader> alwaysIncludeShadersHashSet = new HashSet<Shader>(alwaysIncludedShaders.Shaders);
            foundShaders.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            foreach(Shader foundShader in foundShaders)
            {
                if(alwaysIncludeShadersHashSet.Contains(foundShader))
                {
                    continue;
                }

                anyMissing = true;
                break;
            }

            if(anyMissing)
            {
                alwaysIncludedShaders.Shaders = foundShaders.ToArray();
                EditorUtility.SetDirty(alwaysIncludedShaders);
            }
            #else
            SerializedProperty alwaysIncluded = graphicsManager.FindProperty("m_AlwaysIncludedShaders");
            if (alwaysIncluded.arraySize != 0)
            {
                alwaysIncluded.arraySize = 0;
                isDirty = true;
            }
            #endif

            SerializedProperty preloaded = graphicsManager.FindProperty("m_PreloadedShaders");
            if (preloaded.arraySize != 0)
            {
                preloaded.ClearArray();
                preloaded.arraySize = 0;
                isDirty = true;
            }

            SerializedProperty spritesDefaultMaterial = graphicsManager.FindProperty("m_SpritesDefaultMaterial");
            if (spritesDefaultMaterial.objectReferenceValue == null || spritesDefaultMaterial.objectReferenceValue.name != "Sprites-Default")
            {
                spritesDefaultMaterial.objectReferenceValue = Shader.Find("Sprites/Default");
                isDirty = true;
            }

            SerializedProperty renderPipeline = graphicsManager.FindProperty("m_CustomRenderPipeline");
            if (renderPipeline.objectReferenceValue != null)
            {
                renderPipeline.objectReferenceValue = null;
                isDirty = true;
            }

            SerializedProperty transparencySortMode = graphicsManager.FindProperty("m_TransparencySortMode");
            if (transparencySortMode.enumValueIndex != 0)
            {
                transparencySortMode.enumValueIndex = 0;
                isDirty = true;
            }

            SerializedProperty transparencySortAxis = graphicsManager.FindProperty("m_TransparencySortAxis");
            if (transparencySortAxis.vector3Value != Vector3.forward)
            {
                transparencySortAxis.vector3Value = Vector3.forward;
                isDirty = true;
            }

            SerializedProperty defaultRenderingPath = graphicsManager.FindProperty("m_DefaultRenderingPath");
            if (defaultRenderingPath.intValue != 1)
            {
                defaultRenderingPath.intValue = 1;
                isDirty = true;
            }

            SerializedProperty defaultMobileRenderingPath = graphicsManager.FindProperty("m_DefaultMobileRenderingPath");
            if (defaultMobileRenderingPath.intValue != 1)
            {
                defaultMobileRenderingPath.intValue = 1;
                isDirty = true;
            }

            SerializedProperty tierSettings = graphicsManager.FindProperty("m_TierSettings");
            if (tierSettings.arraySize != 0)
            {
                tierSettings.ClearArray();
                tierSettings.arraySize = 0;
                isDirty = true;
            }

            #if ENV_SET_LIGHTMAP
            SerializedProperty lightmapStripping = graphicsManager.FindProperty("m_LightmapStripping");
            if (lightmapStripping.enumValueIndex != 1)
            {
                lightmapStripping.enumValueIndex = 1;
                isDirty = true;
            }

            SerializedProperty instancingStripping = graphicsManager.FindProperty("m_InstancingStripping");
            if (instancingStripping.enumValueIndex != 2)
            {
                instancingStripping.enumValueIndex = 2;
                isDirty = true;
            }

            SerializedProperty lightmapKeepPlain = graphicsManager.FindProperty("m_LightmapKeepPlain");
            if (lightmapKeepPlain.boolValue != true)
            {
                lightmapKeepPlain.boolValue = true;
                isDirty = true;
            }

            SerializedProperty lightmapKeepDirCombined = graphicsManager.FindProperty("m_LightmapKeepDirCombined");
            if (lightmapKeepDirCombined.boolValue != true)
            {
                lightmapKeepDirCombined.boolValue = true;
                isDirty = true;
            }

            SerializedProperty lightmapKeepDynamicPlain = graphicsManager.FindProperty("m_LightmapKeepDynamicPlain");
            if (lightmapKeepDynamicPlain.boolValue != true)
            {
                lightmapKeepDynamicPlain.boolValue = true;
                isDirty = true;
            }

            SerializedProperty lightmapKeepDynamicDirCombined = graphicsManager.FindProperty("m_LightmapKeepDynamicDirCombined");
            if (lightmapKeepDynamicDirCombined.boolValue != true)
            {
                lightmapKeepDynamicDirCombined.boolValue = true;
                isDirty = true;
            }

            SerializedProperty lightmapKeepShadowMask = graphicsManager.FindProperty("m_LightmapKeepShadowMask");
            if (lightmapKeepShadowMask.boolValue != true)
            {
                lightmapKeepShadowMask.boolValue = true;
                isDirty = true;
            }

            SerializedProperty lightmapKeepSubtractive = graphicsManager.FindProperty("m_LightmapKeepSubtractive");
            if (lightmapKeepSubtractive.boolValue != true)
            {
                lightmapKeepSubtractive.boolValue = true;
                isDirty = true;
            }
            #endif

            SerializedProperty albedoSwatchInfos = graphicsManager.FindProperty("m_AlbedoSwatchInfos");
            if (albedoSwatchInfos.arraySize != 0)
            {
                albedoSwatchInfos.ClearArray();
                albedoSwatchInfos.arraySize = 0;
                isDirty = true;
            }

            SerializedProperty lightsUseLinearIntensity = graphicsManager.FindProperty("m_LightsUseLinearIntensity");
            if (lightsUseLinearIntensity.boolValue != true)
            {
                lightsUseLinearIntensity.boolValue = true;
                isDirty = true;
            }

            SerializedProperty lightsUseColorTemperature = graphicsManager.FindProperty("m_LightsUseColorTemperature");
            if (lightsUseColorTemperature.boolValue != true)
            {
                lightsUseColorTemperature.boolValue = true;
                isDirty = true;
            }

            // if isDirty, apply the modified properties to the graphicsmanager
            if (isDirty)
            {
                graphicsManager.ApplyModifiedProperties();
            }
        }

        public static FogSettings GetFogSettings()
        {
            VRC.Core.Logger.Log("Force-enabling Fog", VRC.Core.DebugLevel.All);

            const string graphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";
            SerializedObject graphicsManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(graphicsSettingsAssetPath)[0]);


            SerializedProperty fogStrippingSerializedProperty = graphicsManager.FindProperty("m_FogStripping");
            FogSettings.FogStrippingMode fogStripping = (FogSettings.FogStrippingMode)fogStrippingSerializedProperty.enumValueIndex;

            SerializedProperty fogKeepLinearSerializedProperty = graphicsManager.FindProperty("m_FogKeepLinear");
            bool keepLinear = fogKeepLinearSerializedProperty.boolValue;

            SerializedProperty fogKeepExpSerializedProperty = graphicsManager.FindProperty("m_FogKeepExp");
            bool keepExp = fogKeepExpSerializedProperty.boolValue;

            SerializedProperty fogKeepExp2SerializedProperty = graphicsManager.FindProperty("m_FogKeepExp2");
            bool keepExp2 = fogKeepExp2SerializedProperty.boolValue;

            FogSettings fogSettings = new FogSettings(fogStripping, keepLinear, keepExp, keepExp2);
            return fogSettings;
        }

        public static void SetFogSettings(FogSettings fogSettings)
        {
            VRC.Core.Logger.Log("Force-enabling Fog", VRC.Core.DebugLevel.All);

            const string graphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";
            SerializedObject graphicsManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(graphicsSettingsAssetPath)[0]);

            SerializedProperty fogStripping = graphicsManager.FindProperty("m_FogStripping");
            fogStripping.enumValueIndex = (int)fogSettings.fogStrippingMode;

            SerializedProperty fogKeepLinear = graphicsManager.FindProperty("m_FogKeepLinear");
            fogKeepLinear.boolValue = fogSettings.keepLinear;

            SerializedProperty fogKeepExp = graphicsManager.FindProperty("m_FogKeepExp");
            fogKeepExp.boolValue = fogSettings.keepExp;

            SerializedProperty fogKeepExp2 = graphicsManager.FindProperty("m_FogKeepExp2");
            fogKeepExp2.boolValue = fogSettings.keepExp2;

            graphicsManager.ApplyModifiedProperties();
        }

        internal static void SetAudioSettings()
        {
            Object audioManager = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/AudioManager.asset");
            SerializedObject audioManagerSerializedObject = new SerializedObject(audioManager);
            audioManagerSerializedObject.Update();

            SerializedProperty sampleRateSerializedProperty = audioManagerSerializedObject.FindProperty("m_SampleRate");
            sampleRateSerializedProperty.intValue = 48000; // forcing 48k seems to avoid sample rate conversion problems

            SerializedProperty dspBufferSizeSerializedProperty = audioManagerSerializedObject.FindProperty("m_RequestedDSPBufferSize");
            dspBufferSizeSerializedProperty.intValue = 0;

            SerializedProperty defaultSpeakerModeSerializedProperty = audioManagerSerializedObject.FindProperty("Default Speaker Mode");
            defaultSpeakerModeSerializedProperty.intValue = 2; // 2 = Stereo

            SerializedProperty virtualVoiceCountSerializedProperty = audioManagerSerializedObject.FindProperty("m_VirtualVoiceCount");
            SerializedProperty realVoiceCountSerializedProperty = audioManagerSerializedObject.FindProperty("m_RealVoiceCount");
            if(EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.Android || EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.iOS)
            {
                virtualVoiceCountSerializedProperty.intValue = 32;
                realVoiceCountSerializedProperty.intValue = 24;
            }
            else
            {
                virtualVoiceCountSerializedProperty.intValue = 64;
                realVoiceCountSerializedProperty.intValue = 32;
            }

            audioManagerSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static void SetPlayerSettings()
        {
            List<string> il2CppArgs = new List<string>();
            List<string> compilerArgs = new List<string>();
            List<string> linkerArgs = new List<string>();

            // asset bundles MUST be built with settings that are compatible with VRC client
            #if VRC_OVERRIDE_COLORSPACE_GAMMA
                PlayerSettings.colorSpace = ColorSpace.Gamma;
            #else
                PlayerSettings.colorSpace = ColorSpace.Linear;
            #endif

            #if !VRC_CLIENT // In client rely on platform-switcher
                if (!EditorApplication.isPlaying)
                {
                #pragma warning disable 618
                    PlayerSettings.SetVirtualRealitySupported(EditorUserBuildSettings.selectedBuildTargetGroup, true);
                #pragma warning restore 618
                }
            #endif

            #if UNITY_STANDALONE && !VRC_DISABLE_STANDALONE_GRAPHICS_JOBS
            PlayerSettings.graphicsJobs = true;
            #elif (UNITY_ANDROID || UNITY_IOS) && VRC_ENABLE_MOBILE_GRAPHICS_JOBS
            PlayerSettings.graphicsJobs = true;
            #else
            PlayerSettings.graphicsJobs = false;
            #endif
            PlayerSettings.gpuSkinning = true;

            #if UNITY_2019_3_OR_NEWER
                PlayerSettings.gcIncremental = true;
            #endif

            #if VRC_VR_WAVE
                PlayerSettings.stereoRenderingPath = StereoRenderingPath.MultiPass;     // Need to use Multi-pass on Wave SDK otherwise mirrors break
            #else
                PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;
            #endif

            #if UNITY_2018_4_OR_NEWER && !UNITY_2019_3_OR_NEWER
                PlayerSettings.scriptingRuntimeVersion = ScriptingRuntimeVersion.Latest;
            #endif

            #if ENABLE_LTO_BUILD
                // On Desktop, LTO is enabled by Master builds automatically
                PlayerSettings.SetIl2CppCompilerConfiguration(EditorUserBuildSettings.selectedBuildTargetGroup, Il2CppCompilerConfiguration.Master);
            #else
                PlayerSettings.SetIl2CppCompilerConfiguration(EditorUserBuildSettings.selectedBuildTargetGroup, Il2CppCompilerConfiguration.Release);
            #endif

            #if UNITY_ANDROID
                PlayerSettings.Android.forceSDCardPermission = true; // Need access to SD card for saving images
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

                // #if !VRC_MOBILE
                    // Unique for each -march=native:
                    // Quest 1: -target-feature +v8a -fallow-half-arguments-and-returns
                    // Quest 2: -target-feature +v8.2a -fallow-half-arguments-and-returns
                    // Quest Pro: -target-feature +v8.2a

                    // TODO: Quest 1 will be deprecated as of October 2023, so we can change to -march=armv8.2-a
                    // compilerArgs.Add(-Xclang=-target-feature");
                    // compilerArgs.Add(-Xclang=+v8.2a");
                // #endif // !VRC_MOBILE

                #if ENABLE_LTO_BUILD
                    // On Android, LTO must be enabled manually
                    // These flags are provided by unity's default build configuration in addition to LTO
                    // Build flags must be provided at link-time, so we have to list them out manually
                    compilerArgs.Add("-flto=thin");
                    compilerArgs.Add("-ffp-contract=on");
                    compilerArgs.Add("-fno-signed-char");
                    compilerArgs.Add("-DNDEBUG");
                    compilerArgs.Add("-fexceptions");
                    compilerArgs.Add("-fno-limit-debug-info");
                    compilerArgs.Add("-fdata-sections");
                    compilerArgs.Add("-ffunction-sections");
                    compilerArgs.Add("-Wa,--noexecstack");
                    compilerArgs.Add("-fno-rtti");
                    compilerArgs.Add("-fno-strict-aliasing");
                    compilerArgs.Add("-fvisibility=hidden");
                    compilerArgs.Add("-fvisibility-inlines-hidden");
                    compilerArgs.Add("-fno-strict-overflow");
                    compilerArgs.Add("-fno-addrsig");
                    compilerArgs.Add("-fPIC");
                    compilerArgs.Add("-target aarch64-linux-android21");
                    compilerArgs.Add("-D__ANDROID_API__=21");
                    compilerArgs.Add("-march=armv8-a");
                    compilerArgs.Add("-Wno-unused-value");
                    // Aggressive dead code stripping
                    compilerArgs.Add("-Wl,--gc-sections");
                    compilerArgs.Add("-Wl,--as-needed");
                    // Generic optimizations
                    linkerArgs.Add("-lto-O2");
                    linkerArgs.AddRange(compilerArgs);
                    compilerArgs.Add("-Os");
                #endif // ENABLE_LTO_BUILD

                if(PlayerSettings.Android.targetArchitectures.HasFlag(AndroidArchitecture.ARM64))
                {
                    // Since we need different IL2CPP args we can't build ARM64 with other Architectures.
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                } else {
                    linkerArgs.Add("-long-plt");
                }

                #if UNITY_2019_3_OR_NEWER
                    #if VRC_MOBILE
                        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)33;
                        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
                        PlayerSettings.Android.optimizedFramePacing = false;
                    #else
                        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)32;
                        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
			PlayerSettings.Android.optimizedFramePacing = false;
                    #endif
                #else
                    PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
                #endif

                #if VRC_VR_OCULUS
                    #pragma warning disable CS0618
                        PlayerSettings.VROculus.v2Signing = true;
                    #pragma warning restore CS0618
                #endif
            #endif // UNITY_ANDROID

            #if UNITY_2021_3_OR_NEWER
                // IL2CPP struggles with generics in unity 2021 and will not generate some code the UdonBehaviour needs
                // A blog post is available for more information here:
                // https://blog.unity.com/engine-platform/il2cpp-full-generic-sharing-in-unity-2022-1-beta
                // increasing this setting will cause longer builds
                il2CppAdditionalArgs.Add("--generic-virtual-method-iterations=2");
            #endif

            il2CppArgs.Add($"--compiler-flags=\"{string.Join(" ", compilerArgs)}\"");
            il2CppArgs.Add($"--linker-flags=\"{string.Join(" ", linkerArgs)}\"");
            PlayerSettings.SetAdditionalIl2CppArgs(string.Join(" ", il2CppArgs));

            SetActiveSDKDefines();

            EnableBatching(true);
        }

        public static void SetActiveSDKDefines()
        {
            bool definesChanged = false;
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            List<string> defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';').ToList();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if(assemblies.Any(assembly => assembly.GetType("VRC.Udon.UdonBehaviour") != null))
            {
                if(!defines.Contains("UDON", StringComparer.OrdinalIgnoreCase))
                {
                    defines.Add("UDON");
                    definesChanged = true;
                }
            }
            else if(defines.Contains("UDON"))
            {
                defines.Remove("UDON");
            }

            if(VRCSdk3Analysis.IsSdkDllActive(VRCSdk3Analysis.SdkVersion.VRCSDK2))
            {
                if(!defines.Contains("VRC_SDK_VRCSDK2", StringComparer.OrdinalIgnoreCase))
                {
                    defines.Add("VRC_SDK_VRCSDK2");
                    definesChanged = true;
                }
            }
            else if(defines.Contains("VRC_SDK_VRCSDK2"))
            {
                defines.Remove("VRC_SDK_VRCSDK2");
            }

            if(VRCSdk3Analysis.IsSdkDllActive(VRCSdk3Analysis.SdkVersion.VRCSDK3))
            {
                if(!defines.Contains("VRC_SDK_VRCSDK3", StringComparer.OrdinalIgnoreCase))
                {
                    defines.Add("VRC_SDK_VRCSDK3");
                    definesChanged = true;
                }
            }
            else if(defines.Contains("VRC_SDK_VRCSDK3"))
            {
                defines.Remove("VRC_SDK_VRCSDK3");
            }

            if(definesChanged)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines.ToArray()));
            }
        }

        private static void SetBuildTarget()
        {
            #if !VRC_CLIENT
        VRC.Core.Logger.Log("Setting build target", VRC.Core.DebugLevel.All);

        BuildTarget target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;

        if (!allowedBuildtargets.Contains(target))
        {
            Debug.LogError("Target not supported, switching to one that is.");
            target = allowedBuildtargets[0];
            #pragma warning disable CS0618 // Type or member is obsolete
            EditorUserBuildSettings.SwitchActiveBuildTarget(target);
            #pragma warning restore CS0618 // Type or member is obsolete
        }
            #endif
        }

        private static void LoadEditorResources()
        {
            AvatarPerformanceStats.Initialize();
        }

        public readonly struct FogSettings
        {
            public enum FogStrippingMode
            {
                Automatic,
                Custom
            }

            public readonly FogStrippingMode fogStrippingMode;
            public readonly bool keepLinear;
            public readonly bool keepExp;
            public readonly bool keepExp2;

            public FogSettings(FogStrippingMode fogStrippingMode)
            {
                this.fogStrippingMode = fogStrippingMode;
                keepLinear = true;
                keepExp = true;
                keepExp2 = true;
            }

            public FogSettings(FogStrippingMode fogStrippingMode, bool keepLinear, bool keepExp, bool keepExp2)
            {
                this.fogStrippingMode = fogStrippingMode;
                this.keepLinear = keepLinear;
                this.keepExp = keepExp;
                this.keepExp2 = keepExp2;
            }
        }

        private static readonly Dictionary<string, object>[] _graphicsPresets =
        {
            new Dictionary<string, object>
            {
                { "name", "Low" },
                { "pixelLightCount", 4 },
                { "shadows", 2 },
                { "shadowResolution", 1 },
                { "shadowProjection", 1 },
                { "shadowCascades", 2 },
                { "shadowDistance", 75f },
                { "shadowNearPlaneOffset", 2f },
                { "shadowCascade2Split", 0.33333334 },
                { "shadowCascade4Split", new Vector3(0.06666667f, 0.19999999f, 0.46666664f) },
                { "shadowmaskMode", 0 },
                { "skinWeights", 4 },
                { "textureQuality", 0 },
                { "anisotropicTextures", 2 },
                { "antiAliasing", 0 },
                { "softParticles", true },
                { "softVegetation", true },
                { "realtimeReflectionProbes", true },
                { "billboardsFaceCameraPosition", true },
                { "vSyncCount", 0 },
                { "lodBias", 1f },
                { "maximumLODLevel", 0 },
                { "streamingMipmapsActive", false },
                { "streamingMipmapsAddAllCameras", true },
                { "streamingMipmapsMemoryBudget", 512f },
                { "streamingMipmapsRenderersPerFrame", 512 },
                { "streamingMipmapsMaxLevelReduction", 2 },
                { "streamingMipmapsMaxFileIORequests", 1024 },
                { "particleRaycastBudget", 1024 },
                { "asyncUploadTimeSlice", 2 },
                { "asyncUploadBufferSize", 64 },
                { "asyncUploadPersistentBuffer", true },
                { "resolutionScalingFixedDPIFactor", 1f },
                { "customRenderPipeline", null },
                { "excludedTargetPlatforms", new[] { "Android" } }
            },
            new Dictionary<string, object>
            {
                { "name", "Medium" },
                { "pixelLightCount", 4 },
                { "shadows", 2 },
                { "shadowResolution", 2 },
                { "shadowProjection", 1 },
                { "shadowCascades", 2 },
                { "shadowDistance", 75f },
                { "shadowNearPlaneOffset", 2f },
                { "shadowCascade2Split", 0.33333334 },
                { "shadowCascade4Split", new Vector3(0.06666667f, 0.19999999f, 0.46666664f) },
                { "shadowmaskMode", 0 },
                { "skinWeights", 4 },
                { "textureQuality", 0 },
                { "anisotropicTextures", 2 },
                { "antiAliasing", 2 },
                { "softParticles", true },
                { "softVegetation", true },
                { "realtimeReflectionProbes", true },
                { "billboardsFaceCameraPosition", true },
                { "vSyncCount", 0 },
                { "lodBias", 1.5f },
                { "maximumLODLevel", 0 },
                { "streamingMipmapsActive", false },
                { "streamingMipmapsAddAllCameras", true },
                { "streamingMipmapsMemoryBudget", 512f },
                { "streamingMipmapsRenderersPerFrame", 512 },
                { "streamingMipmapsMaxLevelReduction", 2 },
                { "streamingMipmapsMaxFileIORequests", 1024 },
                { "particleRaycastBudget", 2048 },
                { "asyncUploadTimeSlice", 2 },
                { "asyncUploadBufferSize", 64 },
                { "asyncUploadPersistentBuffer", true },
                { "resolutionScalingFixedDPIFactor", 1f },
                { "customRenderPipeline", null },
                { "excludedTargetPlatforms", new[] { "Android" } }
            },
            new Dictionary<string, object>
            {
                { "name", "High" },
                { "pixelLightCount", 8 },
                { "shadows", 2 },
                { "shadowResolution", 3 },
                { "shadowProjection", 1 },
                { "shadowCascades", 4 },
                { "shadowDistance", 150f },
                { "shadowNearPlaneOffset", 2f },
                { "shadowCascade2Split", 0.33333334 },
                { "shadowCascade4Split", new Vector3(0.06666667f, 0.19999999f, 0.46666664f) },
                { "shadowmaskMode", 0 },
                { "skinWeights", 4 },
                { "textureQuality", 0 },
                { "anisotropicTextures", 2 },
                { "antiAliasing", 4 },
                { "softParticles", true },
                { "softVegetation", true },
                { "realtimeReflectionProbes", true },
                { "billboardsFaceCameraPosition", true },
                { "vSyncCount", 0 },
                { "lodBias", 2f },
                { "maximumLODLevel", 0 },
                { "streamingMipmapsActive", false },
                { "streamingMipmapsAddAllCameras", true },
                { "streamingMipmapsMemoryBudget", 512f },
                { "streamingMipmapsRenderersPerFrame", 512 },
                { "streamingMipmapsMaxLevelReduction", 2 },
                { "streamingMipmapsMaxFileIORequests", 1024 },
                { "particleRaycastBudget", 4096 },
                { "asyncUploadTimeSlice", 2 },
                { "asyncUploadBufferSize", 128 },
                { "asyncUploadPersistentBuffer", true },
                { "resolutionScalingFixedDPIFactor", 1f },
                { "customRenderPipeline", null },
                { "excludedTargetPlatforms", new[] { "Android" } }
            },
            new Dictionary<string, object>
            {
                { "name", "Mobile" },
                { "pixelLightCount", 4 },
                { "shadows", 0 },
                { "shadowResolution", 1 },
                { "shadowProjection", 1 },
                { "shadowCascades", 1 },
                { "shadowDistance", 50f },
                { "shadowNearPlaneOffset", 2f },
                { "shadowCascade2Split", 0.33333334 },
                { "shadowCascade4Split", new Vector3(0.06666667f, 0.19999999f, 0.46666664f) },
                { "shadowmaskMode", 0 },
                { "skinWeights", 4 },
                { "textureQuality", 0 },
                { "anisotropicTextures", 2 },
                { "antiAliasing", 2 },
                { "softParticles", false },
                { "softVegetation", false },
                { "realtimeReflectionProbes", false },
                { "billboardsFaceCameraPosition", true },
                { "vSyncCount", 0 },
                { "lodBias", 2f },
                { "maximumLODLevel", 0 },
                { "streamingMipmapsActive", false },
                { "streamingMipmapsAddAllCameras", true },
                { "streamingMipmapsMemoryBudget", 512f },
                { "streamingMipmapsRenderersPerFrame", 512 },
                { "streamingMipmapsMaxLevelReduction", 2 },
                { "streamingMipmapsMaxFileIORequests", 1024 },
                { "particleRaycastBudget", 1024 },
                { "asyncUploadTimeSlice", 1 },
                { "asyncUploadBufferSize", 32 },
                { "asyncUploadPersistentBuffer", true },
                { "resolutionScalingFixedDPIFactor", 1f },
                { "customRenderPipeline", null },
                { "excludedTargetPlatforms", new[] { "Standalone" } }
            }
        };
    }
}
