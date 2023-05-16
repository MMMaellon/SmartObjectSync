using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Editor;
using VRC.SDK3.Editor;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using Object = UnityEngine.Object;

[assembly: VRCSdkControlPanelBuilder(typeof(VRCSdkControlPanelWorldBuilder3))]

namespace VRC.SDK3.Editor
{
    public class VRCSdkControlPanelWorldBuilder3 : VRCSdkControlPanelWorldBuilder
    {
        #region IVRCSdkControlPanelBuilder implementation
        public override bool IsValidBuilder(out string message)
        {
            bool result = base.IsValidBuilder(out message);
            message = "A VRCSceneDescriptor or VRCAvatarDescriptor\nis required to build VRChat SDK Content";
            return result;
        }

        public override void ShowBuilder()
        {
            List<UdonBehaviour> failedBehaviours = ShouldShowPrimitivesWarning();
            if (failedBehaviours.Count > 0)
            {
                _builder.OnGUIWarning(null,
                    "Udon Objects reference builtin Unity mesh assets, this won't work. Consider making a copy of the mesh to use instead.",
                    () =>
                    {
                        Selection.objects = failedBehaviours.Select(s => s.gameObject).Cast<Object>().ToArray();
                    }, FixPrimitivesWarning);
            }
            base.ShowBuilder();
        }

        public override void SelectAllComponents()
        {
            Debug.Log("SelectAllComponents");
        }

        protected override bool IsSDK3Scene()
        {
            return true;
        }

        protected override void OnGUISceneCheck(VRC.SDKBase.VRC_SceneDescriptor scene)
        {
            base.OnGUISceneCheck(scene);
            
            var resyncNotEnabled = Object.FindObjectsOfType<VRC.SDK3.Video.Components.Base.BaseVRCVideoPlayer>().Where(vp => !vp.EnableAutomaticResync).ToArray();
            if (resyncNotEnabled.Length > 0)
            {
                _builder.OnGUIWarning(null,
                "Video Players do not have automatic resync enabled; audio may become desynchronized from video during low performance.", 
                () =>
                {
                    Selection.objects = resyncNotEnabled.Select(s => s.gameObject).Cast<Object>().ToArray();
                },
                () =>
                {
                    foreach (var vp in resyncNotEnabled)
                        vp.EnableAutomaticResync = true;
                });
            }
            
            foreach (VRC.SDK3.Components.VRCObjectSync os in Object.FindObjectsOfType<VRC.SDK3.Components.VRCObjectSync>())
            {
                if (os.GetComponents<VRC.Udon.UdonBehaviour>().Any((ub) => ub.SyncIsManual))
                    _builder.OnGUIError(scene, "Object Sync cannot share an object with a manually synchronized Udon Behaviour",
                        delegate { Selection.activeObject = os.gameObject; }, null);
                if (os.GetComponent<VRC.SDK3.Components.VRCObjectPool>() != null)
                    _builder.OnGUIError(scene, "Object Sync cannot share an object with an object pool",
                        delegate { Selection.activeObject = os.gameObject; }, null);
            }
        } 

        #endregion

        public override void OnGUIScene()
        {
                 GUILayout.Label("", VRCSdkControlPanel.scrollViewSeparatorStyle);

            _builderScrollPos = GUILayout.BeginScrollView(_builderScrollPos, false, false, GUIStyle.none,
                GUI.skin.verticalScrollbar, GUILayout.Width(VRCSdkControlPanel.SdkWindowWidth),
                GUILayout.MinHeight(217));

            GUILayout.BeginVertical(VRCSdkControlPanel.boxGuiStyle, GUILayout.Width(VRCSdkControlPanel.SdkWindowWidth));
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(300));
            EditorGUILayout.Space();
            GUILayout.Label("Local Testing", VRCSdkControlPanel.infoGuiStyle);
            GUILayout.Label(
                "Before uploading your world you may build and test it in the VRChat client. You won't be able to invite anyone from online but you can launch multiple of your own clients.",
                VRCSdkControlPanel.infoGuiStyle);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(200));
            EditorGUILayout.Space();
            VRCSettings.NumClients = EditorGUILayout.IntField("Number of Clients", Mathf.Clamp(VRCSettings.NumClients, 0, 8), GUILayout.MaxWidth(190));
            EditorGUILayout.Space();
            VRCSettings.ForceNoVR = EditorGUILayout.Toggle("Force Non-VR", VRCSettings.ForceNoVR, GUILayout.MaxWidth(190));
            EditorGUILayout.Space();
            if (VRCSettings.DisplayAdvancedSettings)
            {
                VRCSettings.WatchWorlds =
                    EditorGUILayout.Toggle("Enable World Reload", VRCSettings.WatchWorlds, GUILayout.MaxWidth(190));
                EditorGUILayout.Space();
            }

            GUI.enabled = _builder.NoGuiErrorsOrIssues();

            string lastUrl = VRC_SdkBuilder.GetLastUrl();

            bool doReload = VRCSettings.WatchWorlds && VRCSettings.NumClients == 0;
            
            bool lastBuildPresent = lastUrl != null;
            if (lastBuildPresent == false)
                GUI.enabled = false;
            if (VRCSettings.DisplayAdvancedSettings)
            {
                string lastBuildLabel = doReload ? "Reload Last Build" : "Last Build";
                if (GUILayout.Button(lastBuildLabel))
                {
                    if (doReload)
                    {
                        // Todo: get this from settings or make key a const
                        string path = EditorPrefs.GetString("lastVRCPath");
                        if (File.Exists(path))
                        {
                            File.SetLastWriteTimeUtc(path, DateTime.Now);
                        }
                        else
                        {
                            Debug.LogWarning($"Cannot find last built scene, please Rebuild.");
                        }
                    }
                    else
                    {
                        VRC_SdkBuilder.shouldBuildUnityPackage = false;
                        VRC_SdkBuilder.RunLastExportedSceneResource();
                    }
                }

                if (Core.APIUser.CurrentUser.hasSuperPowers)
                {
                    if (GUILayout.Button("Copy Test URL"))
                    {
                        TextEditor te = new TextEditor {text = lastUrl};
                        te.SelectAll();
                        te.Copy();
                    }
                }
            }

            GUI.enabled = _builder.NoGuiErrorsOrIssues() ||
                          Core.APIUser.CurrentUser.developerType == Core.APIUser.DeveloperType.Internal;

#if UNITY_ANDROID || UNITY_IOS
            EditorGUI.BeginDisabledGroup(true);
#endif
            string buildLabel = doReload ? "Build & Reload" : "Build & Test";
            if (GUILayout.Button(buildLabel))
            {
                bool buildTestBlocked = !VRCBuildPipelineCallbacks.OnVRCSDKBuildRequested(VRCSDKRequestedBuildType.Scene);
                if (!buildTestBlocked)
                {
                    EnvConfig.ConfigurePlayerSettings();
                    VRC_SdkBuilder.shouldBuildUnityPackage = false;
                    AssetExporter.CleanupUnityPackageExport(); // force unity package rebuild on next publish
                    VRC_SdkBuilder.PreBuildBehaviourPackaging();
                    if (doReload)
                    {
                        VRC_SdkBuilder.ExportSceneResource();
                    }
                    else
                    {
                        VRC_SdkBuilder.ExportSceneResourceAndRun();
                    }
                }
            }
#if UNITY_ANDROID || UNITY_IOS
            EditorGUI.EndDisabledGroup();
#endif

            GUILayout.EndVertical();

            if (Event.current.type != EventType.Used)
            {
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
                GUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            GUILayout.BeginVertical(VRCSdkControlPanel.boxGuiStyle, GUILayout.Width(VRCSdkControlPanel.SdkWindowWidth));

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(300));
            EditorGUILayout.Space();
            GUILayout.Label("Online Publishing", VRCSdkControlPanel.infoGuiStyle);
            GUILayout.Label(
                "In order for other people to enter your world in VRChat it must be built and published to our game servers.",
                VRCSdkControlPanel.infoGuiStyle);
            EditorGUILayout.Space();
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(200));
            EditorGUILayout.Space();

            if (lastBuildPresent == false)
                GUI.enabled = false;
            if (VRCSettings.DisplayAdvancedSettings)
            {
                if (GUILayout.Button("Last Build"))
                {
                    if (Core.APIUser.CurrentUser.canPublishWorlds)
                    {
                        EditorPrefs.SetBool("VRC.SDKBase_StripAllShaders", false);
                        VRC_SdkBuilder.shouldBuildUnityPackage = VRCSdkControlPanel.FutureProofPublishEnabled;
                        VRC_SdkBuilder.UploadLastExportedSceneBlueprint();
                    }
                    else
                    {
                        VRCSdkControlPanel.ShowContentPublishPermissionsDialog();
                    }
                }
            }

            GUI.enabled = _builder.NoGuiErrorsOrIssues() ||
                          Core.APIUser.CurrentUser.developerType == Core.APIUser.DeveloperType.Internal;
            if (GUILayout.Button(VRCSdkControlPanel.GetBuildAndPublishButtonString()))
            {
                bool buildBlocked = !VRCBuildPipelineCallbacks.OnVRCSDKBuildRequested(VRCSDKRequestedBuildType.Scene);
                if (!buildBlocked)
                {
                    if (Core.APIUser.CurrentUser.canPublishWorlds)
                    {
                        EnvConfig.ConfigurePlayerSettings();
                        EditorPrefs.SetBool("VRC.SDKBase_StripAllShaders", false);
                        
                        VRC_SdkBuilder.shouldBuildUnityPackage = VRCSdkControlPanel.FutureProofPublishEnabled;
                        VRC_SdkBuilder.PreBuildBehaviourPackaging();
                        VRC_SdkBuilder.ExportAndUploadSceneBlueprint();
                    }
                    else
                    {
                        VRCSdkControlPanel.ShowContentPublishPermissionsDialog();
                    }
                }
            }

            GUILayout.EndVertical();
            GUI.enabled = true;

            if (Event.current.type == EventType.Used) return;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private static Mesh[] _primitiveMeshes;

        private static List<UdonBehaviour> ShouldShowPrimitivesWarning()
        {
            if (_primitiveMeshes == null)
            {
                PrimitiveType[] primitiveTypes = (PrimitiveType[]) System.Enum.GetValues(typeof(PrimitiveType));
                _primitiveMeshes = new Mesh[primitiveTypes.Length];

                for (int i = 0; i < primitiveTypes.Length; i++)
                {
                    PrimitiveType primitiveType = primitiveTypes[i];
                    GameObject go = GameObject.CreatePrimitive(primitiveType);
                    _primitiveMeshes[i] = go.GetComponent<MeshFilter>().sharedMesh;
                    Object.DestroyImmediate(go);
                }
            }

            UdonBehaviour[] allBehaviours = Object.FindObjectsOfType<UdonBehaviour>();
            List<UdonBehaviour> failedBehaviours = new List<UdonBehaviour>(allBehaviours.Length);
            foreach (UdonBehaviour behaviour in allBehaviours)
            {
                IUdonVariableTable publicVariables = behaviour.publicVariables;
                foreach (string symbol in publicVariables.VariableSymbols)
                {
                    if (!publicVariables.TryGetVariableValue(symbol, out Mesh mesh))
                    {
                        continue;
                    }

                    if (mesh == null)
                    {
                        continue;
                    }

                    bool all = true;
                    foreach (Mesh primitiveMesh in _primitiveMeshes)
                    {
                        if (mesh != primitiveMesh)
                        {
                            continue;
                        }

                        all = false;
                        break;
                    }

                    if (all)
                    {
                        continue;
                    }

                    failedBehaviours.Add(behaviour);
                }
            }

            return failedBehaviours;
        }

        private void FixPrimitivesWarning()
        {
            UdonBehaviour[] allObjects = Object.FindObjectsOfType<UdonBehaviour>();
            foreach (UdonBehaviour behaviour in allObjects)
            {
                IUdonVariableTable publicVariables = behaviour.publicVariables;
                foreach (string symbol in publicVariables.VariableSymbols)
                {
                    if (!publicVariables.TryGetVariableValue(symbol, out Mesh mesh))
                    {
                        continue;
                    }

                    if (mesh == null)
                    {
                        continue;
                    }

                    bool all = true;
                    foreach (Mesh primitiveMesh in _primitiveMeshes)
                    {
                        if (mesh != primitiveMesh)
                        {
                            continue;
                        }

                        all = false;
                        break;
                    }

                    if (all)
                    {
                        continue;
                    }

                    Mesh clone = Object.Instantiate(mesh);

                    Scene scene = behaviour.gameObject.scene;
                    string scenePath = Path.GetDirectoryName(scene.path) ?? "Assets";

                    string folderName = $"{scene.name}_MeshClones";
                    string folderPath = Path.Combine(scenePath, folderName);

                    if (!AssetDatabase.IsValidFolder(folderPath))
                    {
                        AssetDatabase.CreateFolder(scenePath, folderName);
                    }

                    string assetPath = Path.Combine(folderPath, $"{clone.name}.asset");

                    Mesh existingClone = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                    if (existingClone == null)
                    {
                        AssetDatabase.CreateAsset(clone, assetPath);
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        clone = existingClone;
                    }

                    publicVariables.TrySetVariableValue(symbol, clone);
                    EditorSceneManager.MarkSceneDirty(behaviour.gameObject.scene);
                }
            }
        }
    }
}
