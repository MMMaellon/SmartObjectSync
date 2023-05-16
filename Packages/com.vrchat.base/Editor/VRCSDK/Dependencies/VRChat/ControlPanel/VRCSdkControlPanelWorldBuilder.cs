#if !VRC_CLIENT
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using VRC.Editor;
using VRC.SDKBase.Editor.Validation;
using Object = UnityEngine.Object;

namespace VRC.SDKBase.Editor
{
    public class VRCSdkControlPanelWorldBuilder : IVRCSdkControlPanelBuilder
    {
        private static Type _postProcessVolumeType;
        protected VRCSdkControlPanel _builder;
        private VRC_SceneDescriptor[] _scenes;
        private Vector2 _scrollPos;
        protected Vector2 _builderScrollPos;
        
        public virtual void SelectAllComponents()
        {
            List<Object> show = new List<Object>(Selection.objects);
            foreach (VRC_SceneDescriptor s in _scenes)
                show.Add(s.gameObject);
            Selection.objects = show.ToArray();
        }
        
        public virtual void ShowSettingsOptions()
        {
            EditorGUILayout.BeginVertical(VRCSdkControlPanel.boxGuiStyle);
            GUILayout.Label("World Options", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            int prevLineMode = _builder.triggerLineMode;
            int lineMode = Convert.ToInt32(EditorGUILayout.EnumPopup("Trigger Lines", (VRC_Trigger.EditorTriggerLineMode)_builder.triggerLineMode, GUILayout.Width(250)));
            if (lineMode != prevLineMode)
            {
                _builder.triggerLineMode = lineMode;
                foreach (GameObject t in Selection.gameObjects)
                {
                    EditorUtility.SetDirty(t);
                }
            }
            GUILayout.Space(10);
            switch ((VRC_Trigger.EditorTriggerLineMode)_builder.triggerLineMode)
            {
                case VRC_Trigger.EditorTriggerLineMode.Enabled:
                    EditorGUILayout.LabelField("Lines shown for all selected triggers", EditorStyles.miniLabel);
                    break;
                case VRC_Trigger.EditorTriggerLineMode.Disabled:
                    EditorGUILayout.LabelField("No trigger lines are drawn", EditorStyles.miniLabel);
                    break;
                case VRC_Trigger.EditorTriggerLineMode.PerTrigger:
                    EditorGUILayout.LabelField("Toggle lines directly on each trigger component", EditorStyles.miniLabel);
                    break;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        public virtual bool IsValidBuilder(out string message)
        {
            FindScenes();
            message = null;
            if (_scenes != null && _scenes.Length > 0) return true;
            return false;
        }

        public virtual void ShowBuilder()
        {
            if (_postProcessVolumeType != null)
            {
                if (Camera.main != null && Camera.main.GetComponentInChildren(_postProcessVolumeType))
                {
                    _builder.OnGUIWarning(null,
                        "Scene has a PostProcessVolume on the Reference Camera (Main Camera). This Camera is disabled at runtime. Please move the PostProcessVolume to another GameObject.",
                        () => { Selection.activeGameObject = Camera.main.gameObject; },
                        TryMovePostProcessVolumeAwayFromMainCamera
                    );
                }
            }

            if (_scenes.Length > 1)
            {
                Object[] gos = new Object[_scenes.Length];
                for (int i = 0; i < _scenes.Length; ++i)
                    gos[i] = _scenes[i].gameObject;
                _builder.OnGUIError(null,
                    "A Unity scene containing a VRChat Scene Descriptor should only contain one Scene Descriptor.",
                    delegate { Selection.objects = gos; }, null);

                EditorGUILayout.Separator();
                GUILayout.BeginVertical(GUILayout.Width(VRCSdkControlPanel.SdkWindowWidth));
                _builder.OnGUIShowIssues();
                GUILayout.EndVertical();
            }
            else if (_scenes.Length == 1)
            {
                bool inScrollView = true;

                _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false, GUIStyle.none,
                    GUI.skin.verticalScrollbar, GUILayout.Width(VRCSdkControlPanel.SdkWindowWidth));

                try
                {
                    bool setupRequired = OnGUISceneSetup();

                    if (!setupRequired)
                    {
                        if (!_builder.CheckedForIssues)
                        {
                            _builder.ResetIssues();
                            OnGUISceneCheck(_scenes[0]);
                            _builder.CheckedForIssues = true;
                        }

                        OnGUISceneSettings(_scenes[0]);

                        _builder.OnGUIShowIssues();
                        _builder.OnGUIShowIssues(_scenes[0]);

                        GUILayout.FlexibleSpace();

                        GUILayout.EndScrollView();
                        inScrollView = false;

                        OnGUIScene();
                    }
                    else
                    {
                        _builder.OnGuiFixIssuesToBuildOrTest();
                        GUILayout.EndScrollView();
                    }
                }
                catch (Exception)
                {
                    if (inScrollView)
                        GUILayout.EndScrollView();
                }
            }
            else
            {
                EditorGUILayout.Space();
                if (UnityEditor.BuildPipeline.isBuildingPlayer)
                {
                    GUILayout.Space(20);
                    EditorGUILayout.LabelField("Building – Please Wait ...", VRCSdkControlPanel.titleGuiStyle,
                        GUILayout.Width(VRCSdkControlPanel.SdkWindowWidth));
                }
            }
        }

        public virtual void RegisterBuilder(VRCSdkControlPanel baseBuilder)
        {
            _builder = baseBuilder;
        }

        private void TryMovePostProcessVolumeAwayFromMainCamera()
        {
            if (Camera.main == null)
                return;
            if (_postProcessVolumeType == null)
                return;
            Component oldVolume = Camera.main.GetComponentInChildren(_postProcessVolumeType);
            if (!oldVolume)
                return;
            GameObject oldObject = oldVolume.gameObject;
            GameObject newObject = Object.Instantiate(oldObject);
            newObject.name = "Post Processing Volume";
            newObject.tag = "Untagged";
            foreach (Transform child in newObject.transform)
            {
                Object.DestroyImmediate(child.gameObject);
            }

            var newVolume = newObject.GetComponentInChildren(_postProcessVolumeType);
            foreach (Component c in newObject.GetComponents<Component>())
            {
                if ((c == newObject.transform) || (c == newVolume))
                    continue;
                Object.DestroyImmediate(c);
            }

            Object.DestroyImmediate(oldVolume);
            _builder.Repaint();
            Selection.activeGameObject = newObject;
        }

        [UnityEditor.Callbacks.DidReloadScripts(int.MaxValue)]
        static void DidReloadScripts()
        {
            DetectPostProcessingPackage();
        }

        static void DetectPostProcessingPackage()
        {
            _postProcessVolumeType = null;
            try
            {
                System.Reflection.Assembly
                    postProcAss = System.Reflection.Assembly.Load("Unity.PostProcessing.Runtime");
                _postProcessVolumeType = postProcAss.GetType("UnityEngine.Rendering.PostProcessing.PostProcessVolume");
            }
            catch
            {
                // -> post processing not installed
            }
        }
        
        private void FindScenes()
        {
            VRC_SceneDescriptor[] newScenes = Tools.FindSceneObjectsOfTypeAll<VRC_SceneDescriptor>();

            if (_scenes != null)
            {
                foreach (VRC_SceneDescriptor s in newScenes)
                    if (_scenes.Contains(s) == false)
                        _builder.CheckedForIssues = false;
            }

            _scenes = newScenes;
        }

        private static bool OnGUISceneSetup()
        {
            bool mandatoryExpand = !UpdateLayers.AreLayersSetup() || !UpdateLayers.IsCollisionLayerMatrixSetup();
            if (mandatoryExpand)
                EditorGUILayout.LabelField("VRChat Scene Setup", VRCSdkControlPanel.titleGuiStyle,
                    GUILayout.Height(50));

            if (!UpdateLayers.AreLayersSetup())
            {
                GUILayout.BeginVertical(VRCSdkControlPanel.boxGuiStyle, GUILayout.Height(100),
                    GUILayout.Width(VRCSdkControlPanel.SdkWindowWidth));
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(300));
                EditorGUILayout.Space();
                GUILayout.Label("Layers", VRCSdkControlPanel.infoGuiStyle);
                GUILayout.Label(
                    "VRChat scenes must have the same Unity layer configuration as VRChat so we can all predict things like physics and collisions. Pressing this button will configure your project's layers to match VRChat.",
                    VRCSdkControlPanel.infoGuiStyle, GUILayout.Width(300));
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(150));
                GUILayout.Label("", GUILayout.Height(15));
                if (UpdateLayers.AreLayersSetup())
                {
                    GUILayout.Label("Step Complete!", VRCSdkControlPanel.infoGuiStyle);
                }
                else if (GUILayout.Button("Setup Layers for VRChat", GUILayout.Width(172)))
                {
                    bool doIt = EditorUtility.DisplayDialog("Setup Layers for VRChat",
                        "This adds all VRChat layers to your project and pushes any custom layers down the layer list. If you have custom layers assigned to gameObjects, you'll need to reassign them. Are you sure you want to continue?",
                        "Do it!", "Don't do it");
                    if (doIt)
                        UpdateLayers.SetupEditorLayers();
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }

            if (!UpdateLayers.IsCollisionLayerMatrixSetup())
            {
                GUILayout.BeginVertical(VRCSdkControlPanel.boxGuiStyle, GUILayout.Height(100),
                    GUILayout.Width(VRCSdkControlPanel.SdkWindowWidth));
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(300));
                EditorGUILayout.Space();
                GUILayout.Label("Collision Matrix", VRCSdkControlPanel.infoGuiStyle);
                GUILayout.Label(
                    "VRChat uses specific layers for collision. In order for testing and development to run smoothly it is necessary to configure your project's collision matrix to match that of VRChat.",
                    VRCSdkControlPanel.infoGuiStyle, GUILayout.Width(300));
                GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(150));
                GUILayout.Label("", GUILayout.Height(15));
                if (UpdateLayers.AreLayersSetup() == false)
                {
                    GUILayout.Label("You must first configure your layers for VRChat to proceed. Please see above.",
                        VRCSdkControlPanel.infoGuiStyle);
                }
                else if (UpdateLayers.IsCollisionLayerMatrixSetup())
                {
                    GUILayout.Label("Step Complete!", VRCSdkControlPanel.infoGuiStyle);
                }
                else
                {
                    if (GUILayout.Button("Set Collision Matrix", GUILayout.Width(172)))
                    {
                        bool doIt = EditorUtility.DisplayDialog("Setup Collision Layer Matrix for VRChat",
                            "This will setup the correct physics collisions in the PhysicsManager for VRChat layers. Are you sure you want to continue?",
                            "Do it!", "Don't do it");
                        if (doIt)
                        {
                            UpdateLayers.SetupCollisionLayerMatrix();
                        }
                    }
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }

            return mandatoryExpand;
        }
        
        protected virtual bool IsSDK3Scene()
        {
            return false;
        }

        protected virtual void OnGUISceneCheck(VRC_SceneDescriptor scene)
        {
            CheckUploadChanges(scene);
            
            bool isSdk3Scene = IsSDK3Scene();

            List<VRC_EventHandler> sdkBaseEventHandlers =
                new List<VRC_EventHandler>(Object.FindObjectsOfType<VRC_EventHandler>());
#if VRC_SDK_VRCSDK2
        if (isSdk3Scene == false)
        {
            for (int i = sdkBaseEventHandlers.Count - 1; i >= 0; --i)
                if (sdkBaseEventHandlers[i] as VRCSDK2.VRC_EventHandler)
                    sdkBaseEventHandlers.RemoveAt(i);
        }
#endif
            if (sdkBaseEventHandlers.Count > 0)
            {
                _builder.OnGUIError(scene,
                    "You have Event Handlers in your scene that are not allowed in this build configuration.",
                    delegate
                    {
                        List<Object> gos = sdkBaseEventHandlers.ConvertAll(item => (Object) item.gameObject);
                        Selection.objects = gos.ToArray();
                    },
                    delegate
                    {
                        foreach (VRC_EventHandler eh in sdkBaseEventHandlers)
                        {
#if VRC_SDK_VRCSDK2
                        GameObject go = eh.gameObject;
                        if (isSdk3Scene == false)
                        {
                            if (VRC_SceneDescriptor.Instance as VRCSDK2.VRC_SceneDescriptor != null)
                                go.AddComponent<VRCSDK2.VRC_EventHandler>();
                        }
#endif
                            Object.DestroyImmediate(eh);
                        }
                    });
            }

            Vector3 g = Physics.gravity;
            if (Math.Abs(g.x) > float.Epsilon || Math.Abs(g.z) > float.Epsilon)
                _builder.OnGUIWarning(scene,
                    "Gravity vector is not straight down. Though we support different gravity, player orientation is always 'upwards' so things don't always behave as you intend.",
                    delegate { SettingsService.OpenProjectSettings("Project/Physics"); }, null);
            if (g.y > 0)
                _builder.OnGUIWarning(scene,
                    "Gravity vector is not straight down, inverted or zero gravity will make walking extremely difficult.",
                    delegate { SettingsService.OpenProjectSettings("Project/Physics"); }, null);
            if (Math.Abs(g.y) < float.Epsilon)
                _builder.OnGUIWarning(scene,
                    "Zero gravity will make walking extremely difficult, though we support different gravity, player orientation is always 'upwards' so this may not have the effect you're looking for.",
                    delegate { SettingsService.OpenProjectSettings("Project/Physics"); }, null);

            if (CheckFogSettings())
            {
                _builder.OnGUIWarning(
                    scene,
                    "Fog shader stripping is set to Custom, this may lead to incorrect or unnecessary shader variants being included in the build. You should use Automatic unless you change the fog mode at runtime.",
                    delegate { SettingsService.OpenProjectSettings("Project/Graphics"); },
                    delegate
                    {
                        EnvConfig.SetFogSettings(
                            new EnvConfig.FogSettings(EnvConfig.FogSettings.FogStrippingMode.Automatic));
                    });
            }

            if (scene.autoSpatializeAudioSources)
            {
                _builder.OnGUIWarning(scene,
                    "Your scene previously used the 'Auto Spatialize Audio Sources' feature. This has been deprecated, press 'Fix' to disable. Also, please add VRC_SpatialAudioSource to all your audio sources. Make sure Spatial Blend is set to 3D for the sources you want to spatialize.",
                    null,
                    delegate { scene.autoSpatializeAudioSources = false; }
                );
            }

            AudioSource[] audioSources = Object.FindObjectsOfType<AudioSource>();
            foreach (AudioSource a in audioSources)
            {
                if (a.GetComponent<ONSPAudioSource>() != null)
                {
                    _builder.OnGUIWarning(scene,
                        "Found audio source(s) using ONSP, this is deprecated. Press 'fix' to convert to VRC_SpatialAudioSource.",
                        delegate { Selection.activeObject = a.gameObject; },
                        delegate
                        {
                            Selection.activeObject = a.gameObject;
                            AutoAddSpatialAudioComponents.ConvertONSPAudioSource(a);
                        }
                    );
                    break;
                }
                else if (a.GetComponent<VRC_SpatialAudioSource>() == null)
                {
                    string msg =
                        "Found 3D audio source with no VRC Spatial Audio component, this is deprecated. Press 'fix' to add a VRC_SpatialAudioSource.";
                    if (IsAudioSource2D(a))
                        msg =
                            "Found 2D audio source with no VRC Spatial Audio component, this is deprecated. Press 'fix' to add a (disabled) VRC_SpatialAudioSource.";

                    _builder.OnGUIWarning(scene, msg,
                        delegate { Selection.activeObject = a.gameObject; },
                        delegate
                        {
                            Selection.activeObject = a.gameObject;
                            AutoAddSpatialAudioComponents.AddVRCSpatialToBareAudioSource(a);
                        }
                    );
                    break;
                }
            }

            if (VRCSdkControlPanel.HasSubstances())
            {
                _builder.OnGUIWarning(scene,
                    "One or more scene objects have Substance materials. This is not supported and may break in game. Please bake your Substances to regular materials.",
                    () => { Selection.objects = VRCSdkControlPanel.GetSubstanceObjects(); },
                    null);
            }

            string vrcFilePath = UnityWebRequest.UnEscapeURL(EditorPrefs.GetString("lastVRCPath"));
            bool isMobilePlatform = ValidationEditorHelpers.IsMobilePlatform();
            if (!string.IsNullOrEmpty(vrcFilePath) &&
                ValidationHelpers.CheckIfAssetBundleFileTooLarge(ContentType.World, vrcFilePath, out int fileSize, isMobilePlatform))
            {
                _builder.OnGUIWarning(scene,
                    ValidationHelpers.GetAssetBundleOverSizeLimitMessageSDKWarning(ContentType.World, fileSize, isMobilePlatform), null,
                    null);
            }

            foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.transform.parent == null)
                {
                    // check root game objects
#if UNITY_ANDROID || UNITY_IOS
                    // check root game objects for illegal shaders
                    IEnumerable<Shader> illegalShaders = VRC.SDKBase.Validation.WorldValidation.FindIllegalShaders(go);
                    foreach (Shader s in illegalShaders)
                    {
                        _builder.OnGUIWarning(scene, "World uses unsupported shader '" + s.name + "'. This could cause low performance or future compatibility issues.", null, null);
                    }
#endif
                }
            }

            // detect dynamic materials and prefabs with identical names (since these could break triggers)
            VRC_Trigger[] triggers = Tools.FindSceneObjectsOfTypeAll<VRC_Trigger>();
            List<GameObject> prefabs = new List<GameObject>();
            List<Material> materials = new List<Material>();

#if VRC_SDK_VRCSDK2
        AssetExporter.FindDynamicContent(ref prefabs, ref materials);
#elif VRC_SDK_VRCSDK3
            AssetExporter.FindDynamicContent(ref prefabs, ref materials);
#endif

            foreach (VRC_Trigger t in triggers)
            {
                foreach (VRC_Trigger.TriggerEvent triggerEvent in t.Triggers)
                {
                    foreach (VRC_EventHandler.VrcEvent e in triggerEvent.Events.Where(evt =>
                        evt.EventType == VRC_EventHandler.VrcEventType.SpawnObject))
                    {
                        GameObject go =
                            AssetDatabase.LoadAssetAtPath(e.ParameterString, typeof(GameObject)) as GameObject;
                        if (go == null) continue;
                        foreach (GameObject existing in prefabs)
                        {
                            if (go == existing || go.name != existing.name) continue;
                            _builder.OnGUIWarning(scene,
                                "Trigger prefab '" + AssetDatabase.GetAssetPath(go).Replace(".prefab", "") +
                                "' has same name as a prefab in another folder. This may break the trigger.",
                                delegate { Selection.objects = new Object[] {go}; },
                                delegate
                                {
                                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(go),
                                        go.name + "-" + go.GetInstanceID());
                                    AssetDatabase.Refresh();
                                    e.ParameterString = AssetDatabase.GetAssetPath(go);
                                });
                        }
                    }

                    foreach (VRC_EventHandler.VrcEvent e in triggerEvent.Events.Where(evt =>
                        evt.EventType == VRC_EventHandler.VrcEventType.SetMaterial))
                    {
                        Material mat = AssetDatabase.LoadAssetAtPath<Material>(e.ParameterString);
                        if (mat == null || mat.name.Contains("(Instance)")) continue;
                        foreach (Material existing in materials)
                        {
                            if (mat == existing || mat.name != existing.name) continue;
                            _builder.OnGUIWarning(scene,
                                "Trigger material '" + AssetDatabase.GetAssetPath(mat).Replace(".mat", "") +
                                "' has same name as a material in another folder. This may break the trigger.",
                                delegate { Selection.objects = new Object[] {mat}; },
                                delegate
                                {
                                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(mat),
                                        mat.name + "-" + mat.GetInstanceID());
                                    AssetDatabase.Refresh();
                                    e.ParameterString = AssetDatabase.GetAssetPath(mat);
                                });
                        }
                    }
                }
            }
        }

        private void OnGUISceneSettings(VRC_SceneDescriptor scene)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(VRCSdkControlPanel.boxGuiStyle, GUILayout.Width(VRCSdkControlPanel.SdkWindowWidth));

            string name = "Unpublished VRChat World";
            if (scene.apiWorld != null)
                name = (scene.apiWorld as Core.ApiWorld)?.name;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(name, VRCSdkControlPanel.titleGuiStyle);

            Core.PipelineManager[] pms = Tools.FindSceneObjectsOfTypeAll<Core.PipelineManager>();
            if (pms.Length == 1)
            {
                if (!string.IsNullOrEmpty(pms[0].blueprintId))
                {
                    if (scene.apiWorld == null)
                    {
                        Core.ApiWorld world = Core.API.FromCacheOrNew<Core.ApiWorld>(pms[0].blueprintId);
                        world.Fetch(null,
                            (c) => scene.apiWorld = c.Model as Core.ApiWorld,
                            (c) =>
                            {
                                if (c.Code == 404)
                                {
                                    Core.Logger.Log(
                                        $"Could not load world {pms[0].blueprintId} because it didn't exist.", Core.DebugLevel.All);
                                    Core.ApiCache.Invalidate(pms[0].blueprintId);
                                }
                                else
                                    Debug.LogErrorFormat("Could not load world {0} because {1}", pms[0].blueprintId,
                                        c.Error);
                            });
                        scene.apiWorld = world;
                    }
                }
                else
                {
                    // clear scene.apiWorld if blueprint ID has been detached, so world details in builder panel are also cleared
                    scene.apiWorld = null;
                }
            }

            if (scene.apiWorld != null)
            {
                Core.ApiWorld w = (scene.apiWorld as Core.ApiWorld);
                DrawContentInfoForWorld(w);
                VRCSdkControlPanel.DrawContentPlatformSupport(w);
            }

            VRCSdkControlPanel.DrawBuildTargetSwitcher();

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public virtual void OnGUIScene()
        {
            // Stub, needs to be handled in SDK-specific overrides
        }

        private static void DrawContentInfoForWorld(Core.ApiWorld w)
        {
            VRCSdkControlPanel.DrawContentInfo(w.name, w.version.ToString(), w.description, w.capacity.ToString(), w.releaseStatus,
                w.tags);
        }

        private static void CheckUploadChanges(VRC_SceneDescriptor scene)
        {
            if (!EditorPrefs.HasKey("VRC.SDKBase_scene_changed") ||
                !EditorPrefs.GetBool("VRC.SDKBase_scene_changed")) return;
            EditorPrefs.DeleteKey("VRC.SDKBase_scene_changed");

            if (EditorPrefs.HasKey("VRC.SDKBase_capacity"))
            {
                scene.capacity = EditorPrefs.GetInt("VRC.SDKBase_capacity");
                EditorPrefs.DeleteKey("VRC.SDKBase_capacity");
            }

            if (EditorPrefs.HasKey("VRC.SDKBase_content_sex"))
            {
                scene.contentSex = EditorPrefs.GetBool("VRC.SDKBase_content_sex");
                EditorPrefs.DeleteKey("VRC.SDKBase_content_sex");
            }

            if (EditorPrefs.HasKey("VRC.SDKBase_content_violence"))
            {
                scene.contentViolence = EditorPrefs.GetBool("VRC.SDKBase_content_violence");
                EditorPrefs.DeleteKey("VRC.SDKBase_content_violence");
            }

            if (EditorPrefs.HasKey("VRC.SDKBase_content_gore"))
            {
                scene.contentGore = EditorPrefs.GetBool("VRC.SDKBase_content_gore");
                EditorPrefs.DeleteKey("VRC.SDKBase_content_gore");
            }

            if (EditorPrefs.HasKey("VRC.SDKBase_content_other"))
            {
                scene.contentOther = EditorPrefs.GetBool("VRC.SDKBase_content_other");
                EditorPrefs.DeleteKey("VRC.SDKBase_content_other");
            }

            if (EditorPrefs.HasKey("VRC.SDKBase_release_public"))
            {
                scene.releasePublic = EditorPrefs.GetBool("VRC.SDKBase_release_public");
                EditorPrefs.DeleteKey("VRC.SDKBase_release_public");
            }

            EditorUtility.SetDirty(scene);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static bool CheckFogSettings()
        {
            EnvConfig.FogSettings fogSettings = EnvConfig.GetFogSettings();
            if (fogSettings.fogStrippingMode == EnvConfig.FogSettings.FogStrippingMode.Automatic)
            {
                return false;
            }

            return fogSettings.keepLinear || fogSettings.keepExp || fogSettings.keepExp2;
        }

        private static bool IsAudioSource2D(AudioSource src)
        {
            AnimationCurve curve = src.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
            return Math.Abs(src.spatialBlend) < float.Epsilon && (curve == null || curve.keys.Length <= 1);
        }
        
        void OnGUISceneLayer(int layer, string name, string description)
        {
            if (LayerMask.LayerToName(layer) != name)
                _builder.OnGUIError(null, "Layer " + layer + " must be renamed to '" + name + "'",
                    delegate { SettingsService.OpenProjectSettings("Project/Physics"); }, null);

            if (_builder.showLayerHelp)
                _builder.OnGUIInformation(null, "Layer " + layer + " " + name + "\n" + description);
        }

    }
}
#endif
