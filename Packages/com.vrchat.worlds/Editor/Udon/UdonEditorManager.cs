using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.EditorBindings;
using VRC.Udon.EditorBindings.Interfaces;
using VRC.Udon.Graph;
using VRC.Udon.Graph.Interfaces;
using VRC.Udon.UAssembly.Interfaces;

namespace VRC.Udon.Editor
{
    public class UdonEditorManager : IUdonEditorInterface
    {
        #region Singleton

        private static UdonEditorManager _instance;
        public static UdonEditorManager Instance => _instance ?? (_instance = new UdonEditorManager());

        #endregion

        #region Build Preprocessor Class

        private class UdonBuildPreprocessor : IProcessSceneWithReport
        {
            public int callbackOrder => 0;

            public void OnProcessScene(Scene scene, BuildReport report)
            {
                PopulateSceneSerializedProgramAssetReferences(scene);
                PopulateAssetDependenciesPrefabSerializedProgramAssetReferences(scene.path);
            }
        }

        #endregion

        #region Public Events

        public event Action WantRepaint;

        #endregion

        #region Private Constants

        private const double REFRESH_QUEUE_WAIT_PERIOD = 5.0;

        #endregion

        #region Private Fields

        private Lazy<UdonEditorInterface> _udonEditorInterface;

        private UdonEditorInterface UdonEditorInterface => _udonEditorInterface.Value;

        private readonly HashSet<AbstractUdonProgramSource> _programSourceRefreshQueue = new HashSet<AbstractUdonProgramSource>();

        #endregion

        #region Initialization

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            _instance = new UdonEditorManager();
        }

        #endregion

        #region Constructors

        private UdonEditorManager()
        {
            _udonEditorInterface = new Lazy<UdonEditorInterface>(() =>
            {
                var editorInterface = new UdonEditorInterface();
                editorInterface.AddTypeResolver(new UdonBehaviourTypeResolver());

                return editorInterface;
            });
            
            // Async init the editor interface to avoid 1+ second delay added to assembly reload.
            Task.Run(() =>
            {
                var _ = _udonEditorInterface.Value;
            });

            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        #endregion

        #region UdonBehaviour and ProgramSource Refresh

        public void QueueAndRefreshProgram(AbstractUdonProgramSource programSource)
        {
            QueueProgramSourceRefresh(programSource);
            RefreshQueuedProgramSources();
        }

        public void RefreshQueuedProgramSources()
        {
            foreach(AbstractUdonProgramSource programSource in _programSourceRefreshQueue)
            {
                if(programSource == null)
                {
                    return;
                }

                try
                {
                    programSource.RefreshProgram();
                }
                catch(Exception e)
                {
                    UnityEngine.Debug.LogError($"Failed to refresh program '{programSource.name}' due to exception '{e}'.");
                }
            }

            _programSourceRefreshQueue.Clear();

            WantRepaint?.Invoke();
        }

        public bool IsProgramSourceRefreshQueued(AbstractUdonProgramSource programSource)
        {
            if(_programSourceRefreshQueue.Count <= 0)
            {
                return false;
            }

            if(!_programSourceRefreshQueue.Contains(programSource))
            {
                return false;
            }

            return true;
        }

        public void QueueProgramSourceRefresh(AbstractUdonProgramSource programSource)
        {
            if(Application.isPlaying)
            {
                return;
            }

            if(programSource == null)
            {
                return;
            }

            if(IsProgramSourceRefreshQueued(programSource))
            {
                return;
            }

            _programSourceRefreshQueue.Add(programSource);
        }

        public void CancelQueuedProgramSourceRefresh(AbstractUdonProgramSource programSource)
        {
            if(programSource == null)
            {
                return;
            }

            if(_programSourceRefreshQueue.Contains(programSource))
            {
                _programSourceRefreshQueue.Remove(programSource);
            }
        }

        [MenuItem("VRChat SDK/Utilities/Re-compile All Program Sources")]
        public static void RecompileAllProgramSources()
        {
            string[] programSourceGUIDs = AssetDatabase.FindAssets("t:AbstractUdonProgramSource");
            foreach(string guid in programSourceGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AbstractUdonProgramSource programSource = AssetDatabase.LoadAssetAtPath<AbstractUdonProgramSource>(assetPath);
                if(programSource == null)
                {
                    continue;
                }
                programSource.RefreshProgram();
            }

            AssetDatabase.SaveAssets();

            PopulateAllPrefabSerializedProgramAssetReferences();
        }

        [PublicAPI]
        public static void PopulateAllPrefabSerializedProgramAssetReferences()
        {
            foreach(string prefabPath in GetAllPrefabAssetPaths())
            {
                PopulatePrefabSerializedProgramAssetReferences(prefabPath);
            }
        }

        private static List<UdonBehaviour> prefabBehavioursTempList = new List<UdonBehaviour>();

        [PublicAPI]
        public static void PopulateAssetDependenciesPrefabSerializedProgramAssetReferences(string assetPath)
        {
            IEnumerable<string> prefabDependencyPaths = AssetDatabase.GetDependencies(assetPath, true)
                .Where(path => path.EndsWith(".prefab"))
                .Where(path => path.StartsWith("Assets"));

            foreach(string prefabPath in prefabDependencyPaths)
            {
                if(!(AssetDatabase.LoadMainAssetAtPath(prefabPath) is GameObject prefab))
                {
                    return;
                }

                prefab.GetComponentsInChildren<UdonBehaviour>(prefabBehavioursTempList);
                if(prefabBehavioursTempList.Count < 1)
                {
                    return;
                }

                PopulatePrefabSerializedProgramAssetReferences(prefabPath);
            }
        }

        private static void PopulatePrefabSerializedProgramAssetReferences(string prefabPath)
        {
            using(EditPrefabAssetScope editScope = new EditPrefabAssetScope(prefabPath))
            {
                if(!editScope.IsEditable)
                {
                    return;
                }
            
                editScope.PrefabRoot.GetComponentsInChildren(prefabBehavioursTempList);
                if(prefabBehavioursTempList.Count < 1)
                {
                    return;
                }

                bool dirty = false;
                foreach(UdonBehaviour udonBehaviour in prefabBehavioursTempList)
                {
                    if(PopulateSerializedProgramAssetReference(udonBehaviour))
                    {
                        dirty = true;
                    }
                }

                if(dirty)
                {
                    editScope.MarkDirty();
                }
            }
        }

        #endregion

        #region Scene Manager Callbacks

        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            RefreshQueuedProgramSources();
            PopulateSceneSerializedProgramAssetReferences(scene);
        }

        private void OnSceneSaving(Scene scene, string _)
        {
            RefreshQueuedProgramSources();
            PopulateSceneSerializedProgramAssetReferences(scene);
        }
        
        private static void PopulateSceneSerializedProgramAssetReferences(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }
            
            foreach(GameObject sceneGameObject in scene.GetRootGameObjects())
            {
                foreach(UdonBehaviour udonBehaviour in sceneGameObject.GetComponentsInChildren<UdonBehaviour>(true))
                {
                    PopulateSerializedProgramAssetReference(udonBehaviour);
                }
            }
        }

        // Returns true if the serializedProgramProperty was changed, false otherwise.
        private static bool PopulateSerializedProgramAssetReference(UdonBehaviour udonBehaviour)
        {
            SerializedObject serializedUdonBehaviour = new SerializedObject(udonBehaviour);
            SerializedProperty programSourceSerializedProperty = serializedUdonBehaviour.FindProperty("programSource");
            SerializedProperty serializedProgramAssetSerializedProperty = serializedUdonBehaviour.FindProperty("serializedProgramAsset");

            if(!(programSourceSerializedProperty.objectReferenceValue is AbstractUdonProgramSource abstractUdonProgramSource))
            {
                return false;
            }

            if(abstractUdonProgramSource == null)
            {
                return false;
            }

            if(serializedProgramAssetSerializedProperty.objectReferenceValue == abstractUdonProgramSource.SerializedProgramAsset)
            {
                return false;
            }

            serializedProgramAssetSerializedProperty.objectReferenceValue = abstractUdonProgramSource.SerializedProgramAsset;
            serializedUdonBehaviour.ApplyModifiedPropertiesWithoutUndo();
            return true;

        }

        #endregion

        #region PlayMode Callback

        private void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            if(playModeStateChange != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            for(int index = 0; index < SceneManager.sceneCount; index++)
            {
                PopulateSceneSerializedProgramAssetReferences(SceneManager.GetSceneAt(index));
            }
        }

        #endregion

        #region IUdonEditorInterface Methods

        public IUdonVM ConstructUdonVM()
        {
            return UdonEditorInterface.ConstructUdonVM();
        }

        public IUdonProgram Assemble(string assembly)
        {
            return UdonEditorInterface.Assemble(assembly);
        }

        public IUdonWrapper GetWrapper()
        {
            return UdonEditorInterface.GetWrapper();
        }

        public IUdonHeap ConstructUdonHeap()
        {
            return UdonEditorInterface.ConstructUdonHeap();
        }

        public IUdonHeap ConstructUdonHeap(uint heapSize)
        {
            return UdonEditorInterface.ConstructUdonHeap(heapSize);
        }

        public string CompileGraph(
            IUdonCompilableGraph graph, INodeRegistry nodeRegistry,
            out Dictionary<string, (string uid, string fullName, int index)> linkedSymbols,
            out Dictionary<string, (object value, Type type)> heapDefaultValues
        )
        {
            return UdonEditorInterface.CompileGraph(graph, nodeRegistry, out linkedSymbols, out heapDefaultValues);
        }

        public Type GetTypeFromTypeString(string typeString)
        {
            return UdonEditorInterface.GetTypeFromTypeString(typeString);
        }

        public void AddTypeResolver(IUAssemblyTypeResolver typeResolver)
        {
            UdonEditorInterface.AddTypeResolver(typeResolver);
        }

        public string[] DisassembleProgram(IUdonProgram program)
        {
            return UdonEditorInterface.DisassembleProgram(program);
        }

        public string DisassembleInstruction(IUdonProgram program, ref uint offset)
        {
            return UdonEditorInterface.DisassembleInstruction(program, ref offset);
        }

        public UdonNodeDefinition GetNodeDefinition(string identifier)
        {
            return UdonEditorInterface.GetNodeDefinition(identifier);
        }

        public IEnumerable<UdonNodeDefinition> GetNodeDefinitions()
        {
            return UdonEditorInterface.GetNodeDefinitions();
        }

        public Dictionary<string, INodeRegistry> GetNodeRegistries()
        {
            return UdonEditorInterface.GetNodeRegistries();
        }

        private IReadOnlyDictionary<string, ReadOnlyCollection<KeyValuePair<string, INodeRegistry>>> _topRegistries;

        public IReadOnlyDictionary<string, ReadOnlyCollection<KeyValuePair<string, INodeRegistry>>> GetTopRegistries()
        {
            if (_topRegistries != null) return _topRegistries;

            var topRegistries = new Dictionary<string, List<KeyValuePair<string, INodeRegistry>>>()
            {
                {"System", new List<KeyValuePair<string, INodeRegistry>>()},
                {"Udon", new List<KeyValuePair<string, INodeRegistry>>()},
                {"VRC", new List<KeyValuePair<string, INodeRegistry>>()},
                {"UnityEngine", new List<KeyValuePair<string, INodeRegistry>>()},
            };

            // Go through each node registry and put it in the right parent registry
            foreach (KeyValuePair<string, INodeRegistry> nodeRegistry in GetNodeRegistries())
            {
                if (nodeRegistry.Key.StartsWith("System"))
                {
                    topRegistries["System"].Add(nodeRegistry);
                }
                else if (nodeRegistry.Key.StartsWith("Udon"))
                {
                    topRegistries["Udon"].Add(nodeRegistry);
                }
                else if (nodeRegistry.Key.StartsWith("VRC"))
                {
                    topRegistries["VRC"].Add(nodeRegistry);
                }
                else if (nodeRegistry.Key.StartsWith("UnityEngine"))
                {
                    topRegistries["UnityEngine"].Add(nodeRegistry);
                }
                else if (nodeRegistry.Key.StartsWith("Cinemachine"))
                {
                    topRegistries["UnityEngine"].Add(nodeRegistry);
                }
                else if (nodeRegistry.Key.StartsWith("TMPro"))
                {
                    topRegistries["UnityEngine"].Add(nodeRegistry);
                }
                else
                {
                    // Todo: note and handle these
                    UnityEngine.Debug.Log($"The Registry {nodeRegistry.Key} needs to be Added Somewhere");
                }
            }

            // Save result as cached variable
            _topRegistries = new ReadOnlyDictionary<string, ReadOnlyCollection<KeyValuePair<string, INodeRegistry>>>
            (
                topRegistries.ToDictionary(entry => entry.Key, entry => entry.Value.AsReadOnly())
            );
            
            // return cached version
           return _topRegistries;
        }

        private Dictionary<string, INodeRegistry> _registryLookup;

        private void CacheRegistryLookup()
        {
            _registryLookup = new Dictionary<string, INodeRegistry>();

            foreach (KeyValuePair<string, INodeRegistry> topRegistry in GetNodeRegistries())
            {
                // save top-level registry. do we need to do this? probably not
                _registryLookup.Add(topRegistry.Key, topRegistry.Value);

                foreach (KeyValuePair<string, INodeRegistry> registry in topRegistry.Value.GetNodeRegistries())
                {
                    _registryLookup.Add(registry.Key, registry.Value);
                }
            }
        }

        public bool TryGetRegistry(string name, out INodeRegistry registry)
        {
            if(_registryLookup == null)
            {
                CacheRegistryLookup();
            }
            return _registryLookup.TryGetValue(name, out registry);
        }

        public IEnumerable<UdonNodeDefinition> GetNodeDefinitions(string baseIdentifier)
        {
            return UdonEditorInterface.GetNodeDefinitions(baseIdentifier);
        }

        #endregion

        #region Prefab Utilities

        private static IEnumerable<string> GetAllPrefabAssetPaths()
        {
            return AssetDatabase.GetAllAssetPaths()
                .Where(path => path.EndsWith(".prefab"))
                .Where(path => path.StartsWith("Assets"));
        }

        private class EditPrefabAssetScope : IDisposable
        {
            private readonly string _assetPath;
            private readonly GameObject _prefabRoot;
            public GameObject PrefabRoot => _disposed ? null : _prefabRoot;

            private readonly bool _isEditable;
            public bool IsEditable => !_disposed && _isEditable;

            private bool _dirty = false;
            private bool _disposed;

            public EditPrefabAssetScope(string assetPath)
            {
                _assetPath = assetPath;
                _prefabRoot = PrefabUtility.LoadPrefabContents(_assetPath);
                _isEditable = !PrefabUtility.IsPartOfImmutablePrefab(_prefabRoot);
            }

            public void MarkDirty()
            {
                _dirty = true;
            }

            public void Dispose()
            {
                if(_disposed)
                {
                    return;
                }

                _disposed = true;

                if(_dirty)
                {
                    try
                    {
                        PrefabUtility.SaveAsPrefabAsset(_prefabRoot, _assetPath);
                    }
                    catch(Exception e)
                    {
                        UnityEngine.Debug.LogError($"Failed to save changes to prefab at '{_assetPath}' due to exception '{e}'.");
                    }
                }

                PrefabUtility.UnloadPrefabContents(_prefabRoot);
            }
        }

        #endregion
    }
}
