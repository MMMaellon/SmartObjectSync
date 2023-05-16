using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonGraphWindow : EditorWindow
    {
        private VisualElement _rootView;

        // Reference to actual Graph View
        private UdonGraph _graphView;
        private UdonWelcomeView _welcomeView;
        private VisualElement _curtain;

        // Toolbar and Buttons
        private Toolbar _toolbar;
        private ToolbarMenu _toolbarOptions;
        private UdonGraphStatus _graphStatus;
        private ToolbarButton _graphReload;
        private ToolbarButton _graphCompile;
        private VisualElement _updateOrderField;
        private IntegerField _updateOrderIntField;

        [MenuItem("VRChat SDK/Udon Graph")]
        private static void ShowWindow()
        {
            // Get or focus the window
            var window = GetWindow<UdonGraphWindow>("Udon Graph", true, typeof(SceneView));
            window.titleContent = new GUIContent("Udon Graph");
        }

        private void LogPlayModeState(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                    if (_rootView.Contains(_curtain))
                    {
                        _curtain.RemoveFromHierarchy();
                    }

                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    _rootView.Add(_curtain);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }
        
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += LogPlayModeState;

            InitializeRootView();

            _curtain = new VisualElement
            {
                name = "curtain",
            };
            _curtain.Add(new Label("Graph Locked in Play Mode"));

            _welcomeView = new UdonWelcomeView();
            _welcomeView.StretchToParentSize();
            _rootView.Add(_welcomeView);
            SetupToolbar();

            Undo.undoRedoPerformed -=
                OnUndoRedo; //Remove old handler if present to prevent duplicates, doesn't cause errors if not present
            Undo.undoRedoPerformed += OnUndoRedo;
            
            // Clear bad/old serialized data (ie: from another unity project or a deleted asset)    
            Settings.CleanSerializedData();
            var graphSettingsList = Settings.GraphSettingsList.GetGraphSettings(); // Get list of open graphs from the Settings

            foreach (Settings.GraphSettings t in graphSettingsList)
            {
                LoadGraphFromSettings(t);
                var graphSettingsListNew = Settings.GraphSettingsList.GetGraphSettings(); // Get list of open graphs from the Settings
            }
        }

        private void InitializeRootView()
        {
            _rootView = rootVisualElement;
            _rootView.styleSheets.Add((StyleSheet) Resources.Load("UdonGraphStyle"));
            _rootView.styleSheets.Add((StyleSheet) Resources.Load("UdonSidebarStyle"));
            _rootView.styleSheets.Add((StyleSheet) Resources.Load("UdonToolbarStyle"));
        }


        public void LoadGraphFromAsset(UdonGraphProgramAsset graph, UdonBehaviour udonBehaviour = null)
        {
            // Store opened graph in the settings
            Settings.SetLastGraph(graph, udonBehaviour);
            var graphSettings = Settings.GetLastGraph();
            LoadGraphFromSettings(graphSettings);
        }


        private void LoadGraphFromSettings(Settings.GraphSettings graphSettings)
        {
            void CloseGraphTab(string graphAssetName)
            {
                Settings.CloseGraph(graphAssetName); // Remove the graph asset from the list of opened graphs

                // Iterate through the elements in the toolbar with the "graphTab" style className
                _toolbar.Query(null, "graphTab").ForEach(i =>
                {
                    if (i is Button btn &&
                        btn.text == graphAssetName) // Find the button with the same name as the graph asset
                    {
                        btn.RemoveFromHierarchy(); // Remove the graph tab button from the toolbar
                    }
                });
                // Load the last graph that was opened, after closing this tab
                var lastGraphAfterClosed = Settings.GetLastGraph();
                if (lastGraphAfterClosed == null)
                {
                    // If there are no more graphs open, show the welcome view
                    _rootView.Remove(_graphView);
                    _rootView.Add(_welcomeView);
                }

                LoadLastGraphIntoGraphWindow();
            }

            void UpdateSelectedTab()
            {
                var lastGraph = Settings.GetLastGraph();
                // Iterate through the elements in the toolbar with the "graphTab" style className
                _toolbar.Query(null, "graphTab").ForEach(i =>
                {
                    if (i == null) return;
                    bool isSelected = (lastGraph == null || ((Button)i).text == lastGraph.programAsset.name);
                    i.EnableInClassList("selected", isSelected); // Deselect all the graph tabs
                });
                if (lastGraph != null)
                {
                    var graphToolbar = _rootView.Q<UdonGraphToolbar>();
                    graphToolbar.RefreshAsset(lastGraph.programAsset);
                }
            }

            void LoadLastGraphIntoGraphWindow(UdonBehaviour udonBehaviour = null)
            {
                Settings.GraphSettings lastGraphSettings = Settings.GetLastGraph();
                if (lastGraphSettings == null) return;
                var lastGraph = lastGraphSettings.programAsset;
                if (lastGraph == null) return;

                if (udonBehaviour != null)
                {
                    if (udonBehaviour.programSource != lastGraph)
                    {
                        // If the udon behaviour is not using the last graph, don't load it
                        udonBehaviour = null;
                    }
                }
                
                if (_graphView == null)
                {
                    //Make a new graph view only if one doesn't exist
                    _graphView = new UdonGraph(this);
                }
            
                // Remove the views if they already exist
                RemoveIfContaining(_welcomeView);
                RemoveIfContaining(_graphView);

                Button graphTabButton = FindGraphIfOpen(lastGraph);
                
                // Add the graph view to the root view
                _rootView.Insert(0, _graphView);

                if (graphTabButton == null)
                {
                    OpenNewTab(lastGraph);
                }
                
                // Load the graph into the graph view
                _graphView = _rootView.Children().FirstOrDefault(e => e is UdonGraph) as UdonGraph;
                if (_graphView == null)
                {
                    Debug.LogError("GraphView has not been added to the BaseGraph root view!");
                    return;
                }
                _graphView.Initialize(lastGraph, udonBehaviour);

                UpdateSelectedTab();
                
                if (lastGraph != null)
                {
                    var graphToolbar = _rootView.Q<UdonGraphToolbar>();
                    graphToolbar.RefreshAsset(lastGraph);
                }
            }
            
            Button FindGraphIfOpen(UdonGraphProgramAsset asset)
            {
                VisualElement match = null;
                foreach (var child in _toolbar.Children())
                {   
                    if (child is Button graphButton && graphButton.text == asset.name)
                    {
                        match = graphButton;
                        break;
                    }
                }

                return (Button)match;
            }

            void OpenNewTab(UdonGraphProgramAsset graph)
            {
                // Create a new graph tab button
                var graphAssetName = graph.name; // Get the name of the graph asset
                var graphButton = new Button(() =>
                {
                    var selected = Settings.GetGraph(graphAssetName); // Find the selected graph
                    if (selected == null)
                    {
                        // If the graph is not found, remove the tab
                        CloseGraphTab(graphAssetName);
                    }
                    else
                    {
                        Settings.SetLastGraph(selected);
                        LoadLastGraphIntoGraphWindow(); // Initialize the graph with the selected graph
                        UpdateSelectedTab();
                    }
                })
                {
                    text = graphAssetName // Set the text of the graph tab button to the name of the graph asset
                };
                var closeTabBtn = new Button(() => { CloseGraphTab(graphAssetName); })
                {
                    text = "x" // Set the text of the close tab button to "x"
                };

                closeTabBtn.AddToClassList("graphTabClose");
                graphButton.Add(closeTabBtn);
                graphButton.AddToClassList("graphTab");
                //graphButton.AddToClassList("selected");

                // Add the graph tab button to the toolbar
                _toolbar.Add(graphButton);
            }
            
            if (graphSettings == null) return;
            if (graphSettings.programAsset == null) return;
            
            string assetPath = graphSettings.assetPath;
            string scenePath = graphSettings.scenePath;
            
            if (!string.IsNullOrEmpty(scenePath)) // If the graph was opened from a scene
            {
                var targetScene = SceneManager.GetSceneByPath(scenePath);
                if (!targetScene.isLoaded || !targetScene.IsValid()) return;

                GameObject targetObject = null;
                var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (var gameObject in gameObjects)
                {
                    if (gameObject.scene == targetScene && VRC.Core.ExtensionMethods.GetHierarchyPath(gameObject.transform) == assetPath)
                    {
                        targetObject = gameObject;
                        break;
                    }
                }
                 
                if (targetObject == null) return;

                UdonBehaviour udonBehaviour = targetObject.GetComponent<UdonBehaviour>();
                LoadLastGraphIntoGraphWindow(udonBehaviour);
            }
            else if (!string.IsNullOrEmpty(assetPath)) // If the graph was opened directly from an asset
            {
                var asset = AssetDatabase.LoadAssetAtPath<UdonGraphProgramAsset>(assetPath);
                if (asset == null) return;
                LoadLastGraphIntoGraphWindow();
            }
            Button graphTab = FindGraphIfOpen(graphSettings.programAsset);
            
            if (graphTab == null)
            {
                OpenNewTab(graphSettings.programAsset);
            }
            
        }

        private void ReloadWelcome()
        {
            RemoveIfContaining(_welcomeView);
            _rootView.Add(_welcomeView);
        }

        // TODO: maybe move this to GraphView since it's so connected?
        private void SetupToolbar()
        {
            _toolbar = new Toolbar
            {
                name = "UdonToolbar",
            };
            _rootView.Add(_toolbar);

            _toolbar.Add(new ToolbarButton(() => { ReloadWelcome(); })
                {text = "Welcome"});

            _toolbarOptions = new ToolbarMenu
             {
                 text = "Settings"
             };
             // Show UpdateOrder setting
             _updateOrderField = new VisualElement
             {
                 visible = false
             };
             _updateOrderIntField = new IntegerField
             {
                 name = "UpdateOrderIntegerField",
                 value = (_graphView?.graphProgramAsset == null) ? 0 : _graphView.graphProgramAsset.graphData.updateOrder,
             };
             
             // Search On Noodle Drop
            _toolbarOptions.menu.AppendAction("Search on Noodle Drop",
                m => { Settings.SearchOnNoodleDrop = !Settings.SearchOnNoodleDrop; },
                s => BoolToStatus(Settings.SearchOnNoodleDrop));
            // Search On Selected Node
            _toolbarOptions.menu.AppendAction("Search on Selected Node",
                m => { Settings.SearchOnSelectedNodeRegistry = !Settings.SearchOnSelectedNodeRegistry; },
                s => BoolToStatus(Settings.SearchOnSelectedNodeRegistry));
            _toolbar.Add(_toolbarOptions);
        }

        private DropdownMenuAction.Status BoolToStatus(bool value)
        {
            return value ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
        }

        private void RemoveIfContaining(VisualElement element)
        {
            if (_rootView.Contains(element))
            {
                _rootView.Remove(element);
            }
        }

        private void OnUndoRedo()
        {
            Repaint();
        }
        
    }
}
