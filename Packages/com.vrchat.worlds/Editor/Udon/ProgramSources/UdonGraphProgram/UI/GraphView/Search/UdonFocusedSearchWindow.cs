#if UNITY_2019_3_OR_NEWER
using UnityEditor.Experimental.GraphView;
#else
using UnityEditor.Experimental.UIElements.GraphView;
#endif
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.Udon.Graph;
using VRC.Udon.Graph.Interfaces;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{

    public class UdonFocusedSearchWindow : UdonSearchWindowBase
    {

        public INodeRegistry targetRegistry;
        internal List<SearchTreeEntry> _fullRegistry;

        #region ISearchWindowProvider

        public override List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {

            _fullRegistry = new List<SearchTreeEntry>();

            Texture2D icon = EditorGUIUtility.FindTexture("cs Script Icon");

            var registryName = GetSimpleNameForRegistry(targetRegistry);
            _fullRegistry.Add(new SearchTreeGroupEntry(new GUIContent($"{registryName} Search"), 0));

            // add Registry Level
            AddEntriesForRegistry(_fullRegistry, targetRegistry, 1, true);

            return _fullRegistry;
        }

        public override bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is UdonNodeDefinition definition && !_graphView.IsDuplicateEventNode(definition.fullName))
            {
                _graphView.AddNodeFromSearch(definition, GetGraphPositionFromContext(context));
                return true;
            }
            else
            {
                return false;
            }
        }

        // TODO: move this to Extension
        private string GetSimpleNameForRegistry(INodeRegistry registry)
        {
            string registryName = registry.ToString().Replace("NodeRegistry", "").FriendlyNameify();
            registryName = registryName.Substring(registryName.LastIndexOf(".") + 1);
            registryName = registryName.Replace("UnityEngine", "");
            return registryName;
        }

        #endregion

    }
}