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

        private readonly StyleSheet styles = (StyleSheet)Resources.Load("UdonToolbarStyle");

        public UdonGraphToolbar(UdonGraph graphView)
        {
            name = "UdonGraphToolbar";

            graphView.OnSidebarResize += OnSidebarResize;

            if (!graphView.styleSheets.Contains(styles))
            {
                graphView.styleSheets.Add(styles);
            }

            VisualElement updateOrderField = new VisualElement();
            updateOrderField.AddToClassList("updateOrderField");
            updateOrderField.Add(new Label("UpdateOrder"));
            bool hasValue = graphView.graphProgramAsset != null && graphView.graphProgramAsset.graphData != null;
            _updateOrderIntField = new IntegerField
            {
                name = "UpdateOrderIntegerField",
                value = hasValue ? graphView.graphProgramAsset.graphData.updateOrder : 0,
            };

            _updateOrderIntField.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(graphView.graphProgramAsset, "Changed UpdateOrder");
                graphView.graphProgramAsset.graphData.updateOrder = e.newValue;
                graphView.SaveGraphToDisk();
                AssetDatabase.SaveAssets();
            });
            _updateOrderIntField.isDelayed = true;
            updateOrderField.Add(_updateOrderIntField);
            var leftSide = new VisualElement();
            Add(leftSide);
            leftSide.Add(updateOrderField);

            var rightSide = new VisualElement();

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
            contentContainer.style.paddingLeft = new StyleLength(_sidebarOffset);
        }
    }
}
