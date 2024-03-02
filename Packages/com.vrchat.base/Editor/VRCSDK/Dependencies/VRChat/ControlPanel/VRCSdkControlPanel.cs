using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.Core;
using VRC.Editor;
using VRC.SDKBase.Editor;

/// This class sets up the basic panel layout and draws the main tabs
/// Implementation of each tab is handled within other files extending this partial class

[ExecuteInEditMode]
public partial class VRCSdkControlPanel : EditorWindow, IVRCSdkPanelApi
{
    public static VRCSdkControlPanel window;

    [MenuItem("VRChat SDK/Show Control Panel", false, 600)]
    static void ShowControlPanel()
    {
        if (!ConfigManager.RemoteConfig.IsInitialized())
        {
            VRC.Core.API.SetOnlineMode(true, "vrchat");
            ConfigManager.RemoteConfig.Init(() => ShowControlPanel());
            return;
        }

        GetWindow(typeof(VRCSdkControlPanel));
        window.titleContent.text = "VRChat SDK";
        window.minSize = new Vector2(SdkWindowWidth + 4, 600);
        window.maxSize = new Vector2(SdkWindowWidth + 4, 2000);
        window.Init();
        window.Show();
    }

    public VRCSdkControlPanel()
    {
        window = this;
    }

    #region IMGUI Init
    
    public static GUIStyle titleGuiStyle;
    public static GUIStyle boxGuiStyle;
    public static GUIStyle infoGuiStyle;
    public static GUIStyle listButtonStyleEven;
    public static GUIStyle listButtonStyleOdd;
    public static GUIStyle listButtonStyleSelected;
    public static GUIStyle scrollViewSeparatorStyle;
    public static GUIStyle searchBarStyle;
    public static GUIStyle accountWindowStyle;
    public static GUIStyle centeredLabelStyle;
    public static GUIStyle contentDescriptionStyle;
    public static GUIStyle contentTitleStyle;
    public static GUIStyle unityUpgradeBannerStyle;

    void InitializeStyles()
    {
        titleGuiStyle = new GUIStyle();
        titleGuiStyle.fontSize = 15;
        titleGuiStyle.fontStyle = FontStyle.BoldAndItalic;
        titleGuiStyle.alignment = TextAnchor.MiddleCenter;
        titleGuiStyle.wordWrap = true;
        if (EditorGUIUtility.isProSkin)
            titleGuiStyle.normal.textColor = Color.white;
        else
            titleGuiStyle.normal.textColor = Color.black;

        boxGuiStyle = new GUIStyle
        {
            padding = new RectOffset(5,5,5,5)
        };
        if (EditorGUIUtility.isProSkin)
        {
            boxGuiStyle.normal.background = CreateBackgroundColorImage(new Color(0.3f, 0.3f, 0.3f));
            boxGuiStyle.normal.textColor = Color.white;
        }
        else
        {
            boxGuiStyle.normal.background = CreateBackgroundColorImage(new Color(0.85f, 0.85f, 0.85f));
            boxGuiStyle.normal.textColor = Color.black;
        }

        infoGuiStyle = new GUIStyle();
        infoGuiStyle.wordWrap = true;
        if (EditorGUIUtility.isProSkin)
            infoGuiStyle.normal.textColor = Color.white;
        else
            infoGuiStyle.normal.textColor = Color.black;
        infoGuiStyle.margin = new RectOffset(10, 10, 10, 10);

        listButtonStyleEven = new GUIStyle();
        listButtonStyleEven.margin = new RectOffset(0, 0, 0, 0);
        listButtonStyleEven.border = new RectOffset(0, 0, 0, 0);
        if (EditorGUIUtility.isProSkin)
        {
            listButtonStyleEven.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            listButtonStyleEven.normal.background = CreateBackgroundColorImage(new Color(0.540f, 0.540f, 0.54f));
        }
        else
        {
            listButtonStyleEven.normal.textColor = Color.black;
            listButtonStyleEven.normal.background = CreateBackgroundColorImage(new Color(0.85f, 0.85f, 0.85f));
        }

        listButtonStyleOdd = new GUIStyle();
        listButtonStyleOdd.margin = new RectOffset(0, 0, 0, 0);
        listButtonStyleOdd.border = new RectOffset(0, 0, 0, 0);
        if (EditorGUIUtility.isProSkin)
        {
            listButtonStyleOdd.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            //listButtonStyleOdd.normal.background = CreateBackgroundColorImage(new Color(0.50f, 0.50f, 0.50f));
        }
        else
        {
            listButtonStyleOdd.normal.textColor = Color.black;
            listButtonStyleOdd.normal.background = CreateBackgroundColorImage(new Color(0.90f, 0.90f, 0.90f));
        }

        listButtonStyleSelected = new GUIStyle();
        listButtonStyleSelected.normal.textColor = Color.white;
        listButtonStyleSelected.margin = new RectOffset(0, 0, 0, 0);
        if (EditorGUIUtility.isProSkin)
        {
            listButtonStyleSelected.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            listButtonStyleSelected.normal.background = CreateBackgroundColorImage(new Color(0.4f, 0.4f, 0.4f));
        }
        else
        {
            listButtonStyleSelected.normal.textColor = Color.black;
            listButtonStyleSelected.normal.background = CreateBackgroundColorImage(new Color(0.75f, 0.75f, 0.75f));
        }

        scrollViewSeparatorStyle = new GUIStyle("Toolbar");
        scrollViewSeparatorStyle.fixedWidth = SdkWindowWidth + 10;
        scrollViewSeparatorStyle.fixedHeight = 4;
        scrollViewSeparatorStyle.margin.top = 1;

        searchBarStyle = new GUIStyle("Toolbar");
        searchBarStyle.fixedWidth = SdkWindowWidth - 8;
        searchBarStyle.fixedHeight = 23;
        searchBarStyle.padding.top = 3;

        accountWindowStyle = new GUIStyle("window")
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(0,0,30,30)
        };

        centeredLabelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.UpperCenter,
            margin = new RectOffset(0,0,0,10)
        };
        
        contentDescriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            wordWrap = true
        };
        
        contentTitleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            wordWrap = true
        };
        
        unityUpgradeBannerStyle = new GUIStyle
        {
            normal = new GUIStyleState
            {
                background = Resources.Load<Texture2D>("vrcSdkMigrateTo2022Splash")
            },
            alignment = TextAnchor.LowerCenter,
            margin = new RectOffset(0,0,20,0),
            fixedWidth = 506,
            fixedHeight = 148
        };
    }
    
    #endregion

    private void Init()
    {
        InitializeStyles();
        ResetIssues();
        InitAccount();
    }

    private void OnEnable()
    {
        OnEnableAccount();
        _stylesInitialized = false;
        AssemblyReloadEvents.afterAssemblyReload += BuilderAssemblyReload;
        OnSdkPanelEnable?.Invoke(this, null);
        _panelState = SdkPanelState.Idle;
        OnSdkPanelStateChange?.Invoke(this, _panelState);
        TabsEnabled = true;
    }

    private void OnDisable()
    {
        AssemblyReloadEvents.afterAssemblyReload -= BuilderAssemblyReload;
        OnSdkPanelDisable?.Invoke(this, null);
        _panelState = SdkPanelState.Idle;
        OnSdkPanelStateChange?.Invoke(this, _panelState);
    }

    private void OnDestroy()
    {
        AccountDestroy();
    }

    public const int SdkWindowWidth = 512;

    private readonly bool[] _toolbarOptionsLoggedIn = new bool[4] {true, true, true, true};
    private readonly bool[] _toolbarOptionsNotLoggedIn = new bool[4] {true, false, false, true};
    private bool _stylesInitialized;

    private SdkPanelState _panelState = SdkPanelState.Idle;

    private Button _authenticationTabBtn;
    private Button _buildTabBtn;
    private Button _contentManagerTabBtn;
    private Button _settingsTabBtn;
    private Button[] _tabButtons;
    private VisualElement _sdkPanel;
    private VisualElement _builderPanel;
    private VisualTreeAsset _builderPanelLayout;
    private StyleSheet _builderPanelStyles;
    private float _windowOpenTime;

    private bool _tabsEnabled;
    internal bool TabsEnabled
    {
        get => _tabsEnabled;
        set
        {
            _tabsEnabled = value;
            if (_tabButtons != null)
            {
                foreach (var button in _tabButtons)
                {
                    button.SetEnabled(value);
                }
            }
        }
    }

    private void CreateGUI()
    {
        if (window == null)
        {
            window = (VRCSdkControlPanel)EditorWindow.GetWindow(typeof(VRCSdkControlPanel));
        }
        
        _windowOpenTime = Time.realtimeSinceStartup;
        
        var visualTree = Resources.Load<VisualTreeAsset>("VRCSdkPanelLayout");
        visualTree.CloneTree(rootVisualElement);
        var styles = Resources.Load<StyleSheet>("VRCSdkPanelStyles");
        rootVisualElement.styleSheets.Add(styles);
        rootVisualElement.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "light");

        _sdkPanel = rootVisualElement.Q("sdk-container");
        _builderPanel = rootVisualElement.Q("builder-panel");
        
        CreateTabs();
        RenderTabs();

        rootVisualElement.schedule.Execute(() =>
        {
            var currentPanel = VRCSettings.ActiveWindowPanel;
            if (EditorApplication.isPlaying && currentPanel != 0)
            {
                VRCSettings.ActiveWindowPanel = 0;
                RenderTabs();
                return;
            }
            // Check that the tabs are enabled, if not - we must re-render tabs
            if (APIUser.IsLoggedIn && (!_tabButtons[1].enabledSelf || !_tabButtons[2].enabledSelf))
            {
                RenderTabs();
                return;
            }
            // When the user isn't logged in - we only allow Settings and Authentication tabs to be viewed
            if (APIUser.IsLoggedIn || currentPanel == 0 || currentPanel == 3) return;
            VRCSettings.ActiveWindowPanel = 0;
            RenderTabs();
        }).Every(500);

        var sdkContainer = rootVisualElement.Q("sdk-container");
        sdkContainer.Add(new IMGUIContainer(() =>
        {
            if (!_stylesInitialized)
            {
                InitializeStyles();
                _stylesInitialized = true;
            }
        
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
        
            if (Application.isPlaying)
            {
                GUI.enabled = false;
                GUILayout.Space(20);
                EditorGUILayout.LabelField("Unity Application is running ...\nStop it to access the Control Panel", titleGuiStyle, GUILayout.Width(SdkWindowWidth));
                GUI.enabled = true;
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }
        
            EditorGUILayout.Space();
        
            EnvConfig.SetActiveSDKDefines();
            
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        
            switch (VRCSettings.ActiveWindowPanel)
            {
                case 1:
                    break;
                case 2:
                    ShowContent();
                    break;
                case 3:
                    ShowSettings();
                    break;
                case 0:
                default:
                    ShowAccount();
                    break;
            }
        }));
    }

    private void CreateTabs()
    {
        _authenticationTabBtn = rootVisualElement.Q<Button>("tab-authentication");
        _buildTabBtn = rootVisualElement.Q<Button>("tab-builder");
        _contentManagerTabBtn = rootVisualElement.Q<Button>("tab-content-manager");
        _settingsTabBtn = rootVisualElement.Q<Button>("tab-settings");
        
        _tabButtons = new Button[4]
        {
            _authenticationTabBtn,
            _buildTabBtn,
            _contentManagerTabBtn,
            _settingsTabBtn
        };
        
        var currentPanel = VRCSettings.ActiveWindowPanel;

        for (int i = 0; i < _tabButtons.Length; i++)
        {
            var btnIndex = i;
            _tabButtons[i].EnableInClassList("active", currentPanel == btnIndex);
            _tabButtons[i].SetEnabled(APIUser.IsLoggedIn ? _toolbarOptionsLoggedIn[i] : _toolbarOptionsNotLoggedIn[i]);

            _tabButtons[i].clicked += () =>
            {
                if (EditorApplication.isPlaying) return;
                if (VRCSettings.ActiveWindowPanel == btnIndex)
                {
                    return;
                }
                VRCSettings.ActiveWindowPanel = btnIndex;
                RenderTabs();
            };
        }
    }

    private void RenderTabs()
    {
        var currentPanel = VRCSettings.ActiveWindowPanel;
        
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            _tabButtons[i].EnableInClassList("active", currentPanel == i);
            if (!TabsEnabled) continue;
            _tabButtons[i].SetEnabled(APIUser.IsLoggedIn ? _toolbarOptionsLoggedIn[i] : _toolbarOptionsNotLoggedIn[i]);
        }

        if (currentPanel == 1)
        {
            if (_builderPanel.childCount != 0) return;
            ShowBuilders();
            _builderPanel.RemoveFromClassList("d-none");
            _sdkPanel.AddToClassList("d-none");
        }
        else if (_builderPanel.childCount == 1)
        {
            _builderPanel.AddToClassList("d-none");
            _sdkPanel.RemoveFromClassList("d-none");
            _builderPanel.Remove(_builderPanel.Children().First());
            _builderPanel.styleSheets.Remove(_builderPanelStyles);
        }
    }

    [UnityEditor.Callbacks.PostProcessScene]
    static void OnPostProcessScene()
    {
        if (window != null)
            window.Reset();
    }

    private void OnFocus()
    {
        Reset();
    }
    
    public void Reset()
    {
        ResetIssues();
        // style backgrounds may be nulled on scene load. detect if so has happened
        if((boxGuiStyle != null) && (boxGuiStyle.normal.background == null))
            InitializeStyles();
    }

    [UnityEditor.Callbacks.DidReloadScripts(int.MaxValue)]
    static void DidReloadScripts()
    {
        try
        {
            RefreshApiUrlSetting();
        }
        catch(Exception e)
        {
            //Unity's Mono is trash and randomly fails to assemblies types.
            Debug.LogException(e);
        }
    }
    
    #region Internal API

    [UsedImplicitly]
    internal void SetPanelIdle()
    {
        _panelState = SdkPanelState.Idle;
        OnSdkPanelStateChange?.Invoke(this, _panelState);   
    }

    [UsedImplicitly]
    internal void SetPanelBuilding()
    {
        _panelState = SdkPanelState.Building;
        OnSdkPanelStateChange?.Invoke(this, _panelState);   
    }
    
    [UsedImplicitly]
    internal void SetPanelUploading()
    {
        _panelState = SdkPanelState.Uploading;
        OnSdkPanelStateChange?.Invoke(this, _panelState);    
    }

    #endregion

    #region Public API
    
    public static event EventHandler OnSdkPanelEnable;
    public static event EventHandler OnSdkPanelDisable;
    public static event EventHandler<SdkPanelState> OnSdkPanelStateChange;
    public SdkPanelState PanelState => _panelState;

    #endregion
}
