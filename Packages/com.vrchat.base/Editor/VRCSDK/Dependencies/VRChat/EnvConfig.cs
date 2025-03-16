#define ENV_SET_INCLUDED_SHADERS

using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.Rendering;
using UnityEngine.XR;
using UnityEngine.XR.Management;
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

        private static readonly BuildTarget[] allowedBuildtargets = {
            BuildTarget.StandaloneWindows64,
            BuildTarget.Android,
            BuildTarget.iOS,
        };

        private static readonly Dictionary<BuildTarget, GraphicsDeviceType[]> allowedGraphicsAPIs = new Dictionary<BuildTarget, GraphicsDeviceType[]>()
        {
            { BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3, /* GraphicsDeviceType.Vulkan */ } },
            { BuildTarget.iOS, new[] { GraphicsDeviceType.Metal } },
            { BuildTarget.StandaloneLinux64, null },
            { BuildTarget.StandaloneWindows, new[] { GraphicsDeviceType.Direct3D11 } },
            { BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 } },
            { BuildTarget.StandaloneOSX, new[] { GraphicsDeviceType.Metal } }
        };

        private struct SDKInfo
        {
            public string Name;
            public string LoaderType;
        }
        
        private static readonly List<SDKInfo> xrSDKs = new List<SDKInfo>
        {
            new SDKInfo { Name = "Oculus", LoaderType = "Unity.XR.Oculus.OculusLoader" },
            new SDKInfo { Name = "OpenVR", LoaderType = "Unity.XR.OpenVR.OpenVRLoader" },
            new SDKInfo { Name = "MockHMD", LoaderType = "Unity.XR.MockHMD.MockHMDLoader" },
            new SDKInfo { Name = "OpenXR", LoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader" },
        };
        
        private static readonly List<string> loadersThatNeedsRestart = new List<string>
        {
            "UnityEngine.XR.OpenXR.OpenXRLoader",
        };

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
                VRC.Core.API.SetOnlineMode(true);
                VRC.Core.ConfigManager.RemoteConfig.Init();
            }

            LoadEditorResources();

            return true;
        }

    private static void SetDLLPlatforms(string dllName, bool active, bool isPreloaded = false)
    {
        string[] assetGuids = AssetDatabase.FindAssets(dllName);
        foreach(string guid in assetGuids)
        {
            string dllPath = AssetDatabase.GUIDToAssetPath(guid);
            
            if(string.IsNullOrEmpty(dllPath) || dllPath.ToLower().EndsWith(".dll") == false)
            {
                continue;
            }

            PluginImporter importer = AssetImporter.GetAtPath(dllPath) as PluginImporter;
            if(importer == null)
            {
                continue;
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
                
                if(importer.isPreloaded != isPreloaded && isPreloaded)
                {
                    allCorrect = false;
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
                importer.isPreloaded = isPreloaded;
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
                importer.isPreloaded = isPreloaded;
            }

            importer.SaveAndReimport();
            return;
        }
    }

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

            SetDLLPlatforms("VRCCore-Standalone", false);
            SetDLLPlatforms("VRCCore-Editor", true);
            SetSpatializerPluginSettings();

            SetDefaultGraphicsAPIs();
            SetGraphicsSettings();
            SetQualitySettings();
            SetAudioSettings();
            SetPlayerSettings();
            SetVRSDKs(EditorUserBuildSettings.selectedBuildTargetGroup, new string[] { "None", "Oculus" });
            SetTextureSettings();
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
        
        public static void SetVRSDKs(BuildTargetGroup buildTargetGroup, [NotNull] string[] sdkNames)
        {
            if(sdkNames == null)
            {
                throw new ArgumentNullException(nameof(sdkNames));
            }

            VRC.Core.Logger.Log("Setting virtual reality SDKs in PlayerSettings: ", VRC.Core.DebugLevel.All);
            foreach(string s in sdkNames)
            {
                VRC.Core.Logger.Log("- " + s, VRC.Core.DebugLevel.All);
            }

            if(!EditorApplication.isPlaying)
            {

                var loadersToAssign = new List<string>();
                foreach (var sdkName in sdkNames)
                {
                    if (!sdkName.Equals("None"))
                    {
                        var sdkInfoIndex = xrSDKs.FindIndex(x => x.Name == sdkName);
                        if (sdkInfoIndex == -1)
                        {
                            VRC.Core.Logger.LogError($"No SDK info found for SDK name '{sdkName}'");
                        }
                        else
                        {
                            loadersToAssign.Add(xrSDKs[sdkInfoIndex].LoaderType);
                        }
                    }
                }
                
                var assignedLoaders = new bool[loadersToAssign.Count];
                var validLoaders = new bool[loadersToAssign.Count];
                Array.Fill(assignedLoaders, false);

                {
                    Type xrGeneralSettingsPerBuildTargetType = typeof(XRGeneralSettingsPerBuildTarget);
                    MethodInfo methodInfo = xrGeneralSettingsPerBuildTargetType.GetMethod("GetOrCreate",
                        BindingFlags.Static | BindingFlags.NonPublic);

                    XRGeneralSettingsPerBuildTarget settings = methodInfo?.Invoke(null, null) as XRGeneralSettingsPerBuildTarget;
                    if(settings == null)
                    {
                        return;
                    }

                    if(!settings.HasManagerSettingsForBuildTarget(buildTargetGroup))
                    {
                        settings.CreateDefaultManagerSettingsForBuildTarget(buildTargetGroup);
                    }
                }
                
                var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
                var pluginsSettings = buildTargetSettings != null ? buildTargetSettings.AssignedSettings : null;
                var packages = XRPackageMetadataStore.GetAllPackageMetadataForBuildTarget(buildTargetGroup);
                foreach (var package in packages)
                {
                    foreach (var loader in package.metadata.loaderMetadata)
                    {
                        int loaderIndex = loadersToAssign.IndexOf(loader.loaderType);
                        if (loaderIndex != -1)
                        {
                            assignedLoaders[loaderIndex] = true;
                            validLoaders[loaderIndex] = true;
                            
                            if (loadersThatNeedsRestart.Contains(loader.loaderType) && 
                                !XRPackageMetadataStore.IsLoaderAssigned(loader.loaderType, buildTargetGroup))
                            {
                                // A loader change needs a reboot
                                RequestRestart(loader.loaderType);
                            }
                            
                            if (XRPackageMetadataStore.AssignLoader(pluginsSettings, loader.loaderType, buildTargetGroup))
                            {
                                VRC.Core.Logger.Log($"Assigned XR loader - {loader.loaderType} (buildTargetGroup: {buildTargetGroup})", VRC.Core.DebugLevel.All);
                            }
                        }
                        else
                        {
                            if (loadersThatNeedsRestart.Contains(loader.loaderType) && 
                                XRPackageMetadataStore.IsLoaderAssigned(loader.loaderType, buildTargetGroup))
                            {
                                // A loader change needs a reboot
                                RequestRestart(loader.loaderType);
                            }
                            
                            if (XRPackageMetadataStore.RemoveLoader(pluginsSettings, loader.loaderType, buildTargetGroup))
                            {
                                VRC.Core.Logger.Log($"Removed XR loader - {loader.loaderType} (buildTargetGroup: {buildTargetGroup})", VRC.Core.DebugLevel.All);
                            }
                        }
                    }
                }

                for  (int i = 0; i < assignedLoaders.Length; ++i)
                {
                    // Only create an error for loaders that are valid for the particular platform
                    if (!assignedLoaders[i] && validLoaders[i])
                    {
                        VRC.Core.Logger.LogError($"Failed to assign loader '{loadersToAssign[i]}'. Ensure the plugin is configured for the project correctly.");

                        // A loader fail could be from an xr package being added, a restart would help
                        RequestRestart(loadersToAssign[i]);
                    }
                }
            }
        }
        
        public static bool HasXRPackageSymlinked(bool requiresOpenXR)
        {
            ListRequest openXRListRequest = UnityEditor.PackageManager.Client.List(true);
            while (!openXRListRequest.IsCompleted)
            {
            }
            
            if (requiresOpenXR)
            {
                if (openXRListRequest.Result != null)
                {
                    if (openXRListRequest.Result.Any(x => x.name == "com.unity.xr.openxr"))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (openXRListRequest.Result != null)
                {
                    if (openXRListRequest.Result.Any(x => x.name == "com.unity.xr.oculus"))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        private static void SetTextureSettings()
        {
            // We only force-apply this setting outside of mobile targets to avoid a sudden texture reimport without user consent
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                EditorUserBuildSettings.androidBuildSubtarget != MobileTextureSubtarget.ASTC)
            {
                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            }
            
        }

        private static void RequestRestart(string loader)
        {
            if (!Application.isBatchMode && !EditorPrefs.GetBool("PlatformSwitchRestart"))
            {
                EditorPrefs.SetBool("PlatformSwitchRestart", true);
                EditorPrefs.SetString("PlatformSwitchVRSDK", "");
                
                for (int j = 0; j < xrSDKs.Count; j++)
                {
                    if (xrSDKs[j].LoaderType == loader)
                    {
                        EditorPrefs.SetString("PlatformSwitchVRSDK", xrSDKs[j].Name);
                        break;
                    }
                }
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
                if(setting.Key == "name")
                {
                    settingValue = $"VRC {setting.Value}";
                }
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

            Debug.Log($"A quality setting was changed resetting to the default quality: {_graphicsPresets[defaultQuality]["name"]}.");

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
            
            SerializedProperty alwaysIncluded = graphicsManager.FindProperty("m_AlwaysIncludedShaders");
            if (alwaysIncluded.arraySize != 0)
            {
                alwaysIncluded.arraySize = 0;
                isDirty = true;
            }

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
        
        private static void SetSpatializerPluginSettings()
        {
            string[] desktopGuids = AssetDatabase.FindAssets("AudioPluginOculusSpatializer");

            var plugins = new List<PluginImporter>();
            foreach (var guid in desktopGuids)
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as PluginImporter;
                if (importer == null)
                {
                    continue;
                }

                if (importer.assetPath.Contains("com.vrchat.base"))
                {
                    plugins.Add(importer);
                }
            }

            var shouldWarn = false;

            foreach (var plugin in plugins)
            {
                var sO = new SerializedObject(plugin);
                var overrideProp = sO.FindProperty("m_IsOverridable");
                if (overrideProp.boolValue)
                {
                    shouldWarn = true;
                }
                overrideProp.boolValue = false;
                sO.ApplyModifiedProperties();
                plugin.SaveAndReimport();
            }

            if (shouldWarn)
            {
                if (!EditorUtility.DisplayDialog(
                        "Spatializer Settings Updated", 
                        "VRChat SDK detected incorrect Audio Spatializer settings and corrected them." +
                        "\n\nFor the changes to fully apply - you need to restart your editor",
                        "Restart Later",
                        "Save and Restart",
                        DialogOptOutDecisionType.ForThisMachine,
                        "VRC.Editor.EnvConfig.ShowSpatializerApplyDialog")
                   )
                {
                    EditorSceneManager.SaveOpenScenes();
                    EditorApplication.OpenProject(Directory.GetCurrentDirectory());
                }
            }
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

            if (!EditorApplication.isPlaying)
            {
            #pragma warning disable 618
                PlayerSettings.SetVirtualRealitySupported(EditorUserBuildSettings.selectedBuildTargetGroup, true);
            #pragma warning restore 618
            }
            
            #if VRC_DISABLE_GRAPHICS_JOBS
            PlayerSettings.graphicsJobs = false;
            #else
            PlayerSettings.graphicsJobs = true;
            #endif

            PlayerSettings.gpuSkinning = true;

            PlayerSettings.legacyClampBlendShapeWeights = true;

            PlayerSettings.gcIncremental = true;

            PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;
            
            PlayerSettings.SetIl2CppCompilerConfiguration(EditorUserBuildSettings.selectedBuildTargetGroup, Il2CppCompilerConfiguration.Release);
            
            XRGeneralSettingsPerBuildTarget generalSettings;
            if (!EditorBuildSettings.TryGetConfigObject(
                    XRGeneralSettings.k_SettingsKey, out generalSettings))
            { 
                generalSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                if(!AssetDatabase.IsValidFolder("Assets/XR"))
                    AssetDatabase.CreateFolder("Assets", "XR");
                AssetDatabase.CreateAsset(generalSettings, "Assets/XR/XRGeneralSettings.asset");
                AssetDatabase.SaveAssets();
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, generalSettings, true);
                
                // Re-retrieve the config object so it won't crash CreateDefaultSettingsForBuildTarget
                EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out generalSettings);
            }
            
            if(!generalSettings.HasSettingsForBuildTarget(BuildTargetGroup.Standalone))
                generalSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Standalone);
            
            XRGeneralSettings settings = generalSettings.SettingsForBuildTarget(BuildTargetGroup.Standalone);
            if(settings != null)
            {
                settings.InitManagerOnStart = false;
                
                if(settings.Manager != null)
                {
                    XRPackageMetadataStore.AssignLoader(settings.Manager, "Unity.XR.Oculus.OculusLoader",
                    BuildTargetGroup.Standalone);
                }
            } 
            
            if(!generalSettings.HasSettingsForBuildTarget(BuildTargetGroup.Android)) 
                generalSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            
            settings = generalSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (settings != null)
            {
                settings.InitManagerOnStart = false;
                
                if (settings.Manager != null)
                {
                    XRPackageMetadataStore.AssignLoader(settings.Manager, "Unity.XR.Oculus.OculusLoader",
                        BuildTargetGroup.Android);
                }
            } 

            if(!generalSettings.HasSettingsForBuildTarget(BuildTargetGroup.iOS))
                generalSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.iOS);
            
            settings = generalSettings.SettingsForBuildTarget(BuildTargetGroup.iOS);
            if(settings != null)
            {
                settings.InitManagerOnStart = false;
            } 
            
            #if UNITY_ANDROID
                PlayerSettings.Android.forceSDCardPermission = true; // Need access to SD card for saving images
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

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
                        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)33;
                        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
                        PlayerSettings.Android.optimizedFramePacing = false;
                    #endif
                #else
                    PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
                #endif
            #endif // UNITY_ANDROID

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

            // TODO remove once player persistence is enabled by default
            if(assemblies.Any(assembly => assembly.GetType("VRC.SDK3.ClientSim.Persistence.ClientSimPlayerDataStorage") != null))
            {
                if(!defines.Contains("VRC_ENABLE_PLAYER_PERSISTENCE", StringComparer.OrdinalIgnoreCase))
                {
                    defines.Add("VRC_ENABLE_PLAYER_PERSISTENCE");
                    definesChanged = true;
                }
            }
            else if(defines.Contains("VRC_ENABLE_PLAYER_PERSISTENCE"))
            {
                defines.Remove("VRC_ENABLE_PLAYER_PERSISTENCE");
            }
            
            if(definesChanged)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines.ToArray()));
            }
        }

        private static void SetBuildTarget()
        {
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
                { "globalTextureMipmapLimit", 0 },
                { "textureMipmapLimitSettings", Array.Empty<string>() },
                { "anisotropicTextures", 2 },
                { "antiAliasing", 0 },
                { "softParticles", true },
                { "softVegetation", true },
                { "realtimeReflectionProbes", true },
                { "billboardsFaceCameraPosition", true },
                { "useLegacyDetailDistribution", true },
                { "vSyncCount", 0 },
                { "lodBias", 1f },
                { "maximumLODLevel", 0 },
                { "enableLODCrossFade", true },
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
                { "terrainQualityOverrides", 0 },
                { "terrainPixelError", 1.0f },
                { "terrainDetailDensityScale", 1.0f },
                { "terrainBasemapDistance", 1000.0f },
                { "terrainDetailDistance", 80.0f },
                { "terrainTreeDistance", 5000.0f },
                { "terrainBillboardStart", 50.0f },
                { "terrainFadeLength", 5.0f },
                { "terrainMaxTrees", 50 },
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
                { "globalTextureMipmapLimit", 0 },
                { "textureMipmapLimitSettings", Array.Empty<string>() },
                { "anisotropicTextures", 2 },
                { "antiAliasing", 2 },
                { "softParticles", true },
                { "softVegetation", true },
                { "realtimeReflectionProbes", true },
                { "billboardsFaceCameraPosition", true },
                { "useLegacyDetailDistribution", true },
                { "vSyncCount", 0 },
                { "lodBias", 1.5f },
                { "maximumLODLevel", 0 },
                { "enableLODCrossFade", true },
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
                { "terrainQualityOverrides", 0 },
                { "terrainPixelError", 1.0f },
                { "terrainDetailDensityScale", 1.0f },
                { "terrainBasemapDistance", 1000.0f },
                { "terrainDetailDistance", 80.0f },
                { "terrainTreeDistance", 5000.0f },
                { "terrainBillboardStart", 50.0f },
                { "terrainFadeLength", 5.0f },
                { "terrainMaxTrees", 50 },
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
                { "globalTextureMipmapLimit", 0 },
                { "textureMipmapLimitSettings", Array.Empty<string>() },
                { "anisotropicTextures", 2 },
                { "antiAliasing", 4 },
                { "softParticles", true },
                { "softVegetation", true },
                { "realtimeReflectionProbes", true },
                { "billboardsFaceCameraPosition", true },
                { "useLegacyDetailDistribution", true },
                { "vSyncCount", 0 },
                { "lodBias", 2f },
                { "maximumLODLevel", 0 },
                { "enableLODCrossFade", true },
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
                { "terrainQualityOverrides", 0 },
                { "terrainPixelError", 1.0f },
                { "terrainDetailDensityScale", 1.0f },
                { "terrainBasemapDistance", 1000.0f },
                { "terrainDetailDistance", 80.0f },
                { "terrainTreeDistance", 5000.0f },
                { "terrainBillboardStart", 50.0f },
                { "terrainFadeLength", 5.0f },
                { "terrainMaxTrees", 50 },
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
                { "globalTextureMipmapLimit", 0 },
                { "textureMipmapLimitSettings", Array.Empty<string>() },
                { "anisotropicTextures", 2 },
                { "antiAliasing", 2 },
                { "softParticles", false },
                { "softVegetation", false },
                { "realtimeReflectionProbes", false },
                { "billboardsFaceCameraPosition", true },
                { "useLegacyDetailDistribution", true },
                { "vSyncCount", 0 },
                { "lodBias", 2f },
                { "maximumLODLevel", 0 },
                { "enableLODCrossFade", true },
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
                { "terrainQualityOverrides", 0 },
                { "terrainPixelError", 1.0f },
                { "terrainDetailDensityScale", 1.0f },
                { "terrainBasemapDistance", 1000.0f },
                { "terrainDetailDistance", 80.0f },
                { "terrainTreeDistance", 5000.0f },
                { "terrainBillboardStart", 50.0f },
                { "terrainFadeLength", 5.0f },
                { "terrainMaxTrees", 50 },
                { "excludedTargetPlatforms", new[] { "Standalone" } }
            }
        };
    }
}
