#if UNITY_2019_3_OR_NEWER
using UnityEditor.Experimental.GraphView;
#else
using UnityEditor.Experimental.UIElements.GraphView;
#endif
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonVariableTypeWindow : UdonSearchWindowBase
    {
        internal List<SearchTreeEntry> _fullRegistry;

        #region ISearchWindowProvider

        override public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            if (!skipCache && _fullRegistry != null) return _fullRegistry;

            _fullRegistry = new List<SearchTreeEntry>();

            _fullRegistry.Add(new SearchTreeGroupEntry(new GUIContent("Variable Type Search"), 0));

            var definitions = UdonEditorManager.Instance.GetNodeDefinitions("Variable_").ToList().OrderBy(n=>n.name);
            foreach (var definition in definitions)
            {
                _fullRegistry.Add(new SearchTreeEntry(new GUIContent(UdonGraphExtensions.FriendlyTypeName(definition.type).FriendlyNameify()))
                {
                    level = 1,
                    userData = definition.fullName,
                });
            }

            return _fullRegistry;
        }

        override public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if(_graphView == null)
            {
                return false;
            }
            _graphView.AddNewVariable((string)entry.userData);
            return true;
        }

        #endregion

    }
}