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
using VRC.Udon.Graph.Interfaces;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonRegistrySearchWindow : UdonSearchWindowBase
    {
        private UdonSearchManager _searchManager;
        private static List<SearchTreeEntry> _registryCache;
        private List<(string, string)> _shortcutRegistries = new List<(string, string)>()
        {
            ("UnityEngine","Debug"),
            ("Udon","Special"),
            ("Udon","Type"),
            ("VRC", "UdonCommonInterfacesIUdonEventReceiver")
        };

        private HashSet<string> _hiddenRegistries = new HashSet<string>()
        {
        };

        public void Initialize(UdonGraphWindow editorWindow, UdonGraph graphView, UdonSearchManager manager)
        {
            base.Initialize(editorWindow, graphView);
            _searchManager = manager;
        }

        #region ISearchWindowProvider

        override public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            if (!skipCache && (_registryCache != null && _registryCache.Count > 0)) return _registryCache;

            _registryCache = new List<SearchTreeEntry>();

            Texture2D icon = EditorGUIUtility.FindTexture("cs Script Icon");
            _registryCache.Add(new SearchTreeGroupEntry(new GUIContent("Quick Search"), 0));

            var topRegistriesLookup =  new Dictionary<string, List<KeyValuePair<string, INodeRegistry>>>();
            foreach (var entry in UdonEditorManager.Instance.GetTopRegistries())
            {
                topRegistriesLookup.Add(entry.Key, new List<KeyValuePair<string, INodeRegistry>>(entry.Value));
            }

            // Add shortcut registries
            foreach (var item in _shortcutRegistries)
            {
                if (topRegistriesLookup.TryGetValue(item.Item1, out var searchRegistry))
                {
                    string subRegistryName = $"{item.Item1}{item.Item2}NodeRegistry";
                    var subRegistry = searchRegistry.FindAll(r => r.Key == subRegistryName);
                    if (subRegistry.Count == 1)
                    {
                        topRegistriesLookup.Add(item.Item2, subRegistry);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find sub-registry {subRegistryName}");
                    }
                }
            }

            // Combine Events into special top-level element
            var vrcEvents = topRegistriesLookup["VRC"].Find(r=>r.Key == "VRCEventNodeRegistry").Value.GetNodeDefinitions();
            var udonEvents = topRegistriesLookup["Udon"].Find(r => r.Key == "UdonEventNodeRegistry").Value.GetNodeDefinitions();
            var allEvents = vrcEvents.Concat(udonEvents);
            var eventRegistry = new EventRegistry();
            foreach (var item in allEvents)
            {
                eventRegistry.definitions.Add(item);
            }
            topRegistriesLookup.Add("Events", new List<KeyValuePair<string, INodeRegistry>>() { new KeyValuePair<string, INodeRegistry>("Events", eventRegistry) });

            // Build lookup from newly organized list
            var topRegistries = topRegistriesLookup.OrderBy(s => s.Key);
            foreach (var topRegistry in topRegistries)
            {
                string topName = topRegistry.Key.Replace("NodeRegistry", "");

                // Handle Shortcut registries with only 1 top-level registry listed
                if (topRegistry.Value.Count == 1)
                {
                    string registryName = UdonGraphExtensions.FriendlyNameify(topName);
                    _registryCache.Add(new SearchTreeGroupEntry(new GUIContent(registryName)) { level = 1, userData = topRegistry.Value });
                    AddEntriesForRegistry(_registryCache, topRegistry.Value.First().Value, 2);
                    continue;
                }

                // Skip empty 'Udon' top level
                if (topName != "Udon")
                {
                    _registryCache.Add(new SearchTreeGroupEntry(new GUIContent(topName), 1));
                }

                foreach (KeyValuePair<string, INodeRegistry> registry in topRegistry.Value.OrderBy(s => s.Key))
                {
                    string baseRegistryName = registry.Key.Replace("NodeRegistry", "").FriendlyNameify().ReplaceFirst(topName, "");
                    string registryName = baseRegistryName.UppercaseFirst();
                    
                    // Plural-ize Event->Events and Type->Types
                    if (topName == "Udon" && (registryName == "Event" || registryName == "Type"))
                    {
                        registryName = $"{registryName}s";
                    }
                    else
                    {
                        // add Registry Level
                        if (registryName.StartsWithCached("Object") || registryName.StartsWithCached("Type"))
                        {
                            registryName = $"{topName}.{registryName}";
                        }

                        // skip certain registries
                        if (_hiddenRegistries.Contains(registryName))
                        {
                            continue;
                        }
                        
                        _registryCache.Add(new SearchTreeEntry(new GUIContent(registryName, icon, $"{topName}.{registryName}")) { level = 2, userData = registry.Value });
                    }
                }
            }
            return _registryCache;
        }

        public override bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            // checking type so we can support selecting registries as well
            if (entry.userData is INodeRegistry)
            {
                _searchManager.QueueOpenFocusedSearch(entry.userData as INodeRegistry, context.screenMousePosition);
                return true;
            }
            else if (entry.userData is UdonNodeDefinition definition && !_graphView.IsDuplicateEventNode(definition.fullName))
            {
                _graphView.AddNodeFromSearch(definition, GetGraphPositionFromContext(context));
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        public class EventRegistry : INodeRegistry
        {
            public List<UdonNodeDefinition> definitions = new List<UdonNodeDefinition>();

            public UdonNodeDefinition GetNodeDefinition(string identifier)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<UdonNodeDefinition> GetNodeDefinitions()
            {
                return definitions;
            }

            public IEnumerable<UdonNodeDefinition> GetNodeDefinitions(string baseIdentifier)
            {
                throw new NotImplementedException();
            }

            public Dictionary<string, INodeRegistry> GetNodeRegistries()
            {
                throw new NotImplementedException();
            }
        }

    }
}