using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using VRC.Udon.Graph;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonEventTypeWindow : UdonSearchWindowBase
    {
        private List<SearchTreeEntry> _fullRegistry;

        #region ISearchWindowProvider

        public override List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            if (!skipCache && _fullRegistry != null) return _fullRegistry;

            _fullRegistry = new List<SearchTreeEntry> { new SearchTreeGroupEntry(new GUIContent("Events")) };

            var definitions = UdonEditorManager.Instance.GetNodeDefinitions("Event_").ToList().OrderBy(n=>n.name);
            foreach (var definition in definitions)
            {
                _fullRegistry.Add(new SearchTreeEntry(new GUIContent($"Event {definition.name}"))
                {
                    level = 1,
                    userData = definition,
                });
            }

            return _fullRegistry;
        }

        public override bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (_graphView == null || !(entry.userData is UdonNodeDefinition definition) ||
                _graphView.IsDuplicateEventNode(definition.fullName)) return false;
            _graphView.AddNodeFromSearch(definition, GetGraphPositionFromContext(context));
            return true;
        }

        #endregion

    }
}
