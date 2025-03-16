using UnityEngine;
using UnityEditor;
using VRC.Core;
using VRC.SDKBase;
using VRC.SDKBase.Editor;

// This file handles the Settings tab of the SDK Panel

public partial class VRCSdkControlPanel : EditorWindow
{
    bool UseDevApi
    {
        get
        {
            return VRC.Core.API.GetApiUrl() == VRC.Core.API.devApiUrl;
        }
    }
    
    Vector2 settingsScroll;

    void ShowSettings()
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();

        settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll, GUILayout.Width(SdkWindowWidth - 8));

        EditorGUILayout.BeginVertical(boxGuiStyle);
        EditorGUILayout.LabelField("Developer", EditorStyles.boldLabel);

        VRCSettings.DisplayAdvancedSettings = EditorGUILayout.ToggleLeft("Show Extra Options on account page", VRCSettings.DisplayAdvancedSettings);
        
        bool prevDisplayHelpBoxes = VRCSettings.DisplayHelpBoxes;
        VRCSettings.DisplayHelpBoxes = EditorGUILayout.ToggleLeft("Show Help Boxes on SDK components", VRCSettings.DisplayHelpBoxes);
        if (VRCSettings.DisplayHelpBoxes != prevDisplayHelpBoxes)
        {
            Editor[] editors = (Editor[])Resources.FindObjectsOfTypeAll<Editor>();
            for (int i = 0; i < editors.Length; i++)
            {
                editors[i].Repaint();
            }
        }

        // API logging
        {
            bool isLoggingEnabled = UnityEditor.EditorPrefs.GetBool("apiLoggingEnabled");
            bool enableLogging = EditorGUILayout.ToggleLeft("API Logging Enabled", isLoggingEnabled);
            if (enableLogging != isLoggingEnabled)
            {
                if (enableLogging)
                    VRC.Core.Logger.EnableCategory(DebugLevel.API);
                else
                    VRC.Core.Logger.DisableCategory(DebugLevel.API);

                UnityEditor.EditorPrefs.SetBool("apiLoggingEnabled", enableLogging);
            }
        }

        // Dry Builds
        if (APIUser.CurrentUser != null && APIUser.CurrentUser.hasSuperPowers) {
            var newDryRun = EditorGUILayout.ToggleLeft(new GUIContent("Dry Run Builds", "This will skip actual builds and uploads and instead pass as if they succeeded"), VRC_EditorTools.DryRunState);
            if (newDryRun != VRC_EditorTools.DryRunState)
            {
                VRC_EditorTools.DryRunState = newDryRun;
            }
        }

        EditorGUILayout.Space();

        // DPID based mipmap generation
        bool prevDpidMipmaps = VRCPackageSettings.Instance.dpidMipmaps;
        GUIContent dpidContent = new GUIContent("Override Kaiser mipmapping with Detail-Preserving Image Downscaling (BETA)", 
                "Use a state of the art algorithm (DPID) for mipmap generation when Kaiser is selected. This can improve the quality of mipmaps.");
        VRCPackageSettings.Instance.dpidMipmaps = EditorGUILayout.ToggleLeft(dpidContent, VRCPackageSettings.Instance.dpidMipmaps);

        bool prevDpidConservative = VRCPackageSettings.Instance.dpidConservative;
        GUIContent dpidConservativeContent = new GUIContent("Use conservative settings for DPID mipmapping", 
                "Use conservative settings for DPID mipmapping. This can avoid issues with over-emphasis of details.");
        VRCPackageSettings.Instance.dpidConservative = EditorGUILayout.ToggleLeft(dpidConservativeContent, VRCPackageSettings.Instance.dpidConservative);
        
        // When DPID setting changed, mark all textures as dirty
        if (VRCPackageSettings.Instance.dpidMipmaps != prevDpidMipmaps || 
                (VRCPackageSettings.Instance.dpidMipmaps && VRCPackageSettings.Instance.dpidConservative != prevDpidConservative))
        {
            VRC.Core.Logger.Log("DPID mipmaps setting changed, marking all textures as dirty");
            string[] guids = AssetDatabase.FindAssets("t:Texture");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.mipmapFilter == TextureImporterMipFilter.KaiserFilter)
                {
                    importer.SaveAndReimport();
                }
            }

            VRCPackageSettings.Instance.Save();
        }
        
        EditorGUILayout.Space();

        // Running VRChat constraints in edit mode
        bool prevVrcConstraintsInEditMode = VRCSettings.VrcConstraintsInEditMode;
        GUIContent vrcConstraintsInEditModeContent = new GUIContent("Execute VRChat Constraints in Edit Mode", 
            "Allow VRChat Constraints to run while Unity is in Edit mode.");
        VRCSettings.VrcConstraintsInEditMode = EditorGUILayout.ToggleLeft(vrcConstraintsInEditModeContent, prevVrcConstraintsInEditMode);

        if (VRCSettings.VrcConstraintsInEditMode != prevVrcConstraintsInEditMode)
        {
            VRC.Dynamics.VRCConstraintManager.CanExecuteConstraintJobsInEditMode = VRCSettings.VrcConstraintsInEditMode;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Separator();

        ShowSettingsOptionsForBuilders();

        // debugging
        if (APIUser.CurrentUser != null && APIUser.CurrentUser.hasSuperPowers)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical(boxGuiStyle);

            EditorGUILayout.LabelField("Logging", EditorStyles.boldLabel);

            // All logging
            {
                bool isLoggingEnabled = UnityEditor.EditorPrefs.GetBool("allLoggingEnabled");
                bool enableLogging = EditorGUILayout.ToggleLeft("All Logging Enabled", isLoggingEnabled);
                if (enableLogging != isLoggingEnabled)
                {
                    if (enableLogging)
                        VRC.Core.Logger.EnableCategory(DebugLevel.All);
                    else
                        VRC.Core.Logger.DisableCategory(DebugLevel.All);

                    UnityEditor.EditorPrefs.SetBool("allLoggingEnabled", enableLogging);
                }
            }
            EditorGUILayout.EndVertical();
        }
        else
        {
            // if (UnityEditor.EditorPrefs.GetBool("apiLoggingEnabled"))
            //     UnityEditor.EditorPrefs.SetBool("apiLoggingEnabled", false);
            if (UnityEditor.EditorPrefs.GetBool("allLoggingEnabled"))
                UnityEditor.EditorPrefs.SetBool("allLoggingEnabled", false);
        }


        if (APIUser.CurrentUser != null)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical(boxGuiStyle);

            // custom vrchat install location
            OnVRCInstallPathGUI();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    static void OnVRCInstallPathGUI()
    {
        EditorGUILayout.LabelField("VRChat Client", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Installed Client Path: ", clientInstallPath);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("");
        if (GUILayout.Button("Edit"))
        {
            string initPath = "";
            if (!string.IsNullOrEmpty(clientInstallPath))
                initPath = clientInstallPath;

            clientInstallPath = EditorUtility.OpenFilePanel("Choose VRC Client Exe", initPath, "exe");
            SDKClientUtilities.SetVRCInstallPath(clientInstallPath);
        }
        if (GUILayout.Button("Revert to Default"))
        {
            clientInstallPath = SDKClientUtilities.LoadRegistryVRCInstallPath();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Separator();
    }
}
