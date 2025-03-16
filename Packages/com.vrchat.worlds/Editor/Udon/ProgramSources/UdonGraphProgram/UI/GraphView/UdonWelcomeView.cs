#if UNITY_2019_3_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#else
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonWelcomeView : VisualElement
    {
        private Button _openLastGraphButton;

        public UdonWelcomeView()
        {
            name = "udon-welcome";
            this.RegisterCallback<AttachToPanelEvent>(Initialize);
        }


        private void Initialize(AttachToPanelEvent evt)
        {
            // switch event to do some UI updates instead of initialization from here on out
            UnregisterCallback<AttachToPanelEvent>(Initialize);

            // Add Header
            Add(new TextElement()
            {
                name = "intro",
                text = "Udon Graph",
            });

            Add(new TextElement()
            {
                name = "header-message",
                text =
                    "The Udon Graph is your gateway to creating amazing things in VRChat.\n Open Example Central from the VRChat SDK menu to browse different examples you can import to your projects."
            });

            var mainContainer = new VisualElement()
            {
                name = "main",
            };

            Add(mainContainer);

            var template = Resources.Load<VisualTreeAsset>("UdonChangelog") as VisualTreeAsset;
            #if UNITY_2019_3_OR_NEWER
            var changelog = template.CloneTree((string) null);
            #else
            var changelog = template.CloneTree(null);
            #endif
            changelog.name = "changelog";
            mainContainer.Add(changelog);

            var column2 = new VisualElement() {name = "column-2"};
            mainContainer.Add(column2);

            var settingsTemplate =
                Resources.Load<VisualTreeAsset>("UdonSettings") as VisualTreeAsset;
            #if UNITY_2019_3_OR_NEWER
            var settings = settingsTemplate.CloneTree((string)null);
            #else
            var settings = settingsTemplate.CloneTree(null);
            #endif
            settings.name = "settings";
            column2.Add(settings);

            // get reference to first settings section
            var section = settings.Q("section");

            // Add Grid Snap setting
            var gridSnapContainer = new VisualElement();
            gridSnapContainer.AddToClassList("settings-item-container");
            var gridSnapField = new IntegerField(3)
            {
                value = Settings.GridSnapSize
            };
#if UNITY_2019_3_OR_NEWER
            gridSnapField.RegisterValueChangedCallback(
#else
            gridSnapField.OnValueChanged(
#endif
                e => { Settings.GridSnapSize = e.newValue; });
            gridSnapContainer.Add(new Label("Grid Snap Size"));
            gridSnapContainer.Add(gridSnapField);
            section.Add(gridSnapContainer);
            var gridSnapLabel = new Label("Snap elements to a grid as you move them. 0 for No Snapping.");
            gridSnapLabel.AddToClassList("settings-label");
            section.Add(gridSnapLabel);

            // Add Search On Selected Node settings
            var searchOnSelectedNode = (new Toggle()
            {
                text = "Focus Search On Selected Node",
                value = Settings.SearchOnSelectedNodeRegistry,
            });
#if UNITY_2019_3_OR_NEWER
            searchOnSelectedNode.RegisterValueChangedCallback(
#else
            searchOnSelectedNode.OnValueChanged(
#endif
                (toggleEvent) => { Settings.SearchOnSelectedNodeRegistry = toggleEvent.newValue; });
            section.Add(searchOnSelectedNode);
            var searchOnLabel =
                new Label(
                    "Highlight a node and press Spacebar to open a Search Window focused on nodes for that type. ");
            searchOnLabel.AddToClassList("settings-label");
            section.Add(searchOnLabel);

            // Add Search On Noodle Drop settings
            var searchOnNoodleDrop = (new Toggle()
            {
                text = "Search On Noodle Drop",
                value = Settings.SearchOnNoodleDrop,
            });
#if UNITY_2019_3_OR_NEWER
            searchOnNoodleDrop.RegisterValueChangedCallback(
#else
            searchOnNoodleDrop.OnValueChanged(
#endif
(toggleEvent) => { Settings.SearchOnNoodleDrop = toggleEvent.newValue; });
            section.Add(searchOnNoodleDrop);
            var searchOnDropLabel =
                new Label("Drop a noodle into empty space to search for anything that can be connected.");
            searchOnDropLabel.AddToClassList("settings-label");
            section.Add(searchOnDropLabel);

            // Add UseNeonStyle setting
            var useNeonStyle = (new Toggle()
            {
                text = "Use Neon Style",
                value = Settings.UseNeonStyle,
            });
#if UNITY_2019_3_OR_NEWER
            useNeonStyle.RegisterValueChangedCallback(
#else
            useNeonStyle.OnValueChanged(
#endif
            (toggleEvent) => { Settings.UseNeonStyle = toggleEvent.newValue; });
            section.Add(useNeonStyle);
            var useNeonStyleLabel =
                new Label("Try out an experimental Neon Style. We will support User Styles in an upcoming version.");
            useNeonStyleLabel.AddToClassList("settings-label");
            section.Add(useNeonStyleLabel);
        }

        private void UpdateLastGraphButtonLabel()
        {
            if (_openLastGraphButton == null) return;

            string currentButtonAssetGuid = (string) _openLastGraphButton.userData;
            Settings.GraphSettings graphSettings = Settings.GetLastGraph();
            if (String.CompareOrdinal(currentButtonAssetGuid, graphSettings.uid) != 0)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(graphSettings.uid);
                var graphName = assetPath.Substring(assetPath.LastIndexOf("/", StringComparison.Ordinal) + 1).Replace(".asset", "");

                _openLastGraphButton.userData = graphSettings.uid;
                _openLastGraphButton.text = $"Open {graphName}";
            }
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            UpdateLastGraphButtonLabel();
        }
    }
}