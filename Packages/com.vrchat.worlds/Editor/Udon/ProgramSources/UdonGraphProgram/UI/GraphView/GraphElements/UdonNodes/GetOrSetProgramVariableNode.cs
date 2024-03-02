using EngineUI = UnityEngine.UIElements;
using EditorUI = UnityEngine.UIElements;

using VRC.Udon.Graph;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView.UdonNodes
{
    public class GetOrSetProgramVariableNode : UdonNode
    {
        private EditorUI.PopupField<string> _programVariablePopup;

        public GetOrSetProgramVariableNode(UdonNodeDefinition nodeDefinition, UdonGraph view, UdonNodeData nodeData = null) :
            base(nodeDefinition, view, nodeData)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            _programVariablePopup =
                this.GetProgramPopup(UdonNodeExtensions.ProgramPopupType.Variables, _programVariablePopup);
        }
    }
}