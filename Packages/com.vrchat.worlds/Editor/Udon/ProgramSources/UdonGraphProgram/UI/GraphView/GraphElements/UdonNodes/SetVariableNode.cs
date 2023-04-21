using UnityEngine.UIElements;
using VRC.Udon.Graph;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView.UdonNodes
{
    public class SetVariableNode : UdonNode
    {
        public SetVariableNode(UdonNodeDefinition nodeDefinition, UdonGraph view, UdonNodeData nodeData = null) : base(nodeDefinition, view, nodeData)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            
            UdonPort sendChangePort = portsIn[2];
            var toggle = sendChangePort.Q<Toggle>();
            if (toggle != null)
            {
                sendChangePort.Insert(0, toggle);
            }
        }
    }
}