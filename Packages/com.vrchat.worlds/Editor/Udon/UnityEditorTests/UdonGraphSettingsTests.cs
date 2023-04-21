using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Udon;
using VRC.Udon.Editor.ProgramSources.UdonGraphProgram;
using VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView;
using Object = UnityEngine.Object;

namespace Tests
{
    public class UdonGraphSettingsTests
    {
        private const string TestAssetPath1 = "MaxAllPlayerVoice-TestGraph";
        private const string TestAssetPath2 = "SyncedSlider-TestGraph";

        [SetUp]
        public void Setup()
        {
            // Make sure we have a clean state
            Settings.CleanSerializedData();
            var graphs = Settings.GraphSettingsList.GetGraphSettings();
            
            foreach (var graph in graphs)
            {
                //Validate that the graph settings are in a good state
                Assert.IsNotNull(graph.programAsset); 
            }
        }
        
        [Test]
        public void UseNeonStyleTest()
        {
            Settings.Reset(); // Reset to default values
            bool useNeonStyle = Settings.UseNeonStyle;
            Assert.IsFalse(useNeonStyle); // Default value is false
            
            Settings.UseNeonStyle = true;
            Assert.IsTrue(Settings.UseNeonStyle); // Value should be true after setting it
        }

        [Test]
        public void SearchOnSelectedNodeRegistryTest()
        {
            Settings.Reset(); // Reset to default values
            bool searchOnSelectedNodeRegistry = Settings.SearchOnSelectedNodeRegistry;
            Assert.IsTrue(searchOnSelectedNodeRegistry); // Default value is true
            
            Settings.SearchOnSelectedNodeRegistry = false;
            Assert.IsFalse(Settings.SearchOnSelectedNodeRegistry); // Value should be false after setting it
        }
        
        [Test]
        public void GridSnapSizeTest()
        {
            Settings.Reset(); // Reset to default values
            int gridSnapSize = Settings.GridSnapSize;
            Assert.AreEqual(0, gridSnapSize); // Default value is 0
            
            Settings.GridSnapSize = 10;
            Assert.AreEqual(10, Settings.GridSnapSize); // Value should be 10 after setting it
        }
        
        [Test]
        public void SearchOnNoodleDropTest()
        {
            Settings.Reset(); // Reset to default values
            bool searchOnNoodleDrop = Settings.SearchOnNoodleDrop;
            Assert.IsTrue(searchOnNoodleDrop); // Default value is true
            
            Settings.SearchOnNoodleDrop = false;
            Assert.IsFalse(Settings.SearchOnNoodleDrop); // Value should be false after setting it
        }
        
        [Test]
        public void HighlightFlowTest()
        {
            Settings.Reset(); // Reset to default values
            bool highlightFlow = Settings.HighlightFlow;
            Assert.IsFalse(highlightFlow); // Default value is false
            
            Settings.HighlightFlow = true;
            Assert.IsTrue(Settings.HighlightFlow); // Value should be true after setting it
        }
        
        [Test]
        public void LastGraphIndexTest()
        {
            Settings.Reset(); // Reset to default values
            int lastGraphIndex = Settings.LastGraphIndex;
            Assert.AreEqual(0, lastGraphIndex); // Default value is 0
            
            Settings.LastGraphIndex = 10;
            Assert.AreEqual(10, Settings.LastGraphIndex); // Value should be 10 after setting it
        }

        [Test]
        public void SerializeGraphsTest()
        {
            // Create Test Data
            List<Settings.GraphSettings> testGraphs = new List<Settings.GraphSettings>();
            Settings.GraphSettings testGraph1 = LoadTestGraphSetting(TestAssetPath1);
            Settings.GraphSettings testGraph2 = LoadTestGraphSetting(TestAssetPath2);
            testGraphs.Add(testGraph1);
            testGraphs.Add(testGraph2);
            
            // Serialize test data
            Settings.GraphSettingsList.SetGraphSettings(testGraphs);
            // Deserialize test data
            List<Settings.GraphSettings> deserializedGraphs = Settings.GraphSettingsList.GetGraphSettings();
            
            // Serialized and de-/re-Serialized graphs should match (Makes sure data isn't mutated)
            for (int i = 0; i < deserializedGraphs.Count; i++)
            {
                Settings.GraphSettings deSerializedGraph = deserializedGraphs[i];
                Settings.GraphSettings preSerializedGraph = testGraphs[i];
                
                // Graphs should have the same data
                Assert.AreEqual(preSerializedGraph.uid, deSerializedGraph.uid); 
                Assert.AreEqual(preSerializedGraph.scenePath, deSerializedGraph.scenePath); 
                Assert.AreEqual(preSerializedGraph.assetPath, deSerializedGraph.assetPath); 
                Assert.AreEqual(preSerializedGraph.programAsset, deSerializedGraph.programAsset);
            }
        }

        private static Settings.GraphSettings LoadTestGraphSetting(string graphAssetPath)
        {
            // Load test graph program assets from disk
            UdonGraphProgramAsset graphAsset =
                Resources.Load(graphAssetPath, typeof(UdonGraphProgramAsset)) as UdonGraphProgramAsset;
            //Create test graph settings from the assets
            Settings.GraphSettings testGraph = Settings.GraphSettings.Create(graphAsset);
            Assert.IsNotNull(testGraph);
            return testGraph;
        }

        [Test]
        public void LastGraphTest()
        {
            // Load test graph program assets from disk
            Settings.GraphSettings testGraph1 = LoadTestGraphSetting(TestAssetPath1);
            Settings.GraphSettings testGraph2 = LoadTestGraphSetting(TestAssetPath2);
            
            Settings.SetLastGraph(testGraph1);
            Settings.SetLastGraph(testGraph2);
            Settings.GraphSettings lastGraph = Settings.GetLastGraph();
            
            Assert.AreEqual(testGraph2.uid, lastGraph.uid); // Graph UIDs should match
            Assert.AreEqual(testGraph2.scenePath, lastGraph.scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph2.assetPath, lastGraph.assetPath); // Graph asset paths should match
            
            Settings.SetLastGraph(testGraph1);
            lastGraph = Settings.GetLastGraph();
            Assert.AreEqual(testGraph1.uid, lastGraph.uid); // Graph UIDs should match
            Assert.AreEqual(testGraph1.scenePath, lastGraph.scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph1.assetPath, lastGraph.assetPath); // Graph asset paths should match
            
            Settings.LastGraphIndex = -1; // Set index to -1 to test if it returns null
            lastGraph = Settings.GetLastGraph();
            Assert.IsNull(lastGraph); // Last graph should be null
        }
        
        [Test]
        public void LastGraphFromAssetTest()
        {
            // Load test graph program assets from disk
            UdonGraphProgramAsset graphAsset1 = Resources.Load("MaxAllPlayerVoice-TestGraph", typeof(UdonGraphProgramAsset)) as UdonGraphProgramAsset;
            UdonGraphProgramAsset graphAsset2 = Resources.Load("SyncedSlider-TestGraph", typeof(UdonGraphProgramAsset)) as UdonGraphProgramAsset;
            
            Settings.SetLastGraph(graphAsset1);
            Settings.SetLastGraph(graphAsset2);
            Settings.GraphSettings lastGraph = Settings.GetLastGraph();
            
            Assert.AreEqual(graphAsset2, lastGraph.programAsset); // Graphs should match
        }

        [Test]
        public void GetGraphTest()
        {
            Settings.GraphSettings testGraph = LoadTestGraphSetting(TestAssetPath1);
            Settings.SetLastGraph(testGraph);
            
            Settings.GraphSettings graph = Settings.GetGraph(TestAssetPath1);
            Assert.AreEqual(testGraph.uid, graph.uid); // Graph UIDs should match
            
            // Try to get a graph that doesn't exist
            graph = Settings.GetGraph("123456789");
            Assert.IsNull(graph); // Graph should be null
        }

        [Test]
        public void GetGraphFromSceneTest() 
        {
            // Open test scene
            var scene = Resources.Load("UdonEditorTestScene") as SceneAsset;
            string scenePath = AssetDatabase.GetAssetPath(scene);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            
            // Load test graph programs from scene
            UdonBehaviour[] udonBehaviours = Object.FindObjectsOfType<UdonBehaviour>();
            Assert.AreEqual(2, udonBehaviours.Length);

            UdonGraphProgramAsset graphAsset1 = udonBehaviours[0].programSource as UdonGraphProgramAsset;
            UdonGraphProgramAsset graphAsset2 = udonBehaviours[1].programSource as UdonGraphProgramAsset;
            Assert.IsNotNull(graphAsset1);
            Assert.IsNotNull(graphAsset2);
            
            // Close test graphs
            Settings.CloseGraph(graphAsset1.name);
            Settings.CloseGraph(graphAsset2.name);
            
            // Validate graph settings
            List<Settings.GraphSettings> openGraphs = Settings.GraphSettingsList.GetGraphSettings();
            Assert.AreEqual(0, openGraphs.Count); // There should be 0 open graphs
            
            // Open graphs in GraphWindow            
            Settings.SetLastGraph(graphAsset1, udonBehaviours[0]);
            Settings.SetLastGraph(graphAsset2, udonBehaviours[1]);
            
            // Validate graph settings
            openGraphs = Settings.GraphSettingsList.GetGraphSettings();
            Assert.AreEqual(2, openGraphs.Count); // There should be 2 open graphs
            
            Assert.AreEqual(udonBehaviours[0].gameObject.scene.path, openGraphs[0].scenePath); // Graph scene paths should match
            // TODO: Expected: <UdonGraphProgramAsset: SyncedSlider-TestGraph> But was:  <UdonGraphProgramAsset: MaxAllPlayerVoice-TestGraph>
            Assert.AreEqual(udonBehaviours[0].programSource, openGraphs[0].programAsset); // Graph assets should match
            
            Assert.AreEqual(udonBehaviours[1].gameObject.scene.path, openGraphs[1].scenePath); // Graph scene paths should match
            Assert.AreEqual(udonBehaviours[1].programSource, openGraphs[1].programAsset); // Graph assets should match
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            //yield return new WaitForDomainReload();
            
            // Validate graph settings
            openGraphs = Settings.GraphSettingsList.GetGraphSettings();
            Assert.AreEqual(2, openGraphs.Count); // There should be 2 open graphs
            
            Assert.AreEqual(udonBehaviours[0].gameObject.scene.path, openGraphs[0].scenePath); // Graph scene paths should match
            Assert.AreEqual(udonBehaviours[0].programSource, openGraphs[0].programAsset); // Graph assets should match
            
            Assert.AreEqual(udonBehaviours[1].gameObject.scene.path, openGraphs[1].scenePath); // Graph scene paths should match
            Assert.AreEqual(udonBehaviours[1].programSource, openGraphs[1].programAsset); // Graph assets should match
        }

        [Test]
        public void GetOpenGraphsTest()
        {
            Settings.GraphSettings testGraph1 = LoadTestGraphSetting(TestAssetPath1);
            Settings.GraphSettings testGraph2 = LoadTestGraphSetting(TestAssetPath2);
            
            // Close test graphs
            Settings.GraphSettingsList.SetGraphSettings(Array.Empty<Settings.GraphSettings>());
            
            // Validate graph settings
            List<Settings.GraphSettings> openGraphs = Settings.GraphSettingsList.GetGraphSettings();
            Assert.AreEqual(0, openGraphs.Count); // There should be 0 open graphs
            
            Settings.SetLastGraph(testGraph1);
            Settings.SetLastGraph(testGraph2);
            openGraphs = Settings.GraphSettingsList.GetGraphSettings();
            Assert.AreEqual(2, openGraphs.Count); // There should be 2 open graphs
            
            Assert.AreEqual(testGraph1.uid, openGraphs[0].uid); // Graph UIDs should match
            Assert.AreEqual(testGraph1.scenePath, openGraphs[0].scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph1.assetPath, openGraphs[0].assetPath); // Graph asset paths should match
            
            Assert.AreEqual(testGraph2.uid, openGraphs[1].uid); // Graph UIDs should match
            Assert.AreEqual(testGraph2.scenePath, openGraphs[1].scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph2.assetPath, openGraphs[1].assetPath); // Graph asset paths should match
        }

        [Test]
        public void ChangeLastGraphTest()
        {
            Settings.GraphSettings testGraph1 = LoadTestGraphSetting(TestAssetPath1);
            Settings.GraphSettings testGraph2 = LoadTestGraphSetting(TestAssetPath2);
            
            // Close test graphs
            Settings.GraphSettingsList.SetGraphSettings(Array.Empty<Settings.GraphSettings>());
            
            // Validate graph settings
            List<Settings.GraphSettings> openGraphs = Settings.GraphSettingsList.GetGraphSettings();
            Assert.AreEqual(0, openGraphs.Count); // There should be 0 open graphs
            
            Settings.SetLastGraph(testGraph1);
            Settings.SetLastGraph(testGraph2);
            openGraphs = Settings.GraphSettingsList.GetGraphSettings();
            Assert.AreEqual(2, openGraphs.Count); // There should be 2 open graphs
            
            Assert.AreEqual(testGraph1.uid, openGraphs[0].uid); // Graph UIDs should match
            Assert.AreEqual(testGraph1.scenePath, openGraphs[0].scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph1.assetPath, openGraphs[0].assetPath); // Graph asset paths should match
            
            Assert.AreEqual(testGraph2.uid, openGraphs[1].uid); // Graph UIDs should match
            Assert.AreEqual(testGraph2.scenePath, openGraphs[1].scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph2.assetPath, openGraphs[1].assetPath); // Graph asset paths should match
            
            // Graph 2 should be the last selected graph and the index should point to its original position in the list
            var lastGraph = Settings.GetLastGraph();
            
            Assert.AreEqual(testGraph2.uid, lastGraph.uid); // Graph UIDs should match
            Assert.AreEqual(testGraph2.scenePath, lastGraph.scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph2.assetPath, lastGraph.assetPath); // Graph asset paths should match

            int lastGraphIndex = Settings.LastGraphIndex;
            Assert.AreEqual(1, lastGraphIndex); // Graph index should be 1
            
            Settings.SetLastGraph(testGraph1);
            
            openGraphs = Settings.GraphSettingsList.GetGraphSettings();
            Assert.AreEqual(2, openGraphs.Count); // There should be 2 open graphs
            
            // Graph 1 should still be first in the list, despite being selected last
            Assert.AreEqual(testGraph1.uid, openGraphs[0].uid); // Graph UIDs should match
            Assert.AreEqual(testGraph1.scenePath, openGraphs[0].scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph1.assetPath, openGraphs[0].assetPath); // Graph asset paths should match
            
            Assert.AreEqual(testGraph2.uid, openGraphs[1].uid); // Graph UIDs should match
            Assert.AreEqual(testGraph2.scenePath, openGraphs[1].scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph2.assetPath, openGraphs[1].assetPath); // Graph asset paths should match
            
            // Graph 1 should be the last selected graph and the index should point to its original position in the list
            lastGraph = Settings.GetLastGraph();
            
            Assert.AreEqual(testGraph1.uid, lastGraph.uid); // Graph UIDs should match
            Assert.AreEqual(testGraph1.scenePath, lastGraph.scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph1.assetPath, lastGraph.assetPath); // Graph asset paths should match

            lastGraphIndex = Settings.LastGraphIndex;
            Assert.AreEqual(0, lastGraphIndex); // Graph index should be 0
            
        }
        
        [Test]
        public void CloseGraphTest()
        {
            Settings.GraphSettings testGraph1 = LoadTestGraphSetting(TestAssetPath1);
            Settings.GraphSettings testGraph2 = LoadTestGraphSetting(TestAssetPath2);
            
            // Close test graphs
            Settings.GraphSettingsList.SetGraphSettings(Array.Empty<Settings.GraphSettings>());
            
            // Validate graph settings
            List<Settings.GraphSettings> openGraphs = Settings.GraphSettingsList.GetGraphSettings();
            Assert.AreEqual(0, openGraphs.Count); // There should be 0 open graphs
         
            Settings.SetLastGraph(testGraph1);
            Settings.SetLastGraph(testGraph2);
            openGraphs = Settings.GraphSettingsList.GetGraphSettings();
            Assert.AreEqual(2, openGraphs.Count); // There should be 2 open graphs

            Assert.AreEqual(testGraph1.uid, openGraphs[0].uid); // Graph UIDs should match
            Assert.AreEqual(testGraph1.scenePath, openGraphs[0].scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph1.assetPath, openGraphs[0].assetPath); // Graph asset paths should match
            
            Assert.AreEqual(testGraph2.uid, openGraphs[1].uid); // Graph UIDs should match
            Assert.AreEqual(testGraph2.scenePath, openGraphs[1].scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph2.assetPath, openGraphs[1].assetPath); // Graph asset paths should match
            
            // Close last graph
            Settings.CloseGraph(testGraph2.programAsset.name);
            
            openGraphs = Settings.GraphSettingsList.GetGraphSettings();
            Assert.AreEqual(1, openGraphs.Count); // There should be 1 open graph
            
            Settings.GraphSettings lastGraph = Settings.GetLastGraph();
            // Last graph should be the first graph now
            Assert.AreEqual(testGraph1.uid, lastGraph.uid); // Graph UIDs should match
            Assert.AreEqual(testGraph1.scenePath, lastGraph.scenePath); // Graph scene paths should match
            Assert.AreEqual(testGraph1.assetPath, lastGraph.assetPath); // Graph asset paths should match
            
            // Try closing something that doesn't exist
            Settings.CloseGraph("This graph doesn't exist");
            
            openGraphs = Settings.GraphSettingsList.GetGraphSettings();
            Assert.AreEqual(1, openGraphs.Count); // There should still be 1 open graph
        }
        
        [Test]
        public void ResetSettings()
        {
            Settings.Reset();
            Assert.IsFalse(Settings.HighlightFlow); // HighlightFlow should be false
            Assert.AreEqual(0, Settings.LastGraphIndex); // LastGraphIndex should be 0
            Assert.IsNull(Settings.GetLastGraph()); // LastGraph should be null
        }
        
    }
}
