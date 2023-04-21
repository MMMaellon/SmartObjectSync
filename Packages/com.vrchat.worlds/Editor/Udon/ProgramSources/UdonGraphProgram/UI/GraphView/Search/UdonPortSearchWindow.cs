#if UNITY_2019_3_OR_NEWER
using UnityEditor.Experimental.GraphView;
#else
using UnityEditor.Experimental.UIElements.GraphView;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Udon.Graph;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{

    public class UdonPortSearchWindow : UdonSearchWindowBase
    {

        internal List<SearchTreeEntry> _fullRegistry;

        #region ISearchWindowProvider

        public Type typeToSearch;
        public UdonPort startingPort;
        public Direction direction;

        public class VariableInfo
        {
            public string uid;
            public bool isGetter;

            public VariableInfo(string uid, bool isGetter)
            {
                this.uid = uid;
                this.isGetter = isGetter;
            }
        }

        override public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            _fullRegistry = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent($"{direction.ToString()} Search"), 0)
            };

            var defsToAdd = new Dictionary<string, List<UdonNodeDefinition>>();
            var registries = UdonEditorManager.Instance.GetNodeRegistries();
            foreach (var item in registries)
            {
                var definitions = item.Value.GetNodeDefinitions().ToList();

                var registryName = item.Key.FriendlyNameify().Replace("NodeRegistry", "");
                defsToAdd.Add(registryName, new List<UdonNodeDefinition>());

                foreach (var def in definitions)
                {
                    var collection = direction == Direction.Input ? def.Inputs : def.Outputs;
                    if(collection.Any(p=>p.type == typeToSearch))
                    {
                        defsToAdd[registryName].Add(def);
                    }
                }
            }

            var variables = _graphView.VariableNodes;

            // Add Getters and Setters for matched variable types
            Texture2D icon = EditorGUIUtility.FindTexture("GameManager Icon");
            string typeToSearchSimple = typeToSearch.ToString().Replace(".", "");
            foreach (var item in variables)
            {
                string variableSimpleName = item.fullName.Replace("Variable_", "");
                string getOrSet = direction == Direction.Output ? "Get" : "Set";
                if(variableSimpleName == typeToSearchSimple)
                {
                    string customVariableName = item.nodeValues[1].Deserialize().ToString();
                    _fullRegistry.Add(new SearchTreeEntry(new GUIContent($"{getOrSet} {customVariableName}", icon))
                    {
                        level = 1,
                        userData = new VariableInfo(item.uid, direction == Direction.Output),
                    });
                }
            }

            foreach (var item in defsToAdd)
            {
                // Skip empty lists
                if (item.Value.Count == 0) continue;

                _fullRegistry.Add(new SearchTreeGroupEntry(new GUIContent(item.Key), 1));
                AddEntries(_fullRegistry, item.Value, 2);
            }

            return _fullRegistry;
        }

        public override bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            var position = GetGraphPositionFromContext(context) - new Vector2(140, 0);
            // checking type so we can support selecting registries as well
            if (entry.userData is UdonNodeDefinition definition && !_graphView.IsDuplicateEventNode(definition.fullName))
            {
                var node = _graphView.AddNodeFromSearch(definition, position);
                _graphView.ConnectNodeTo(node, startingPort, direction, typeToSearch);
                return true;
            }
            else if(entry.userData is VariableInfo data)
            {
                UdonNode node = _graphView.MakeVariableNode(data.uid, position, data.isGetter ? GraphView.UdonGraph.VariableNodeType.Getter : GraphView.UdonGraph.VariableNodeType.Setter );
                _graphView.AddElement(node);
                _graphView.ConnectNodeTo(node, startingPort, direction, typeToSearch);
                _graphView.RefreshVariables(true);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

    }
}
