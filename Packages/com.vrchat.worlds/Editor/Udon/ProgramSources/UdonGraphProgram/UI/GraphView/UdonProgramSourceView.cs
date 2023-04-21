using UnityEngine.UIElements;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonProgramSourceView : VisualElement
    {
        private UdonProgramAsset _asset;
        private readonly TextElement _assemblyField;

        public UdonProgramSourceView ()
        {
            // Create and add container and children to display latest Assembly
            VisualElement assemblyContainer = new VisualElement { name = "Container", };
            Label assemblyHeader = new Label("Udon Assembly")
            {
                name = "Header"
            };

            ScrollView scrollView = new ScrollView();

            _assemblyField = new TextElement
            {
                name = "AssemblyField"
            };

            assemblyContainer.Add(assemblyHeader);
            assemblyContainer.Add(scrollView);
            assemblyContainer.Add(_assemblyField);
            scrollView.contentContainer.Add(_assemblyField);

            Add(assemblyContainer);
            
            // block dragging/moving
            RegisterCallback((EventCallback<DragUpdatedEvent>) (e => e.StopPropagation()));
            RegisterCallback((EventCallback<WheelEvent>) (e => e.StopPropagation()));
            RegisterCallback((EventCallback<MouseDownEvent>) (e => e.StopPropagation()));
        }

        public void LoadAsset(UdonGraphProgramAsset asset)
        {
            _asset = asset;
        }

        public void SetText(string newValue)
        {
            _assemblyField.text = newValue;
        }

        // ReSharper disable once UnusedMember.Global
        public void Unload()
        {
            UdonProgramAsset udonProgramAsset = _asset;
            if(udonProgramAsset != null)
            {
                _asset = null;
            }
        }
    }
}
