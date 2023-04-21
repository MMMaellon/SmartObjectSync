using EditorGV = UnityEditor.Experimental.GraphView;
using EngineUI = UnityEngine.UIElements;
using EditorUI = UnityEditor.UIElements;
using System.Linq;
using UnityEngine;
using VRC.Udon.Graph;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView.UdonNodes
{
    public class SetReturnValueNode : UdonNode
    {
        public SetReturnValueNode(UdonNodeDefinition nodeDefinition, UdonGraph view, UdonNodeData nodeData = null) : base(nodeDefinition, view, nodeData)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            
            const string returnVariable = UdonBehaviour.ReturnVariableName;
            string uuid = !Graph.VariableNames.Contains(returnVariable) ? Graph.AddNewVariable("Variable_SystemObject", returnVariable) : 
                Graph.VariableNodes.FirstOrDefault(n => (string)n.nodeValues[1].Deserialize() == returnVariable)?.uid;

            if (!string.IsNullOrWhiteSpace(uuid))
                SetNewValue(uuid, 0);
            else
                Debug.LogError("Could not find return value name!");
        }
    }
}