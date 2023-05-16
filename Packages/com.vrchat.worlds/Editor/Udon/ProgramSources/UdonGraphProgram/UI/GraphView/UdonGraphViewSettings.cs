using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Core;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public static class Settings
    {
        private const string UseNeonStyleString = "UdonGraphViewSettings.UseNeonStyle";
        private const string SearchOnSelectedNodeRegistryString = "UdonGraphViewSettings.SearchOnSelectedNodeRegistry";
        private const string GridSnapSizeString = "UdonGraphViewSettings.GridSnapSize";
        private const string SearchOnNoodleDropString = "UdonGraphViewSettings.SearchOnNoodleDrop";
        private const string HighlightFlowString = "UdonGraphViewSettings.HighlightFlow";
        private const string OpenGraphsString = "UdonGraphViewSettings.OpenGraphs";
        private const string LastGraphIndexString = "UdonGraphViewSetting.LastGraphIndex";
        
        [Serializable]
        public class GraphSettings
        {
            public string uid;
            public string scenePath;
            public string assetPath;
            public UdonGraphProgramAsset programAsset;

            public static bool IsValid(GraphSettings graphSettings)
            {
                if (graphSettings.uid == null) return true; // unsaved, freshly instantiated graph (for testing)
                if (graphSettings.programAsset == null) return false;
                // only one of these needs to be set
                return graphSettings.scenePath != null || graphSettings.assetPath != null;
            }

            public static GraphSettings Create(UdonGraphProgramAsset programAsset, UdonBehaviour programBehaviour = null)
            {
                var graphSettings = new GraphSettings(programAsset, programBehaviour);

                // return null if the graph settings are invalid
                return !IsValid(graphSettings) ? null :
                    // graph was valid, return it
                    graphSettings;
            }
            
            private GraphSettings(UdonGraphProgramAsset programAsset, UdonBehaviour programBehaviour = null)
            {
                this.programAsset = programAsset;
                
                // Store GUID for this asset to settings for easy reload later
                if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(programAsset, out string guid, out long _))
                {
                    uid = guid;
                }

                if (programBehaviour == null) // If no udonBehaviour is provided, we're opening a graph asset directly
                {
                    assetPath = AssetDatabase.GetAssetPath(programAsset);
                    scenePath = "";
                    this.programAsset = programAsset;
                }
                else
                {
                    assetPath = VRC.Core.ExtensionMethods.GetHierarchyPath(programBehaviour.transform);
                    scenePath = programBehaviour.gameObject.scene.path;
                    this.programAsset = programAsset;
                }
                
            }
        }
        
        [Serializable]
        public class GraphSettingsList
        {
            public GraphSettings[] graphSettingsArray;

            public static List<GraphSettings> GetGraphSettings()
            {
                return DeserializeGraphs(PlayerPrefs.GetString(OpenGraphsString));
            }
            
            public static void SetGraphSettings(IEnumerable<GraphSettings> graphSettings)
            {
                PlayerPrefs.SetString(OpenGraphsString, SerializeGraphs(graphSettings));
            }
            
            private GraphSettingsList(IEnumerable<GraphSettings> graphSettingsList)
            {
                graphSettingsArray = graphSettingsList.ToArray();
            }
            
            private static string SerializeGraphs(IEnumerable<GraphSettings> graphs)
            {
                GraphSettingsList graphSettingsList = new GraphSettingsList(graphs);
                return JsonUtility.ToJson(graphSettingsList);
            }

            private static List<GraphSettings> DeserializeGraphs(string value)
            {
                GraphSettingsList data = JsonUtility.FromJson<GraphSettingsList>(value) ?? new GraphSettingsList(Array.Empty<GraphSettings>()); 
                return new List<GraphSettings>(data.graphSettingsArray);
            }
        }

        // For Testing
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(UseNeonStyleString);
            PlayerPrefs.DeleteKey(SearchOnSelectedNodeRegistryString);
            PlayerPrefs.DeleteKey(GridSnapSizeString);
            PlayerPrefs.DeleteKey(SearchOnNoodleDropString);
            PlayerPrefs.DeleteKey(HighlightFlowString);
            PlayerPrefs.DeleteKey(OpenGraphsString);
            PlayerPrefs.DeleteKey(LastGraphIndexString);
            GraphSettingsList.SetGraphSettings(Array.Empty<GraphSettings>());
        }
        
        public static void CleanSerializedData()
        {
            List<GraphSettings> graphSettingsList = GraphSettingsList.GetGraphSettings();
            for (int index = graphSettingsList.Count - 1; index >= 0; index--)
            {
                GraphSettings openGraph = graphSettingsList[index];
                if (!GraphSettings.IsValid(openGraph))
                {
                    graphSettingsList.RemoveAt(index);
                }
            }
            GraphSettingsList.SetGraphSettings(graphSettingsList);
        }

        public static bool UseNeonStyle
        {
            get => PlayerPrefs.GetInt(UseNeonStyleString, 0) == 1;
            set => PlayerPrefs.SetInt(UseNeonStyleString, value ? 1 : 0);
        }

        public static int LastGraphIndex
        {
            get => PlayerPrefs.GetInt(LastGraphIndexString, 0);
            set => PlayerPrefs.SetInt(LastGraphIndexString, value);
        }
        
        public static GraphSettings GetGraph(string graphName)
        {
            List<GraphSettings> openGraphList = GraphSettingsList.GetGraphSettings();
            for (int index = openGraphList.Count - 1; index >= 0; index--)
            {
                GraphSettings t = openGraphList[index];
                if (t.programAsset == null) continue;
                if (t.programAsset.name != graphName) continue;
                return t;
            }

            return null;
        }
        
        public static GraphSettings GetLastGraph()
        { 
            // Fetch Open Graphs from Persistant Storage
            var openGraphList = GraphSettingsList.GetGraphSettings();
            int lastGraphIndex = LastGraphIndex;
            if (lastGraphIndex < 0 || lastGraphIndex >= openGraphList.Count)
            {
                return null;
            }
            return openGraphList[LastGraphIndex];
        }

        public static void SetLastGraph(UdonGraphProgramAsset programAsset, UdonBehaviour programBehaviour = null)
        {
            GraphSettings graphSettings = GraphSettings.Create(programAsset, programBehaviour);
            if (graphSettings != null)
            {
                SetLastGraph(graphSettings);
            }
        }

        public static void SetLastGraph(GraphSettings graphSettings)
        {
            var openGraphList = GraphSettingsList.GetGraphSettings();
            
            // flag to track if we've found the graph we're trying to open
            // in the opened graphs list
            bool isOpen = false;
            
            for (int i = 0; i < openGraphList.Count; i++)
            {
                GraphSettings graph = openGraphList[i];
                if (graph.uid != graphSettings.uid) continue;
                // Graph was open already, update it
                LastGraphIndex = i;
                openGraphList[i] = graphSettings;
                isOpen = true;
                break;
            }

            if (!isOpen)
            {
                // Open the graph
                openGraphList.Add(graphSettings);
                LastGraphIndex = openGraphList.Count - 1;
            }
            // Update Persistant Data
            GraphSettingsList.SetGraphSettings(openGraphList);
        }

        public static void CloseGraph(string graphName)
        {
            var graphSettings = GetGraph(graphName);
            if (graphSettings == null) return;
            CloseGraph(graphSettings);
        }

        private static void CloseGraph(GraphSettings graphSettings)
        {
            // stash the last graph
            var lastGraph = GetLastGraph();
            
            // Fetch Open Graphs from Persistant Storage
            var openGraphList = GraphSettingsList.GetGraphSettings();
            
            // Remove the graph from the list
            for (int index = openGraphList.Count - 1; index >= 0; index--)
            {
                GraphSettings setting = openGraphList[index];
                if (setting.uid == graphSettings.uid)
                    openGraphList.RemoveAt(index);
            }

            // restore the last graph index after mutating list
            LastGraphIndex = openGraphList.LastIndexOf(lastGraph);
            if(LastGraphIndex < 0)
                LastGraphIndex = openGraphList.Count - 1;
            
            // Update Persistant Data
            GraphSettingsList.SetGraphSettings(openGraphList);
        }
        
        public static bool SearchOnSelectedNodeRegistry
        {
            get => PlayerPrefs.GetInt(SearchOnSelectedNodeRegistryString, 1) == 1;
            set => PlayerPrefs.SetInt(SearchOnSelectedNodeRegistryString, value ? 1 : 0);
        }

        public static int GridSnapSize
        {
            get => PlayerPrefs.GetInt(GridSnapSizeString, 0);
            set => PlayerPrefs.SetInt(GridSnapSizeString, value);
        }

        public static bool SearchOnNoodleDrop
        {
            get => PlayerPrefs.GetInt(SearchOnNoodleDropString, 1) == 1;
            set => PlayerPrefs.SetInt(SearchOnNoodleDropString, value ? 1 : 0);
        }

        public static bool HighlightFlow
        {
            get => PlayerPrefs.GetInt(HighlightFlowString, 0) == 1;
            set => PlayerPrefs.SetInt(HighlightFlowString, value ? 1 : 0);
        }

        
    }
}