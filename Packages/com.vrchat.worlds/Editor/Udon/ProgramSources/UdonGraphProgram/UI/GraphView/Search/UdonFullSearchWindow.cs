using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.Udon.Graph;
using VRC.Udon.Graph.Interfaces;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonFullSearchWindow : UdonSearchWindowBase
    {
        private static List<SearchTreeEntry> _slowRegistryCache;

        #region ISearchWindowProvider

        public override List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            if (!skipCache && _slowRegistryCache != null && _slowRegistryCache.Count > 0) return _slowRegistryCache;

            _slowRegistryCache = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Full Search"))
            };

            var topRegistries = UdonEditorManager.Instance.GetTopRegistries();

            foreach (var topRegistry in topRegistries)
            {
                string topName = topRegistry.Key.Replace("NodeRegistry", "");

                if (topName != "Udon")
                {
                    _slowRegistryCache.Add(new SearchTreeGroupEntry(new GUIContent(topName), 1));
                }

                // get all registries, save into registryName > INodeRegistry Lookup
                var subRegistries = new Dictionary<string, INodeRegistry>();
                foreach (KeyValuePair<string, INodeRegistry> registry in topRegistry.Value.OrderBy(s => s.Key))
                {
                    string baseRegistryName = registry.Key.Replace("NodeRegistry", "").FriendlyNameify().ReplaceFirst(topName, "");
                    string registryName = baseRegistryName.UppercaseFirst();
                    subRegistries.Add(registryName, registry.Value);
                }

                // Go through each registry entry and add the top-level registry and associated array registry
                foreach (KeyValuePair<string, INodeRegistry> regEntry in subRegistries)
                {
                    INodeRegistry registry = regEntry.Value;
                    string registryName = regEntry.Key;

                    int level = 2;
                    // Special cases for Udon sub-levels, added at top
                    if (topName == "Udon")
                    {
                        level = 1;
                        if (registryName == "Event" || registryName == "Type")
                        {
                            registryName = $"{registryName}s";
                        }
                    }

                    if (!registryName.EndsWith("[]"))
                    {
                        // add Registry Level
                        var groupEntry = new SearchTreeGroupEntry(new GUIContent(registryName, (Texture2D)null), level) { userData = registry };
                        _slowRegistryCache.Add(groupEntry);
                    }

                    // Check for Array Type first
                    string regArrayType = $"{registryName}[]";
                    if (subRegistries.TryGetValue(regArrayType, out INodeRegistry arrayRegistry))
                    {
                        // we have a matching subRegistry, add that next
                        var arrayLevel = level + 1;
                        var arrayGroupEntry = new SearchTreeGroupEntry(new GUIContent(regArrayType, (Texture2D)null), arrayLevel) { userData = registry };
                        _slowRegistryCache.Add(arrayGroupEntry);

                        // Add all array entries
                        AddEntriesForRegistry(_slowRegistryCache, arrayRegistry, arrayLevel + 1);
                    }
                    
                    AddEntriesForRegistry(_slowRegistryCache, registry, level + 1, true);

                }
            }
            return _slowRegistryCache;
        }

        public override bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            // checking type so we can support selecting registries as well
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

        #endregion

    }
}
