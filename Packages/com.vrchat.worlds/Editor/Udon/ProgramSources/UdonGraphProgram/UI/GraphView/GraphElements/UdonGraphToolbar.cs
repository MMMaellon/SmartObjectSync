using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonGraphToolbar: VisualElement
    {
        private readonly IntegerField _updateOrderIntField;
        private readonly Button _graphHighlightFlow;
        private readonly UdonGraphStatus _graphStatus;
        private float _sidebarOffset = 238f;
        private VisualElement _sidebarSpacer;

        private readonly StyleSheet styles = (StyleSheet)Resources.Load("UdonToolbarStyle");

        public UdonGraphToolbar(UdonGraph graphView)
        {
            name = "UdonGraphToolbar";

            graphView.OnSidebarResize += OnSidebarResize;

            if (!graphView.styleSheets.Contains(styles))
            {
                graphView.styleSheets.Add(styles);
            }
            
            bool hasValue = graphView.graphProgramAsset != null && graphView.graphProgramAsset.graphData != null;
            _updateOrderIntField = new IntegerField
            {
                name = "UpdateOrderIntegerField",
                label = "Update Order",
                value = hasValue ? graphView.graphProgramAsset.graphData.updateOrder : 0
            };
            _updateOrderIntField.AddToClassList("updateOrderField");

            _updateOrderIntField.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(graphView.graphProgramAsset, "Changed UpdateOrder");
                graphView.graphProgramAsset.graphData.updateOrder = e.newValue;
                graphView.SaveGraphToDisk();
                AssetDatabase.SaveAssets();
            });
            _updateOrderIntField.isDelayed = true;
            var leftSide = new VisualElement();

            _sidebarSpacer = new VisualElement();
            _sidebarSpacer.AddToClassList("toolbarSpacer");
            Add(_sidebarSpacer);
            
            Add(leftSide);
            leftSide.Add(_updateOrderIntField);

            var rightSide = new VisualElement();
            rightSide.AddToClassList("toolbarRightSide");

            _graphHighlightFlow = new Button(() =>
            {
                Settings.HighlightFlow = !Settings.HighlightFlow;
                _graphHighlightFlow?.EnableInClassList("selected", Settings.HighlightFlow);
                graphView.OnHighlightFlowChanged();
            })
            {
                text = "Highlight Flow",
                tooltip = "Highlights the flow-connected nodes on click"
            };
            rightSide.Add(_graphHighlightFlow);

            Button graphCompile = new Button(() =>
                {
                    if (graphView.graphProgramAsset != null &&
                        graphView.graphProgramAsset is AbstractUdonProgramSource udonProgramSource)
                    {
                        UdonEditorManager.Instance.QueueAndRefreshProgram(udonProgramSource);
                    }
                })
                { text = "Compile" };
            rightSide.Add(graphCompile);

            Button graphReload = new Button(graphView.Reload)
                { text = "Reload" };
            rightSide.Add(graphReload);

            _graphStatus = new UdonGraphStatus(graphView);
            rightSide.Add(_graphStatus);
            Add(rightSide);

            UpdateStatusAsset(graphView.graphProgramAsset);
            
            // block dragging/moving
            RegisterCallback((EventCallback<DragUpdatedEvent>) (e => e.StopPropagation()));
            RegisterCallback((EventCallback<WheelEvent>) (e => e.StopPropagation()));
            RegisterCallback((EventCallback<MouseDownEvent>) (e => e.StopPropagation()));
        }

        private void UpdateStatusAsset(UdonGraphProgramAsset graph)
        {
            if (graph == null) return;
            _graphStatus.LoadAsset(graph);
        }

        public void RefreshAsset(UdonGraphProgramAsset asset)
        {
            if (asset == null) return;
            _updateOrderIntField.value = asset.graphData.updateOrder;
            UpdateStatusAsset(asset);
        }

        private void OnSidebarResize(object sender, MouseMoveEvent evt)
        {
            _sidebarOffset = evt.mousePosition.x + 8f;
            _sidebarSpacer.style.width = new StyleLength(_sidebarOffset);
        }
    }
}
