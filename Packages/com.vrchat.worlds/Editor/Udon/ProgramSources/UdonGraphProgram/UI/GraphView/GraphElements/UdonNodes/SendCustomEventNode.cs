using EditorGV = UnityEditor.Experimental.GraphView;
using EngineUI = UnityEngine.UIElements;
using EditorUI = UnityEngine.UIElements;
using VRC.Udon.Graph;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView.UdonNodes
{
    public class SendCustomEventNode : UdonNode
    {
        private EditorUI.PopupField<string> _eventNamePopup;

        public SendCustomEventNode(UdonNodeDefinition nodeDefinition, UdonGraph view, UdonNodeData nodeData = null) : base(nodeDefinition, view, nodeData)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            _eventNamePopup = this.GetProgramPopup(UdonNodeExtensions.ProgramPopupType.Events, _eventNamePopup);
        }
    }
}