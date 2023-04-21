#if UNITY_2019_3_OR_NEWER
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Udon.Graph;
using VRC.Udon.Graph.Interfaces;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonSearchWindowBase : ScriptableObject, ISearchWindowProvider
    {
        // Reference to actual Graph View
        internal UdonGraph _graphView;
        private List<SearchTreeEntry> _exampleLookup;
        internal UdonGraphWindow _editorWindow;
        protected bool skipCache = false;

        private readonly HashSet<string> nodesToSkip = new HashSet<string>()
        {
            "Get_Variable",
            "Set_Variable",
            "Comment",
            "Event_OnVariableChange",
        };

        public virtual void Initialize(UdonGraphWindow editorWindow, UdonGraph graphView)
        {
            _editorWindow = editorWindow;
            _graphView = graphView;
        }

        #region ISearchWindowProvider

        public virtual List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            if (!skipCache && ( _exampleLookup != null && _exampleLookup.Count > 0)) return _exampleLookup;

            _exampleLookup = new List<SearchTreeEntry>();

            Texture2D icon = EditorGUIUtility.FindTexture("cs Script Icon");
            _exampleLookup.Add(new SearchTreeGroupEntry(new GUIContent("Create Node"), 0));

            return _exampleLookup;
        }

        public virtual bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            return true;
        }

        #endregion

        internal Vector2 GetGraphPositionFromContext(SearchWindowContext context)
        {
#if UNITY_2019_3_OR_NEWER
            var windowRoot = _editorWindow.rootVisualElement;
#else
            var windowRoot = _editorWindow.GetRootVisualContainer();
#endif
            var windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent,
                context.screenMousePosition - _editorWindow.position.position);
            var graphMousePosition = _graphView.contentViewContainer.WorldToLocal(windowMousePosition);
            return graphMousePosition;
        }

        internal void AddEntries(List<SearchTreeEntry> cache, IEnumerable<UdonNodeDefinition> definitions, int level,
            bool stripToLastDot = false)
        {
            Texture2D icon = AssetPreview.GetMiniTypeThumbnail(typeof(GameObject));
            Texture2D iconGetComponents = EditorGUIUtility.FindTexture("d_ViewToolZoom");
            Texture2D iconOther = new Texture2D(1, 1);
            iconOther.SetPixel(0,0, new Color(0,0,0,0));
            iconOther.Apply();
            Dictionary<string, UdonNodeDefinition> baseNodeDefinition = new Dictionary<string, UdonNodeDefinition>();

            foreach (UdonNodeDefinition nodeDefinition in definitions.OrderBy(
                s => UdonGraphExtensions.PrettyFullName(s)))
            {
                string baseIdentifier = nodeDefinition.fullName;
                string[] splitBaseIdentifier = baseIdentifier.Split(new[] { "__" }, StringSplitOptions.None);
                if (splitBaseIdentifier.Length >= 2)
                {
                    baseIdentifier = $"{splitBaseIdentifier[0]}__{splitBaseIdentifier[1]}";
                }

                if (baseNodeDefinition.ContainsKey(baseIdentifier))
                {
                    continue;
                }

                baseNodeDefinition.Add(baseIdentifier, nodeDefinition);
            }

            var nodesOfGetComponentType = new List<SearchTreeEntry>();
            var nodesOfOtherType = new List<SearchTreeEntry>();
            
            // add all subTypes
            foreach (KeyValuePair<string, UdonNodeDefinition> nodeDefinitionsEntry in baseNodeDefinition)
            {
                string nodeName = UdonGraphExtensions.PrettyBaseName(nodeDefinitionsEntry.Key);
                nodeName = nodeName.UppercaseFirst();
                nodeName = nodeName.Replace("_", " ");
                if (stripToLastDot)
                {
                    int lastDotIndex = nodeName.LastIndexOf('.');
                    nodeName = nodeName.Substring(lastDotIndex + 1);
                }
                
                // Skip some nodes that should be added in other ways (variables and comments)
                if (nodeName.StartsWithCached("Variable") || nodesToSkip.Contains(nodeDefinitionsEntry.Key))
                {
                    continue;
                }

                if (nodeName.StartsWithCached("Object"))
                {
                    nodeName = $"{nodeDefinitionsEntry.Value.type.Namespace}.{nodeName}";
                }

                if (nodeNamesGetComponentType.Contains(nodeName))
                {
                    nodesOfGetComponentType.Add(new SearchTreeEntry(new GUIContent(nodeName, iconGetComponents)) { level = level+1, userData = nodeDefinitionsEntry.Value });
                    continue;
                }
                
                // Only put 'Equals' in the 'Other' category if this definition is not an Enum
                if (nodeNamesOtherType.Contains(nodeName) || nodeName == "Equals" && !nodeDefinitionsEntry.Value.type.IsEnum)
                {
                    nodesOfOtherType.Add(new SearchTreeEntry(new GUIContent(nodeName, iconOther)) { level = level+1, userData = nodeDefinitionsEntry.Value });
                    continue;
                }

                cache.Add(new SearchTreeEntry(new GUIContent(nodeName, icon)) { level = level, userData = nodeDefinitionsEntry.Value });
            }
            
            // add getComponents level
            if (nodesOfGetComponentType.Count > 0)
            {
                cache.Add(new SearchTreeGroupEntry(new GUIContent("GetComponents"), level));
                foreach (var entry in nodesOfGetComponentType)
                {
                    cache.Add(entry);
                }   
            }

            // add other level
            if (nodesOfOtherType.Count > 0)
            {
                cache.Add(new SearchTreeGroupEntry(new GUIContent("Other"), level));
                foreach (var entry in nodesOfOtherType)
                {
                    cache.Add(entry);
                }   
            }
        }

        private static HashSet<string> nodeNamesGetComponentType = new HashSet<string>()
        {
            "GetComponent",
            "GetComponentInChildren",
            "GetComponentInParent",
            "GetComponents",
            "GetComponentsInChildren",
            "GetComponentsInParent",
        };
        
        private static HashSet<string> nodeNamesOtherType = new HashSet<string>()
        {
            "Equality",
            "GetHashCode",
            "GetInstanceID",
            "GetType",
            "Implicit",
            "Inequality",
            "Tostring",
        };

        // adds all entries so we can use this for regular and array registries
        internal void AddEntriesForRegistry(List<SearchTreeEntry> cache, INodeRegistry registry, int level,
            bool stripToLastDot = false)
        {
            AddEntries(cache, registry.GetNodeDefinitions(), level, stripToLastDot);
        }
    }
}