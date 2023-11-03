using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using VRC.Core;
using VRC.Editor;
using VRC.SDKBase.Validation.Performance;
using Object = UnityEngine.Object;
using VRC.SDKBase.Editor;

/// This file handles the Build tab of the SDK Panel
/// It finds currently available builder via an attribute and a common interface, checks its validity and displays its UI
/// The actual Builder implementations are contained in their own Packages to avoid coupling
///
/// It is also responsible for collecting and drawing validation messages

public partial class VRCSdkControlPanel : EditorWindow
{
    public static System.Action _EnableSpatialization = null;   // assigned in AutoAddONSPAudioSourceComponents

    const string kCantPublishContent = "Before you can upload avatars or worlds, you will need to spend some time in VRChat.";
    const string kCantPublishAvatars = "Before you can upload avatars, you will need to spend some time in VRChat.";
    const string kCantPublishWorlds = "Before you can upload worlds, you will need to spend some time in VRChat.";
    private const string FIX_ISSUES_TO_BUILD_OR_TEST_WARNING_STRING = "You must address the above issues before you can build or test this content!";
    private readonly Dictionary<string, string> BUILD_TARGET_ICONS = new Dictionary<string, string>
    {
        {"Windows", "windows-icon"},
        {"Android", "android-icon"},
        {"iOS", "ios-icon"}
    };

    public static readonly Dictionary<string, string> CONTENT_PLATFORMS_MAP = new Dictionary<string, string>
    {
        {"android", "Android"},
        {"standalonewindows", "Windows"},
        {"ios", "iOS"}
    };

    #region Validations
    
    private VisualElement _validationsContainer;
    private VisualElement _descriptorErrorBlock;
    
    static Texture _perfIcon_Excellent;
    static Texture _perfIcon_Good;
    static Texture _perfIcon_Medium;
    static Texture _perfIcon_Poor;
    static Texture _perfIcon_VeryPoor;
    static Texture _bannerImage;

    public void ResetIssues()
    {
        GUIErrors.Clear();
        GUIInfos.Clear();
        GUIWarnings.Clear();
        GUILinks.Clear();
        GUIStats.Clear();
        CheckedForIssues = false;
    }

    public bool CheckedForIssues { get; set; } = false;

    public class Issue
    {
        public string issueText;
        public System.Action showThisIssue;
        public System.Action fixThisIssue;
        public PerformanceRating performanceRating;

        public Issue(string text, System.Action show, System.Action fix, PerformanceRating rating = PerformanceRating.None)
        {
            issueText = text;
            showThisIssue = show;
            fixThisIssue = fix;
            performanceRating = rating;
        }

        public class Equality : IEqualityComparer<Issue>, IComparer<Issue>
        {
            public bool Equals(Issue b1, Issue b2)
            {
                return (b1.issueText == b2.issueText);
            }
            public int Compare(Issue b1, Issue b2)
            {
                return string.Compare(b1.issueText, b2.issueText);
            }
            public int GetHashCode(Issue bx)
            {
                return bx.issueText.GetHashCode();
            }
        }
    }

    Dictionary<Object, List<Issue>> GUIErrors = new Dictionary<Object, List<Issue>>();
    Dictionary<Object, List<Issue>> GUIWarnings = new Dictionary<Object, List<Issue>>();
    Dictionary<Object, List<Issue>> GUIInfos = new Dictionary<Object, List<Issue>>();
    Dictionary<Object, List<Issue>> GUILinks = new Dictionary<Object, List<Issue>>();
    Dictionary<Object, List<Issue>> GUIStats = new Dictionary<Object, List<Issue>>();

    public bool NoGuiErrors()
    {
        return GUIErrors.Count == 0;
    }

    public bool NoGuiErrorsOrIssues()
    {
        return GUIErrors.Count == 0 && CheckedForIssues;
    }

    // Null objects (i.e. (Object) null) are allowed here, those correspond to project-wide issues which we should also report
    public bool NoGuiErrorsOrIssuesForItem(Object item)
    {
        return (!GUIErrors.TryGetValue(item, out var guiError) || guiError.Count == 0) && CheckedForIssues;
    }
    
    public List<Issue> GetGuiErrorsOrIssuesForItem(Object item)
    {
        if (!GUIErrors.TryGetValue(item, out var guiError))
        {
            return new List<Issue>();
        }

        return guiError;
    }

    void AddToReport(Dictionary<Object, List<Issue>> report, Object subject, string output, System.Action show, System.Action fix)
    {
        if (subject == null)
            subject = this;
        if (!report.ContainsKey(subject))
            report.Add(subject, new List<Issue>());

        var issue = new Issue(output, show, fix);
        if (!report[subject].Contains(issue, new Issue.Equality()))
        {
            report[subject].Add(issue);
            report[subject].Sort(new Issue.Equality());
        }
    }

    void BuilderAssemblyReload()
    {
        ResetIssues();
    }

    public void OnGUIError(Object subject, string output, System.Action show, System.Action fix)
    {
        AddToReport(GUIErrors, subject, output, show, fix);
    }

    public void OnGUIWarning(Object subject, string output, System.Action show, System.Action fix)
    {
        AddToReport(GUIWarnings, subject, output, show, fix);
    }

    public void OnGUIInformation(Object subject, string output)
    {
        AddToReport(GUIInfos, subject, output, null, null);
    }

    public void OnGUILink(Object subject, string output, string link)
    {
        AddToReport(GUILinks, subject, output + "\n" + link, null, null);
    }

    public void OnGUIStat(Object subject, string output, PerformanceRating rating, System.Action show, System.Action fix)
    {
        if (subject == null)
            subject = this;
        if (!GUIStats.ContainsKey(subject))
            GUIStats.Add(subject, new List<Issue>());
        GUIStats[subject].Add(new Issue(output, show, fix, rating));
    }
    
    public bool showLayerHelp = false;

    bool ShouldShowLightmapWarning
    {
        get
        {
            const string GraphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";
            SerializedObject graphicsManager = new SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsAssetPath)[0]);
            SerializedProperty lightmapStripping = graphicsManager.FindProperty("m_LightmapStripping");
            return lightmapStripping.enumValueIndex == 0;
        }
    }

    bool ShouldShowFogWarning
    {
        get
        {
            const string GraphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";
            SerializedObject graphicsManager = new SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsAssetPath)[0]);
            SerializedProperty lightmapStripping = graphicsManager.FindProperty("m_FogStripping");
            return lightmapStripping.enumValueIndex == 0;
        }
    }

    void DrawIssueBox(MessageType msgType, Texture icon, string message, System.Action show, System.Action fix)
    {
        bool haveButtons = ((show != null) || (fix != null));

        GUIStyle style = new GUIStyle("HelpBox");
        style.fixedWidth = (haveButtons ? (SdkWindowWidth - 110) : SdkWindowWidth - 20);
        float minHeight = 40;

        try
        {
            EditorGUILayout.BeginHorizontal();
            if (icon != null)
            {
                GUIContent c = new GUIContent(message, icon);
                float height = style.CalcHeight(c, style.fixedWidth);
                GUILayout.Box(c, style, GUILayout.MinHeight(Mathf.Max(minHeight, height)));
            }
            else
            {
                GUIContent c = new GUIContent(message);
                float height = style.CalcHeight(c, style.fixedWidth);
                Rect rt = GUILayoutUtility.GetRect(c, style, GUILayout.MinHeight(Mathf.Max(minHeight, height)));
                EditorGUI.HelpBox(rt, message, msgType);    // note: EditorGUILayout resulted in uneven button layout in this case
            }

            if (haveButtons)
            {
                EditorGUILayout.BeginVertical();
                float buttonHeight = ((show == null || fix == null) ? minHeight : (minHeight * 0.5f));
                if ((show != null) && GUILayout.Button("Select", GUILayout.Height(buttonHeight)))
                    show();
                if ((fix != null) && GUILayout.Button("Auto Fix", GUILayout.Height(buttonHeight)))
                {
                    fix();
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    CheckedForIssues = false;
                    Repaint();
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }
        catch
        {
            // mutes 'ArgumentException: Getting control 0's position in a group with only 0 controls when doing repaint'
        }
    }

    public void OnGuiFixIssuesToBuildOrTest()
    {
        EditorGUILayout.Space(5);
        GUIStyle s = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        GUILayout.BeginVertical(boxGuiStyle, GUILayout.Height(WARNING_ICON_SIZE));
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        var textDimensions = s.CalcSize(new GUIContent(FIX_ISSUES_TO_BUILD_OR_TEST_WARNING_STRING));
        GUILayout.Label(new GUIContent(warningIconGraphic), GUILayout.Width(WARNING_ICON_SIZE), GUILayout.Height(WARNING_ICON_SIZE));
        EditorGUILayout.LabelField(FIX_ISSUES_TO_BUILD_OR_TEST_WARNING_STRING, s, GUILayout.Height(WARNING_ICON_SIZE));
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
    }

    public void OnGUIShowIssues(Object subject = null)
    {
        if (subject == null)
            subject = this;

        EditorGUI.BeginChangeCheck();

        GUIStyle style = GUI.skin.GetStyle("HelpBox");

        if (GUIErrors.ContainsKey(subject))
            foreach (Issue error in GUIErrors[subject].Where(s => !string.IsNullOrEmpty(s.issueText)))
                DrawIssueBox(MessageType.Error, null, error.issueText, error.showThisIssue, error.fixThisIssue);
        if (GUIWarnings.ContainsKey(subject))
            foreach (Issue error in GUIWarnings[subject].Where(s => !string.IsNullOrEmpty(s.issueText)))
                DrawIssueBox(MessageType.Warning, null, error.issueText, error.showThisIssue, error.fixThisIssue);

        if (GUIStats.ContainsKey(subject))
        {
            foreach (var kvp in GUIStats[subject].Where(k => k.performanceRating == PerformanceRating.VeryPoor))
                DrawIssueBox(MessageType.Warning, GetPerformanceIconForRating(kvp.performanceRating), kvp.issueText, kvp.showThisIssue, kvp.fixThisIssue);

            foreach (var kvp in GUIStats[subject].Where(k => k.performanceRating == PerformanceRating.Poor))
                DrawIssueBox(MessageType.Warning, GetPerformanceIconForRating(kvp.performanceRating), kvp.issueText, kvp.showThisIssue, kvp.fixThisIssue);

            foreach (var kvp in GUIStats[subject].Where(k => k.performanceRating == PerformanceRating.Medium))
                DrawIssueBox(MessageType.Warning, GetPerformanceIconForRating(kvp.performanceRating), kvp.issueText, kvp.showThisIssue, kvp.fixThisIssue);

            foreach (var kvp in GUIStats[subject].Where(k => k.performanceRating == PerformanceRating.Good || k.performanceRating == PerformanceRating.Excellent))
                DrawIssueBox(MessageType.Warning, GetPerformanceIconForRating(kvp.performanceRating), kvp.issueText, kvp.showThisIssue, kvp.fixThisIssue);
        }

        if (GUIInfos.ContainsKey(subject))
            foreach (Issue error in GUIInfos[subject].Where(s => !string.IsNullOrEmpty(s.issueText)))
                EditorGUILayout.HelpBox(error.issueText, MessageType.Info);
        if (GUILinks.ContainsKey(subject))
        {
            EditorGUILayout.BeginVertical(style);
            foreach (Issue error in GUILinks[subject].Where(s => !string.IsNullOrEmpty(s.issueText)))
            {
                var s = error.issueText.Split('\n');
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(s[0]);
                if (GUILayout.Button("Open Link", GUILayout.Width(100)))
                    Application.OpenURL(s[1]);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(subject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
    
    #endregion

    // Renders any builder-specific options on the settings panel
    private void ShowSettingsOptionsForBuilders()
    {
        if (_sdkBuilders == null)
        {
            PopulateSdkBuilders();
        }
        for (int i = 0; i < _sdkBuilders.Length; i++)
        {
            IVRCSdkControlPanelBuilder builder = _sdkBuilders[i];
            builder.ShowSettingsOptions();
            if (i < _sdkBuilders.Length - 1)
            {
                EditorGUILayout.Separator();
            }
        }
    }

    private IVRCSdkControlPanelBuilder[] _sdkBuilders;

    private static List<Type> GetSdkBuilderTypesFromAttribute()
    {
        Type sdkBuilderInterfaceType = typeof(IVRCSdkControlPanelBuilder);
        Type sdkBuilderAttributeType = typeof(VRCSdkControlPanelBuilderAttribute);

        List<Type> moduleTypesFromAttribute = new List<Type>();
        foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            VRCSdkControlPanelBuilderAttribute[] sdkBuilderAttributes;
            try
            {
                sdkBuilderAttributes = (VRCSdkControlPanelBuilderAttribute[])assembly.GetCustomAttributes(sdkBuilderAttributeType, true);
            }
            catch
            {
                sdkBuilderAttributes = new VRCSdkControlPanelBuilderAttribute[0];
            }

            foreach(VRCSdkControlPanelBuilderAttribute udonWrapperModuleAttribute in sdkBuilderAttributes)
            {
                if(udonWrapperModuleAttribute == null)
                {
                    continue;
                }

                if(!sdkBuilderInterfaceType.IsAssignableFrom(udonWrapperModuleAttribute.Type))
                {
                    continue;
                }

                moduleTypesFromAttribute.Add(udonWrapperModuleAttribute.Type);
            }
        }

        return moduleTypesFromAttribute;
    }

    private void PopulateSdkBuilders()
    {
        if (_sdkBuilders != null)
        {
            return;
        }
        List<IVRCSdkControlPanelBuilder> builders = new List<IVRCSdkControlPanelBuilder>();
        foreach (Type type in GetSdkBuilderTypesFromAttribute())
        {
            IVRCSdkControlPanelBuilder builder = (IVRCSdkControlPanelBuilder)Activator.CreateInstance(type);
            builder.RegisterBuilder(this);
            builders.Add(builder);
        }
        _sdkBuilders = builders.ToArray();
    }

    public static bool TryGetBuilder<T>(out T builder) where T : IVRCSdkBuilderApi
    {
        if (window == null)
        {
            Debug.LogError("Cannot get builder, SDK window is not open");
            builder = default;
            return false;
        }
        
        builder = default;
        
        if (window._sdkBuilders == null)
        {
            window.PopulateSdkBuilders();
        }

        if (window._sdkBuilders == null)
        {
            Debug.LogError("Could not find valid sdk builders");
            builder = default;
            return false;
        }
        
        foreach (var sdkBuilder in window._sdkBuilders)
        {
            if (sdkBuilder.GetType().GetInterface(typeof(T).Name) == null) continue;
            try
            {
                builder = (T) sdkBuilder;
                return true;
            }
            catch
            {
                // continue looking for a builder
            }
        }

        return false;
    }
    

    // Validates scene and project settings
    private void CheckProjectSetup()
    {
        if (VRC.Core.ConfigManager.RemoteConfig.IsInitialized())
        {
            string sdkUnityVersion = VRC.Core.ConfigManager.RemoteConfig.GetString("sdkUnityVersion");
            if (Application.unityVersion != sdkUnityVersion)
            {
                OnGUIWarning(null,
                    "You are not using the recommended Unity version for the VRChat SDK. Content built with this version may not work correctly. Please use Unity " +
                    sdkUnityVersion,
                    null,
                    () => { Application.OpenURL("https://unity3d.com/get-unity/download/archive"); }
                );
            }
        }

        if (VRCSdk3Analysis.IsSdkDllActive(VRCSdk3Analysis.SdkVersion.VRCSDK2) &&
            VRCSdk3Analysis.IsSdkDllActive(VRCSdk3Analysis.SdkVersion.VRCSDK3))
        {
            List<Component> sdk2Components = VRCSdk3Analysis.GetSDKInScene(VRCSdk3Analysis.SdkVersion.VRCSDK2);
            List<Component> sdk3Components = VRCSdk3Analysis.GetSDKInScene(VRCSdk3Analysis.SdkVersion.VRCSDK3);
            if (sdk2Components.Count > 0 && sdk3Components.Count > 0)
            {
                OnGUIError(null,
                    "This scene contains components from the VRChat SDK version 2 and version 3. Version two elements will have to be replaced with their version 3 counterparts to build with SDK3 and UDON.",
                    () => { Selection.objects = sdk2Components.ToArray(); },
                    null
                );
            }
        }

        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
            EditorUserBuildSettings.androidBuildSubtarget != MobileTextureSubtarget.ASTC)
        {
            OnGUIError(null,
                "Default texture format on Android should be set to the newer ASTC format to reduce VRAM usage (this could take a while). Texture settings can be overridden on an individual basis.",
                null,
                () =>
                {
                    // This will be reimported automatically on build.
                    EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
                }
            );
        }

        if (ApiUserPlatforms.CurrentUserPlatforms != null && !ApiUserPlatforms.CurrentUserPlatforms.SupportsiOS)
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                OnGUIError(null,
                    "iOS is not supported as a build target.",
                    null,
                    null
                );
            }
        }

        if (Lightmapping.giWorkflowMode == Lightmapping.GIWorkflowMode.Iterative)
        {
            OnGUIWarning(null,
                "Automatic lightmap generation is enabled, which may stall the Unity build process. Before building and uploading, consider turning off 'Auto Generate' at the bottom of the Lighting Window.",
                () =>
                {
                    EditorWindow lightingWindow = GetLightingWindow();
                    if (lightingWindow)
                    {
                        lightingWindow.Show();
                        lightingWindow.Focus();
                    }
                },
                () =>
                {
                    Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
                    EditorWindow lightingWindow = GetLightingWindow();
                    if (!lightingWindow) return;
                    lightingWindow.Repaint();
                    Focus();
                }
            );
        }
    }

    private void ShowBuilders()
    {
        
        if (_builderPanelLayout == null)
        {
            _builderPanelLayout = Resources.Load<VisualTreeAsset>("VRCSdkBuilderLayout");
        }

        if (_builderPanelStyles == null)
        {
            _builderPanelStyles = Resources.Load<StyleSheet>("VRCSdkBuilderStyles");
        }

        _builderPanelLayout.CloneTree(_builderPanel);
        _builderPanel.styleSheets.Add(_builderPanelStyles);

        _validationsContainer = _builderPanel.Q("validations-block");
        _descriptorErrorBlock = _builderPanel.Q("descriptor-error");
        _builderNotificationBlock = _builderPanel.Q("builder-notification");
        _builderNotificationTitle = _builderPanel.Q<Label>("builder-notification-title");
        _builderNotificationContent = _builderPanel.Q("builder-notification-content");
        _builderNotificationDismiss = _builderPanel.Q("builder-notification-dismiss");
        
        _builderNotificationDismiss.RegisterCallback<MouseDownEvent>(async evt =>
        {
            await DismissNotification();
        });

        CleanUpPipelineSavers();

        _builderPanel.schedule.Execute(() =>
        {
            // scheduled job running when the UI has unmounted
            if (_builderPanel == null || _builderPanel.childCount == 0)
            {
                return;
            }
            PopulateSdkBuilders();
            
            IVRCSdkControlPanelBuilder selectedBuilder = null;
            string errorMessage = null;

            // Grab the first valid builder, and if all builders are invalid then errorMessage will contain the last error.
            foreach (IVRCSdkControlPanelBuilder sdkBuilder in _sdkBuilders)
            {
                if (sdkBuilder.IsValidBuilder(out string message))
                {
                    selectedBuilder = sdkBuilder;
                    errorMessage = null;
                    break;
                }
                else
                {
                    errorMessage = message;
                }
            }

            if (selectedBuilder == null)
            {
                _descriptorErrorBlock.RemoveFromClassList("d-none");
                _builderPanel.Q("content-info-block")?.Clear();
                _validationsContainer?.Clear();
                _builderPanel.Q("info-section")?.AddToClassList("d-none");
                _builderPanel.Q("build-section")?.AddToClassList("d-none");
                var builderErrorBlock = _descriptorErrorBlock.Q("descriptor-builder-error");
                var errorText = _descriptorErrorBlock.Q<Label>("descriptor-error-text");
                
                // if there is a builder discovered, let the builder create a more specific error message
                if (_sdkBuilders.Length == 1)
                {
                    // only add the GUI if there is not already a GUI
                    if (builderErrorBlock.childCount != 0) return;
                    _sdkBuilders[0].CreateBuilderErrorGUI(builderErrorBlock);
                    errorText.AddToClassList("d-none");
                    return;
                }
                
                string message = "";
#if UDON
                message = "A VRCSceneDescriptor is required to build a World";
#elif VRC_SDK_VRCSDK3
                message = "A VRCAvatarDescriptor is required to build an Avatar";
#else
                message = "The SDK did not load properly. Try selecting VRChat SDK -> Reload SDK in the Unity menu bar.";
#endif
                errorText.RemoveFromClassList("d-none");
                errorText.text = message;
                return;
            }

            _descriptorErrorBlock.AddToClassList("d-none");

            if (errorMessage != null)
            {
                _builderPanel.Q("content-info-block")?.Clear();
                _validationsContainer.Clear();
                _builderPanel.Q("build-section").AddToClassList("d-none");
                
                OnGUIError(null,
                    errorMessage,
                    () => {
                        foreach (IVRCSdkControlPanelBuilder builder in _sdkBuilders)
                        {
                            builder.SelectAllComponents();
                        } },
                    null
                );
            
                _validationsContainer.Add(new IMGUIContainer(() =>
                {
                    using (new GUILayout.VerticalScope())
                    {
                        CheckProjectSetup();
                        OnGUIShowIssues();
                    }
                }));
                return;
            }

            // Draw content info
            _builderPanel.Q("info-section").RemoveFromClassList("d-none");
            _builderPanel.Q("build-section").RemoveFromClassList("d-none");
            var infoBlock = _builderPanel.Q("content-info-block");
            infoBlock.RemoveFromClassList("d-none");
            if (infoBlock.childCount == 0)
            {
                selectedBuilder.CreateContentInfoGUI(infoBlock);
            }

            // Draw platform switcher
            var switcherBlock = _builderPanel.Q("platform-switcher");
            if (switcherBlock.childCount == 0)
            {
                {
                    var options = GetBuildTargetOptions();
                    var currentTarget = GetCurrentBuildTarget();
                    var selectedIndex = options.IndexOf(currentTarget);
                    if (!BUILD_TARGET_ICONS.TryGetValue(currentTarget, out var iconClass))
                    {
                        iconClass = "";
                    }
                    if (selectedIndex == -1)
                    {
                        selectedIndex = 0;
                    }
                    var popup = new PopupField<string>("Selected Platform", options, selectedIndex)
                    {
                        name = "platform-switcher-popup"
                    };
                    var icon = new VisualElement();
                    icon.AddToClassList("icon");
                    icon.AddToClassList(iconClass);
                    
                    popup.hierarchy.Insert(0, icon);
                    popup.schedule.Execute(() =>
                    {
                        currentTarget = GetCurrentBuildTarget();
                        popup.SetValueWithoutNotify(currentTarget);
                    }).Every(500);
                    popup.RegisterValueChangedCallback(evt =>
                    {
                        switch (evt.newValue)
                        {
                            case "Windows":
                            {
                                if (EditorUtility.DisplayDialog("Build Target Switcher", "Are you sure you want to switch your build target to Windows? This could take a while.", "Confirm", "Cancel"))
                                {
                                    EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Standalone;
                                    EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                                }

                                break;
                            }
                            case "Android":
                            {
                                if (EditorUtility.DisplayDialog("Build Target Switcher", "Are you sure you want to switch your build target to Android? This could take a while.", "Confirm", "Cancel"))
                                {
                                    EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
                                    EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android);
                                }

                                break;
                            }
                            case "iOS":
                            {
                                if (ApiUserPlatforms.CurrentUserPlatforms?.SupportsiOS != true) return;
                                if (EditorUtility.DisplayDialog("Build Target Switcher", "Are you sure you want to switch your build target to iOS? This could take a while.", "Confirm", "Cancel"))
                                {
                                    EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.iOS;
                                    EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.iOS, BuildTarget.iOS);
                                }
                                
                                break;
                            }
                        }
                    });
                    popup.AddToClassList("flex-grow-1");
                    switcherBlock.Add(popup);
                }
            }

            // Draw validations
            if (_validationsContainer.childCount == 0)
            {
                // Execute the code of the discovered builder class
                _validationsContainer.Add(new IMGUIContainer(() =>
                {
                    CheckProjectSetup();
                    using (new GUILayout.VerticalScope())
                    {
                        selectedBuilder.ShowBuilder();
                    }
                }));
            }
            
            // Draw build panel
            var buildBlock = _builderPanel.Q("build-panel-gui");
            if (buildBlock.childCount == 0)
            {
                selectedBuilder.CreateBuildGUI(buildBlock);
            }
        }).Every(1000);
    }

    private static void CleanUpPipelineSavers()
    {
        #pragma warning disable CS0618 // Disabled obsolete warnings because we're trying to get rid of the pipelineSavers in the scene
        var pipelineSavers = FindObjectsOfType<PipelineSaver>();
        foreach (var saver in pipelineSavers)
        {
            Undo.DestroyObjectImmediate(saver);
        }
        #pragma warning restore CS0618
    }

    private bool _notificationShown = false;
    private VisualElement _builderNotificationBlock;
    private VisualElement _builderNotificationContent;
    private Label _builderNotificationTitle;
    private VisualElement _builderNotificationDismiss;
    private string _builderNotificationTitleColorClass;

    public async Task ShowBuilderNotification(string title, VisualElement content, string titleColor = null, int timeout = 0)
    {
        if (_notificationShown)
        {
            await DismissNotification();
        }

        _builderNotificationTitle.text = title;

        _builderNotificationTitle.RemoveFromClassList(_builderNotificationTitleColorClass);
        _builderNotificationTitleColorClass = null;
        
        if (!string.IsNullOrWhiteSpace(titleColor))
        {
            _builderNotificationTitleColorClass = $"text-{titleColor}";
            _builderNotificationTitle.AddToClassList(_builderNotificationTitleColorClass);
        }

        _builderNotificationContent.Clear();
        _builderNotificationContent.Add(content);

        _notificationShown = true;

        _builderNotificationBlock.experimental.animation.Start(new StyleValues {bottom = -500},
            new StyleValues {bottom = 0}, 500);

        if (timeout > 0)
        {
// we do not want to await this task, so we disable the warning
#pragma warning disable CS4014
            Task.Run(async () =>
            {
                await Task.Delay(timeout);
                DismissNotification();
            });
#pragma warning restore CS4014
        }
    }

    public async Task DismissNotification()
    {
        if (!_notificationShown) return;
        await UniTask.SwitchToMainThread();
        _builderNotificationBlock.experimental.animation.Start(new StyleValues {bottom = 0},
            new StyleValues {bottom = -500}, 500);
        _notificationShown = false;
        await Task.Delay(500);
    }

    private Texture GetPerformanceIconForRating(PerformanceRating value)
    {
        if (_perfIcon_Excellent == null)
            _perfIcon_Excellent = Resources.Load<Texture>("PerformanceIcons/Perf_Great_32");
        if (_perfIcon_Good == null)
            _perfIcon_Good = Resources.Load<Texture>("PerformanceIcons/Perf_Good_32");
        if (_perfIcon_Medium == null)
            _perfIcon_Medium = Resources.Load<Texture>("PerformanceIcons/Perf_Medium_32");
        if (_perfIcon_Poor == null)
            _perfIcon_Poor = Resources.Load<Texture>("PerformanceIcons/Perf_Poor_32");
        if (_perfIcon_VeryPoor == null)
            _perfIcon_VeryPoor = Resources.Load<Texture>("PerformanceIcons/Perf_Horrible_32");

        switch (value)
        {
            case PerformanceRating.Excellent:
                return _perfIcon_Excellent;
            case PerformanceRating.Good:
                return _perfIcon_Good;
            case PerformanceRating.Medium:
                return _perfIcon_Medium;
            case PerformanceRating.Poor:
                return _perfIcon_Poor;
            case PerformanceRating.None:
            case PerformanceRating.VeryPoor:
                return _perfIcon_VeryPoor;
        }

        return _perfIcon_Excellent;
    }

    private List<string> GetBuildTargetOptions()
    {
        var options = new List<string>
        {
            "Windows",
            "Android"
        };
        if (ApiUserPlatforms.CurrentUserPlatforms?.SupportsiOS == true)
        {
            options.Add("iOS");
        }

        return options;
    }

    private string GetCurrentBuildTarget()
    {
        string currentTarget;
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                currentTarget = "Windows";
                break;
            case BuildTarget.Android:
                currentTarget = "Android";
                break;
            case BuildTarget.iOS:
                currentTarget = "iOS";
                break;
            default:
                currentTarget = "Unsupported Target Platform";
                break;
        }

        return currentTarget;
    }

    Texture2D CreateBackgroundColorImage(UnityEngine.Color color)
    {
        int w = 4, h = 4;
        Texture2D back = new Texture2D(w, h);
        UnityEngine.Color[] buffer = new UnityEngine.Color[w * h];
        for (int i = 0; i < w; ++i)
            for (int j = 0; j < h; ++j)
                buffer[i + w * j] = color;
        back.SetPixels(buffer);
        back.Apply(false);
        return back;
    }

    public static void DrawBuildTargetSwitcher()
    {
        EditorGUILayout.LabelField("Active Build Target: " + EditorUserBuildSettings.activeBuildTarget);

        if (GUILayout.Button("Switch Build Target"))
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Windows"), EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64,
                () =>
                {
                    if (EditorUtility.DisplayDialog("Build Target Switcher", "Are you sure you want to switch your build target to Windows? This could take a while.", "Confirm", "Cancel"))
                    {
                        EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Standalone;
                        EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                    }
                });
            
            menu.AddItem(new GUIContent("Android"), EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android,
                () =>
                {
                    if (EditorUtility.DisplayDialog("Build Target Switcher", "Are you sure you want to switch your build target to Android? This could take a while.", "Confirm", "Cancel"))
                    {
                        EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
                        EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android);
                    }
                });

            if (ApiUserPlatforms.CurrentUserPlatforms?.SupportsiOS == true)
            {
                menu.AddItem(new GUIContent("iOS"), EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS,
                    () =>
                    {
                        if (EditorUtility.DisplayDialog("Build Target Switcher", "Are you sure you want to switch your build target to iOS? This could take a while.", "Confirm", "Cancel"))
                        {
                            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.iOS;
                            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.iOS, BuildTarget.iOS);
                        }
                    });
            }

            menu.ShowAsContext();
        }
    }

    public static string GetBuildAndPublishButtonString()
    {
        string buildButtonString = "Build & Publish for UNSUPPORTED";
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
            buildButtonString = "Build & Publish for Windows";
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            buildButtonString = "Build & Publish for Android";
        if (ApiUserPlatforms.CurrentUserPlatforms?.SupportsiOS == true)
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
                buildButtonString = "Build & Publish for iOS";
        }

        return buildButtonString;
    }

    public static Object[] GetSubstanceObjects(GameObject obj = null, bool earlyOut = false)
    {
        // if 'obj' is null we check entire scene
        // if 'earlyOut' is true we only return 1st object (to detect if substances are present)

        List<Object> objects = new List<Object>();
        if (obj == null) return objects.Count < 1 ? null : objects.ToArray();
        Renderer[] renderers = obj ? obj.GetComponentsInChildren<Renderer>(true) : FindObjectsOfType<Renderer>();

        if (renderers == null || renderers.Length < 1)
            return null;
        foreach (Renderer r in renderers)
        {
            if (r.sharedMaterials.Length < 1)
                continue;
            foreach (Material m in r.sharedMaterials)
            {
                if (!m)
                    continue;
                string path = AssetDatabase.GetAssetPath(m);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (path.EndsWith(".sbsar", true, System.Globalization.CultureInfo.InvariantCulture))
                {
                    objects.Add(r.gameObject);
                    if (earlyOut)
                        return objects.ToArray();
                }
            }
        }

        return objects.Count < 1 ? null : objects.ToArray();
    }

    public static bool HasSubstances(GameObject obj = null)
    {
        return (GetSubstanceObjects(obj, true) != null);
    }

    EditorWindow GetLightingWindow()
    {
        var editorAsm = typeof(UnityEditor.Editor).Assembly;
        return EditorWindow.GetWindow(editorAsm.GetType("UnityEditor.LightingWindow"));
    }

    public static void ShowContentPublishPermissionsDialog()
    {
        if (!VRC.Core.ConfigManager.RemoteConfig.IsInitialized())
        {
            VRC.Core.ConfigManager.RemoteConfig.Init(() => ShowContentPublishPermissionsDialog());
            return;
        }

        string message = VRC.Core.ConfigManager.RemoteConfig.GetString("sdkNotAllowedToPublishMessage");
        int result = UnityEditor.EditorUtility.DisplayDialogComplex("VRChat SDK", message, "Developer FAQ", "VRChat Discord", "OK");
        if (result == 0)
        {
            VRCSdkControlPanelHelp.ShowDeveloperFAQ();
        }
        if (result == 1)
        {
            VRCSdkControlPanelHelp.ShowVRChatDiscord();
        }
    }
}
