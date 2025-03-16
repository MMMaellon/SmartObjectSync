using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.PostProcessing;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VRC.Core;
using VRC.SDKBase.Validation.Performance;
using Object = UnityEngine.Object;
using VRC.SDKBase.Editor;
using VRC.SDKBase;
using VRC.SDKBase.Editor.Elements;
using Task = System.Threading.Tasks.Task;

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

    private Texture2D _defaultHeaderImage;

    #region Validations
    
    private VisualElement _validationsContainer;
    private VisualElement _descriptorErrorBlock;
    
    static Texture _perfIcon_Excellent;
    static Texture _perfIcon_Good;
    static Texture _perfIcon_Medium;
    static Texture _perfIcon_Poor;
    static Texture _perfIcon_VeryPoor;
    static Texture _bannerImage;
    
    public const int MAX_SDK_TEXTURE_SIZE = 8192;
    const int buildSectionHeightCollapseThreshold = 80;

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
                bool b1HasActions = b1.showThisIssue != null || b1.fixThisIssue != null;
                bool b2HasActions = b2.showThisIssue != null || b2.fixThisIssue != null;

                if (b1HasActions != b2HasActions)
                {
                    // Show messages with actions first
                    return b1HasActions ? -1 : 1;
                }
                
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

    public int GUIAlertCount(Object item)
    {
        GUIErrors.TryGetValue(item, out var guiError);
        GUIWarnings.TryGetValue(item, out var guiWarning);
        GUIInfos.TryGetValue(item, out var guiInfo);
        GUILinks.TryGetValue(item, out var guiLink);
        GUIStats.TryGetValue(item, out var guiStat);
        
        return (guiError?.Count ?? 0) + (guiWarning?.Count ?? 0) + (guiInfo?.Count ?? 0) + (guiLink?.Count ?? 0) + (guiStat?.Count ?? 0);
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

    public void OnGUIError(Object subject, string output, System.Action show = null, System.Action fix = null)
    {
        AddToReport(GUIErrors, subject, output, show, fix);
    }

    public void OnGUIWarning(Object subject, string output, System.Action show = null, System.Action fix = null)
    {
        AddToReport(GUIWarnings, subject, output, show, fix);
    }

    public void OnGUIInformation(Object subject, string output, System.Action show = null, System.Action fix = null)
    {
        AddToReport(GUIInfos, subject, output, show, fix);
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
    
    private VisualElement CreateIssueBoxGUI(Object subject, Issue issue, HelpBoxMessageType messageType = HelpBoxMessageType.None, Texture icon = null)
    {
        var haveButtons = ((issue.showThisIssue != null) || (issue.fixThisIssue != null));
        
        var container = new VisualElement();
        container.AddToClassList("row");
        container.AddToClassList("align-items-stretch");

        VisualElement message;
        if (icon != null)
        {
            message = new HelpBox(issue.issueText, HelpBoxMessageType.None);
            var iconElement = new Image { image = icon };
            iconElement.AddToClassList("mr-2");
            message.Insert(0, iconElement);
        }
        else
        {
            message = new HelpBox(issue.issueText, messageType);
        }

        // Expand the b
        if (!haveButtons)
        {
            message.AddToClassList("flex-1");
            container.Add(message);
            return container;
        }
        
        message.AddToClassList("flex-10");
        var buttonsContainer = new VisualElement();
        buttonsContainer.AddToClassList("flex-2");
        buttonsContainer.style.minWidth = 50;
        if (issue.showThisIssue != null)
        {
            var button = new Button(issue.showThisIssue)
            {
                text = "Select"
            };
            button.AddToClassList("flex-1");
            buttonsContainer.Add(button);
        }
        
        if (issue.fixThisIssue != null)
        {
            var button = new Button(() =>
            {
                issue.fixThisIssue();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorUtility.SetDirty(subject);
                RunValidations();
            })
            {
                text = "Auto Fix"
            };
            button.AddToClassList("flex-1");
            buttonsContainer.Add(button);
        }
        
        container.Add(message);
        container.Add(buttonsContainer);
        return container;
    }

    private VisualElement CreateLinkBoxGUI(Issue issue)
    {
        var s = issue.issueText.Split('\n');
        
        var message = new HelpBox(s[0], HelpBoxMessageType.None);
        message.AddToClassList("align-items-stretch");
        var text = message.Q<Label>();
        text.AddToClassList("flex-1");
        text.AddToClassList("ml-1");
        
        var button = new Button(() =>
        {
            Application.OpenURL(s[1]);
        })
        {
            text = "Open Link",
            style =
            {
                maxWidth = 100,
                minWidth = 50
            }
        };
        button.AddToClassList("flex-1");
        message.Add(button);

        return (VisualElement) message;
    }

    internal VisualElement CreateFixIssuesToBuildOrTestGUI()
    {
        var text = new Label(FIX_ISSUES_TO_BUILD_OR_TEST_WARNING_STRING);
        text.AddToClassList("text-center");
        text.AddToClassList("white-space-normal");

        var container = new HelpBox();
        container.AddToClassList("row");
        container.AddToClassList("align-items-center");
        container.Add(new Image
        {
            image = warningIconGraphic,
            style =
            {
                width = WARNING_ICON_SIZE,
                height = WARNING_ICON_SIZE
            }
        });
        container.Add(text);
        return container;
    }

    internal VisualElement CreateIssuesGUI(Object subject = null)
    {
        var container = new VisualElement
        {
            name = "issues-list"
        };
        
        if (subject == null) subject = this;

        if (GUIErrors.TryGetValue(subject, out var issues))
        {
            foreach (var issue in issues.Where(i => !string.IsNullOrWhiteSpace(i.issueText)))
            {
                container.Add(CreateIssueBoxGUI(subject, issue, HelpBoxMessageType.Error));
            }
        }
        
        if (GUIWarnings.TryGetValue(subject, out issues))
        {
            foreach (var issue in issues.Where(i => !string.IsNullOrWhiteSpace(i.issueText)))
            {
                container.Add(CreateIssueBoxGUI(subject, issue, HelpBoxMessageType.Warning));
            }
        }
        
        if (GUIStats.TryGetValue(subject, out issues))
        {
            // Stats need to be displayed in order from very poor to excellent
            var sortedIssues = issues.OrderBy(i => -(int) i.performanceRating);
            foreach (var issue in sortedIssues.Where(i => !string.IsNullOrWhiteSpace(i.issueText)))
            {
                container.Add(CreateIssueBoxGUI(subject, issue, HelpBoxMessageType.Warning, GetPerformanceIconForRating(issue.performanceRating)));
            }
        }
        
        if (GUIInfos.TryGetValue(subject, out issues))
        {
            foreach (var issue in issues.Where(i => !string.IsNullOrWhiteSpace(i.issueText)))
            {
                container.Add(CreateIssueBoxGUI(subject, issue, HelpBoxMessageType.Info));
            }
        }
        
        if (GUILinks.TryGetValue(subject, out issues))
        {
            foreach (var issue in issues.Where(i => !string.IsNullOrWhiteSpace(i.issueText)))
            {
                container.Add(CreateLinkBoxGUI(issue));
            }
        }
        

        return container;
    }
    #endregion

    // Refresh platform switcher post-login to grab latest allowed platforms
    private void RefreshPlatformSwitcher(object sender, ApiUserPlatforms userPlatforms)
    {
        rootVisualElement.schedule.Execute(() =>
        {
            var switcherBlock = _builderPanel?.Q<PlatformSwitcherPopup>("platform-switcher-popup");
            switcherBlock?.Refresh();
        }).ExecuteLater(300);
    }
    
    // Renders any builder-specific options on the settings panel
    private void ShowSettingsOptionsForBuilders()
    {
        if (_sdkBuilders == null)
        {
            PopulateSdkBuilders();
        }
        bool hasShownAnySettings = false;
        for (int i = 0; i < _sdkBuilders.Length; i++)
        {
            IVRCSdkControlPanelBuilder builder = _sdkBuilders[i];
            if (builder.IsValidBuilder(out _))
            {
                if (hasShownAnySettings)
                {
                    EditorGUILayout.Separator();
                }
                builder.ShowSettingsOptions();
                hasShownAnySettings = true;
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
    [UsedImplicitly]
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
                    AssetDatabase.Refresh();
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
        _builderNotificationDismiss = _builderPanel.Q<Button>("builder-notification-dismiss");

        _builderNotificationDismiss.clicked += () => DismissNotification().ConfigureAwait(false);

        CleanUpPipelineSavers();
        
        var infoFoldout = _builderPanel.Q<Foldout>("info-foldout");
        var validationsFoldout = _builderPanel.Q<Foldout>("validations-foldout");
        var validationsMounted = false;

        _builderPanel.schedule.Execute(() =>
        {
            // scheduled job running when the UI has unmounted
            if (_builderPanel == null || _builderPanel.childCount == 0)
            {
                return;
            }
            PopulateSdkBuilders();
            
            _selectedBuilder = null;
            string errorMessage = null;

            // Grab the first valid builder, and if all builders are invalid then errorMessage will contain the last error.
            foreach (IVRCSdkControlPanelBuilder sdkBuilder in _sdkBuilders)
            {
                if (sdkBuilder.IsValidBuilder(out string message))
                {
                    _selectedBuilder = sdkBuilder;
                    errorMessage = null;
                    break;
                }
                else
                {
                    errorMessage = message;
                }
            }

            if (_selectedBuilder == null)
            {
                _descriptorErrorBlock.RemoveFromClassList("d-none");
                _builderPanel.Q("content-info-block")?.Clear();
                _validationsContainer?.Clear();
                _builderPanel.Q("info-section")?.AddToClassList("d-none");
                _builderPanel.Q("build-section")?.AddToClassList("d-none");
                var builderErrorBlock = _descriptorErrorBlock.Q("descriptor-builder-error");
                var errorText = _descriptorErrorBlock.Q<Label>("descriptor-error-text");
                
                // if there is a builder discovered, let the builder create a more specific error message
                // if there are two builders discovered, we check if they implement the same interfaces, which will indicate that they're the same kind of builder
                if (_sdkBuilders.Length == 1 || (_sdkBuilders.Length == 2 && _sdkBuilders[0].GetType().GetInterfaces().SequenceEqual(_sdkBuilders[1].GetType().GetInterfaces())))
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
                
                _validationsContainer.Add(CreateIssuesGUI());
                return;
            }

            var headerImage = _selectedBuilder.GetHeaderImage();
            if (headerImage != null)
            {
                rootVisualElement.Q("banner").style.backgroundImage = headerImage;
            }
            else
            {
                if (_defaultHeaderImage == null)
                {
                    _defaultHeaderImage = Resources.Load<Texture2D>("SDK_Panel_Banner");
                }

                rootVisualElement.Q("banner").style.backgroundImage = _defaultHeaderImage;
            }

            // Draw content info
            _builderPanel.Q("info-section").RemoveFromClassList("d-none");
            _builderPanel.Q("build-section").RemoveFromClassList("d-none");
            var infoBlock = _builderPanel.Q("content-info-block");
            infoBlock.RemoveFromClassList("d-none");
            if (infoBlock.childCount == 0)
            {
                _selectedBuilder.Initialize();
                _selectedBuilder.CreateContentInfoGUI(infoBlock);
                
                validationsFoldout.style.maxHeight = new StyleLength(StyleKeyword.Auto);

                void MakeSpaceForUncollapse(bool infoSectionOpen, bool validationSectionOpen)
                {
                    float parentHeight = infoFoldout.parent.contentRect.height;
                        
                    float validationSectionHeight = validationsFoldout.contentRect.height;
                    float validationSectionContentHeight = validationsFoldout.contentContainer.contentRect.height;
                    float validationSectionTabHeight = validationSectionHeight - validationSectionContentHeight;
                        
                    float infoSectionHeight = infoFoldout.contentRect.height;
                    float infoSectionContentHeight = infoFoldout.contentContainer.contentRect.height;
                    float infoSectionTabHeight = infoSectionHeight - infoSectionContentHeight;
                        
                    float tabHeights = infoSectionTabHeight + validationSectionTabHeight;

                    float infoRequiredContentHeight = buildSectionHeightCollapseThreshold * (infoSectionOpen ? 1 : 0);
                    float validationRequiredContentHeight = buildSectionHeightCollapseThreshold * (validationSectionOpen ? 1 : 0);
                    if (infoSectionOpen && validationSectionOpen)
                    {
                        // Both are open, so now the info section will take up 2/3 of the space and validation will take up 1/3
                        validationRequiredContentHeight *= 3.0f / 2.0f;
                    }

                    float contentHeights = infoRequiredContentHeight + validationRequiredContentHeight;
                    float totalRequiredHeight = tabHeights + contentHeights * 1.1f; // Add 10% more content height just so it's not exactly on the collapse threshold
  
                    if (parentHeight < totalRequiredHeight)
                    {
                        var rect = window.position;
                        rect.height = window.position.height + Mathf.Max(totalRequiredHeight - parentHeight, 0);
                        window.position = rect;
                    }
                }
                infoFoldout.RegisterValueChangedCallback(evt =>
                {
                    validationsFoldout.style.maxHeight = evt.newValue ? new StyleLength(new Length(50, LengthUnit.Percent)) : new StyleLength(StyleKeyword.Auto);
                    if (evt.newValue)
                    {
                        MakeSpaceForUncollapse(true, validationsFoldout.value);
                    }
                });
                
                validationsFoldout.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                    {
                        MakeSpaceForUncollapse(infoFoldout.value, true);
                    }
                });
            }

            if (!validationsMounted)
            {
                validationsMounted = true;

                // Initial validations check
                RunValidations();
                
                // Re-Validate on World/Avatar selection change
                _selectedBuilder.OnContentChanged += (_, _) =>
                {
                    RunValidations();
                };
                // Re-validate on forced revalidations, e.g. project settings adjustment, etc
                _selectedBuilder.OnShouldRevalidate += (_, _) =>
                {
                    RunValidations();
                };

                // Re-validate on unity change publish
                // Make sure we don't call RunValidations too often
                var debouncedChangeCheck = new DebouncedCall(TimeSpan.FromSeconds(2), RunValidations, DebouncedCall.ExecuteMode.End);

                void HandleSceneChanges(ref ObjectChangeEventStream _)
                {
                    debouncedChangeCheck.Invoke();
                }
                // Avoid double-subscribing to the event
                ObjectChangeEvents.changesPublished -= HandleSceneChanges;
                ObjectChangeEvents.changesPublished += HandleSceneChanges;
                // Clean up on unmount
                _validationsContainer.RegisterCallback<DetachFromPanelEvent>(_ =>
                {
                    ObjectChangeEvents.changesPublished -= HandleSceneChanges;
                });
            }
            
            // Draw build panel
            var buildBlock = _builderPanel.Q("build-panel-gui");
            if (buildBlock.childCount == 0)
            {
                _selectedBuilder.CreateBuildGUI(buildBlock);
            }
        }).Every(1000);
        
        _builderPanel.schedule.Execute(() =>
        {
            _builderPanel.schedule.Execute(() =>
            {
                if (validationsFoldout != null &&
                    validationsFoldout.contentRect.height < buildSectionHeightCollapseThreshold)
                {
                        validationsFoldout.value = false;
                }

                if (infoFoldout != null && infoFoldout.contentRect.height < buildSectionHeightCollapseThreshold)
                {
                    infoFoldout.value = false;
                }

                if (infoFoldout != null && validationsFoldout != null)
                {
                    validationsFoldout.style.maxHeight = infoFoldout.value
                        ? new StyleLength(new Length(50, LengthUnit.Percent))
                        : new StyleLength(StyleKeyword.Auto);
                }
            }).Every(200);
        })
        // This is delayed with ExecuteLater because for whatever reason opening the SDK with an uncollapsed validations section will cause it to be automatically closed by collapsing behavior,
        // seemingly because it starts out with no size before the first update is ran
        .ExecuteLater(0);  
    }

    private void RunValidations()
    {
        CheckedForIssues = false;
        _selectedBuilder?.CreateValidationsGUI(_validationsContainer);
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

    private bool _notificationShown;
    private VisualElement _builderNotificationBlock;
    private VisualElement _builderNotificationContent;
    private Label _builderNotificationTitle;
    private Button _builderNotificationDismiss;
    private string _builderNotificationTitleColorClass;
    private IVRCSdkControlPanelBuilder _selectedBuilder;

    public async Task ShowBuilderNotification(string title, VisualElement content, string titleColor = null,
        int timeout = 0)
    {
        if (_notificationShown)
        {
            await DismissNotification();
        }
        
        // If we're not on the builder tab or not logged in, defer the notification until we are
        if ((PanelTab) VRCSettings.ActiveWindowPanel != PanelTab.Builder || !APIUser.IsLoggedIn)
        {
            var waitSource = new CancellationTokenSource();
            // If unity is out of focus - the tabs will not re-render, so a large timeout is used here
            // We should avoid kicking the user to the Account tab in the future every time assembly reloads, so this wouldn't be necessary
            waitSource.CancelAfter(TimeSpan.FromMinutes(10));
            try
            {
                await UniTask.WaitUntil(
                    () => (PanelTab) VRCSettings.ActiveWindowPanel == PanelTab.Builder && APIUser.IsLoggedIn,
                    PlayerLoopTiming.Update, waitSource.Token);
            }
            catch (TaskCanceledException)
            {
                // no-op, we timed out
            }
        }
        
        // Assign the contents after tab-switch waiting, otherwise it will be cleared due to re-mounts
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
        
        _builderNotificationBlock.RemoveFromClassList("d-none");
        
        // wait for ui to rebuild and adjust size
        _builderNotificationBlock.schedule.Execute(() =>
        {
            // ensure things fit correctly
            if (_builderNotificationBlock.parent.contentRect.height < _builderNotificationBlock.hierarchy[0].layout.height + 40)
            {
                _builderNotificationBlock.parent.style.height = _builderNotificationBlock.hierarchy[0].layout.height + 40;
            }
        }).ExecuteLater(100);

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
        _builderNotificationBlock.AddToClassList("d-none");
        _builderNotificationBlock.parent.style.height = StyleKeyword.Auto;
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

    public static GameObject GetReferenceCameraObject()
    {
        var sceneDescriptor = FindObjectOfType<VRC_SceneDescriptor>();
        if (sceneDescriptor == null) return null;

        return sceneDescriptor.ReferenceCamera;
    }

    public static bool ReferenceCameraHasTAAEnabled()
    {
        var refCam = GetReferenceCameraObject();
        if (refCam == null) return false;

        var ppl = refCam.GetComponent<PostProcessLayer>();
        if (ppl == null) return false;

        return ppl.antialiasingMode == PostProcessLayer.Antialiasing.TemporalAntialiasing;
    }

    public static List<TextureImporter> GetOversizeTextureImporters(List<Renderer> renderers)
    {
        HashSet<Material> uniqueMaterials = new HashSet<Material>();
        List<TextureImporter> badTextureImporters = new List<TextureImporter>();

        // Collect all unique materials from renderers
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (!material) { continue;}

                uniqueMaterials.Add(material);
            }
        }

        // Check textures in each unique material
        foreach (Material material in uniqueMaterials)
        {
            int[] texIDs = material.GetTexturePropertyNameIDs();
            foreach (int texID in texIDs)
            {
                Texture texture = material.GetTexture(texID);
                if (!texture) { continue; }

                string path = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrEmpty(path)) { continue; }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.maxTextureSize <= MAX_SDK_TEXTURE_SIZE)
                { continue; }

                badTextureImporters.Add(importer);
            }
        }

        return badTextureImporters;
    }

    public static List<TextureImporter> GetBoxFilteredTextureImporters(List<Renderer> renderers)
    {
        HashSet<Material> uniqueMaterials = new HashSet<Material>();
        List<TextureImporter> badTextureImporters = new List<TextureImporter>();

        // Collect all unique materials from renderers
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (!material) { continue;}

                uniqueMaterials.Add(material);
            }
        }

        // Check textures in each unique material
        foreach (Material material in uniqueMaterials)
        {
            int[] texIDs = material.GetTexturePropertyNameIDs();
            foreach (int texID in texIDs)
            {
                Texture texture = material.GetTexture(texID);
                if (!texture) { continue; }

                string path = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrEmpty(path)) { continue; }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.mipmapFilter != TextureImporterMipFilter.BoxFilter || !importer.mipmapEnabled)
                { continue; }

                badTextureImporters.Add(importer);
            }
        }

        return badTextureImporters;
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
