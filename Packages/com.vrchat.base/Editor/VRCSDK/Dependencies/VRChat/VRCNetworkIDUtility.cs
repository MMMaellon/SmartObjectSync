
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using System;
using BestHTTP.JSON;
using System.Linq;
using System.Data;
using VRC;

public class VRCNetworkIDUtility : EditorWindow
{
    public class NetworkObjectRef
    {
        public int ID;
        public GameObject gameObject;
        public string gameObjectPath;
        public List<string> typeNames;

        public override bool Equals(object obj)
        {
            if (!(obj is NetworkObjectRef otherRef) || otherRef == null)
                return false;

            return ID.Equals(otherRef.ID) && gameObjectPath.Equals(otherRef.gameObjectPath);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public enum ConflictType
    {
        ID,
        Object,
        NotFound,
        NewID,
        TypeMismatch
    }

    public static Dictionary<ConflictType, string> ConflictTypeNames = new Dictionary<ConflictType, string> {
        { ConflictType.ID, "Identifier Mismatch" },
        { ConflictType.Object, "Object Mismatch" },
        { ConflictType.NotFound, "Object not in Scene" },
        { ConflictType.NewID, "New Identifier from File" },
        { ConflictType.TypeMismatch, "Network Components Changed" }
    };

    public enum ConflictResolution
    {
        Nothing,
        IgnoreAll,
        AcceptAll,
        SelectOne,
        IgnoreOne,
    }

    public static Dictionary<ConflictResolution, string> ConflictResolutionNames = new Dictionary<ConflictResolution, string> {
        { ConflictResolution.Nothing, "" },
        { ConflictResolution.IgnoreAll, "Ignore All" },
        { ConflictResolution.AcceptAll, "Accept All" },
        { ConflictResolution.SelectOne, "Select" },
        { ConflictResolution.IgnoreOne, "Ignore" }
    };

    public static Dictionary<ConflictType, ConflictResolution> MassConflictResolutions = new Dictionary<ConflictType, ConflictResolution>
    {
        { ConflictType.ID, ConflictResolution.IgnoreAll },
        { ConflictType.Object, ConflictResolution.IgnoreAll },
        { ConflictType.NotFound, ConflictResolution.IgnoreAll },
        { ConflictType.NewID, ConflictResolution.AcceptAll },
        { ConflictType.TypeMismatch, ConflictResolution.Nothing },
    };

    public class Conflict
    {
        public ConflictType Type;

        public Conflict(ConflictType type)
            => Type = type;

        public bool IsMatch(NetworkObjectRef objRef)
            => (Type == ConflictType.Object && IDs.Contains(objRef.ID)) 
                || (Type == ConflictType.ID && Paths.Contains(objRef.gameObjectPath))
                || (Type == ConflictType.NotFound && Paths.Contains(objRef.gameObjectPath))
                || (Type == ConflictType.TypeMismatch && IDs.Contains(objRef.ID));
        
        public bool IsMatch(VRC.SDKBase.Network.NetworkIDPair netRef, VRCNetworkIDUtility utility)
            => (Type == ConflictType.Object && IDs.Contains(netRef.ID)) 
                || (Type == ConflictType.ID && Paths.Contains(utility.Path(netRef.gameObject)))
                || (Type == ConflictType.TypeMismatch && IDs.Contains(netRef.ID));

        public void AddScene(NetworkObjectRef objRef)
        {
            if (objRef == null || SceneRefs.Any(existing => existing.Equals(objRef)))
                return;

            SceneRefs.Add(objRef);
            AddRef(objRef);
        }

        public void AddLoaded(NetworkObjectRef objRef)
        {
            if (objRef == null || LoadedRefs.Any(existing => existing.Equals(objRef)))
                return;

            LoadedRefs.Add(objRef);
            AddRef(objRef);
        }

        private void AddRef(NetworkObjectRef objRef)
        {            
            if (objRef.ID > 0)
                IDs.Add(objRef.ID);
            if (!string.IsNullOrWhiteSpace(objRef.gameObjectPath))
                Paths.Add(objRef.gameObjectPath);
        }

        public HashSet<int> IDs = new HashSet<int>();
        public HashSet<string> Paths = new HashSet<string>();
        public List<NetworkObjectRef> SceneRefs = new List<NetworkObjectRef>();
        public List<NetworkObjectRef> LoadedRefs = new List<NetworkObjectRef>();
    }

    private static GUIStyle titleGuiStyle;
    private static GUIStyle objectAreaGuiStyle;
    private static GUIStyle conflictStyle;
    private static GUIStyle noConflictStyle;
    private static GUIStyle conflictGroupStyle;

    private const string titleName = "Network ID Utility";

    private Vector2 scrollPos;
    private INetworkIDContainer networkTarget;

    private List<INetworkIDContainer> networkTargets = new List<INetworkIDContainer>();
    private string[] networkTargetNames;

    private List<Conflict> conflicts = new List<Conflict>();
    Dictionary<int, NetworkObjectRef> fileRefs = new Dictionary<int, NetworkObjectRef>();

    bool didInit = false;

    [MenuItem("VRChat SDK/Utilities/Network ID Import and Export Utility", false, 990)]
    static void Create()
    {
        VRCNetworkIDUtility window = EditorWindow.GetWindow<VRCNetworkIDUtility>();
        window.titleContent = new GUIContent(titleName);
        window.minSize = new Vector2(325, 410);
        window.Show();
    }

    void Init()
    {
        if (didInit)
            return;

        titleGuiStyle = new GUIStyle
        {
            fontSize = 15,
            fontStyle = FontStyle.BoldAndItalic,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        if (EditorGUIUtility.isProSkin)
            titleGuiStyle.normal.textColor = Color.white;
        else
            titleGuiStyle.normal.textColor = Color.black;

        conflictGroupStyle = new GUIStyle(GUI.skin.box);
        conflictGroupStyle.normal.background = Texture2D.linearGrayTexture;
        conflictGroupStyle.padding = new RectOffset(2, 2, 2, 2);
        conflictGroupStyle.margin = new RectOffset(2, 2, 2, 2);

        conflictStyle = new GUIStyle(GUI.skin.box);
        conflictStyle.normal.background = Texture2D.blackTexture;
        conflictStyle.padding = new RectOffset(2, 2, 2, 2);
        conflictStyle.margin = new RectOffset(2, 2, 2, 2);

        noConflictStyle = new GUIStyle(GUI.skin.box);
        noConflictStyle.padding = new RectOffset(2, 2, 2, 2);
        noConflictStyle.margin = new RectOffset(2, 2, 2, 2);

        objectAreaGuiStyle = new GUIStyle();
        objectAreaGuiStyle.padding = new RectOffset(5, 5, 5, 5);
    }

    void OnGUI()
    {
        if (Application.isPlaying)
        {
            this.Close();
            return;
        }

        Init();

        GUILayout.Space(10);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label(titleName, titleGuiStyle);
            GUILayout.FlexibleSpace();
        }

        GUILayout.Space(15);

        //Find network targets
        if(networkTarget == null || networkTargets.Count == 0)
            FindNetworkTargets();

        if (networkTarget == null)
        {
            //Choose if available
            if(networkTargets.Count > 0)
                SetTarget(networkTargets[0]);
            else
            {
                //Non-found
                using(new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Please load a scene with a Scene or Avatar Descriptor.", EditorStyles.helpBox, GUILayout.ExpandWidth(true));
                    GUILayout.FlexibleSpace();
                }
                return;
            }
        }

        //Choose target
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        var targetIndex = networkTargets.IndexOf(networkTarget);
        targetIndex = EditorGUILayout.Popup("Target", targetIndex, networkTargetNames);
        if(EditorGUI.EndChangeCheck())
        {
            SetTarget(networkTargets[targetIndex]);
        }
        if(GUILayout.Button("Refresh", GUILayout.Width(80)))
        {
            FindNetworkTargets();
        }
        EditorGUILayout.EndHorizontal();

        // When players delete game objects they aren't automagically removed from the list, yet
        networkTarget.NetworkIDCollection.RemoveAll(pair => pair.gameObject == null);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Export", GUILayout.Width(80)))
                Export();

            if (GUILayout.Button("Import", GUILayout.Width(80)))
                Import();

            GUILayout.FlexibleSpace();
        }

        GUILayout.Space(5);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear Scene IDs", GUILayout.Width(150)))
                Clear();

            if (GUILayout.Button("Regenerate Scene IDs", GUILayout.Width(150)))
                Regenerate();

            GUILayout.FlexibleSpace();
        }

        GUILayout.Space(15);

        // Retest for conflicts
        var currentRefs = networkTarget.NetworkIDCollection
            .Where(nid => !conflicts.Any(c => c.IsMatch(nid, this)))
            .OrderBy(nid => nid.ID)
            .ToDictionary(
                nid => nid.ID,
                nid => new NetworkObjectRef {
                    ID = nid.ID,
                    gameObject = nid.gameObject,
                    gameObjectPath = Path(nid.gameObject),
                    typeNames = VRC.SDKBase.Network.NetworkIDAssignment.GetSerializedTypes(nid.gameObject)
                });

        DetectConflicts(currentRefs, conflicts);

        var hasDuplicatePaths = networkTarget.NetworkIDCollection
            .GroupBy(nid => Path(nid.gameObject))
            .Any(group => group.Count() > 1);
        if (hasDuplicatePaths)
            EditorGUILayout.HelpBox("Some networked behaviours share the same transform path, this tool is not reliable under these circumstances.", MessageType.Warning);

        using (new EditorGUILayout.VerticalScope(objectAreaGuiStyle))
        using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos, false, false))
        {
            scrollPos = scrollView.scrollPosition;
            
            foreach (ConflictType type in System.Enum.GetValues(typeof(ConflictType)))
            {
                IEnumerable<Conflict> found = conflicts.Where(c => c.Type == type).ToArray();
                if (found.FirstOrDefault() == null)
                    continue;

                using (new EditorGUILayout.VerticalScope(conflictGroupStyle))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(ConflictTypeNames[type]);
                        GUILayout.FlexibleSpace();

                        ConflictResolution massResolution = MassConflictResolutions[type];
                        if (massResolution != ConflictResolution.Nothing
                            && GUILayout.Button(ConflictResolutionNames[massResolution]))
                        {
                            switch (massResolution)
                            {
                                default:
                                    break;
                                case ConflictResolution.IgnoreAll:
                                    conflicts.RemoveAll(c => c.Type == type);
                                    break;
                                case ConflictResolution.AcceptAll:
                                    foreach (Conflict conflict in conflicts.Where(c => c.Type == type).ToArray())
                                        foreach (NetworkObjectRef objRef in conflict.LoadedRefs.Concat(conflict.SceneRefs).ToArray())
                                            UseRef(objRef);
                                    break;
                            }
                            continue;
                        }
                    }

                    int remaining = found.Count();
                    foreach (Conflict conflict in found)
                    {
                        DrawConflict(conflict);
                        remaining--;
                        
                        if (remaining > 0)
                            EditorGUILayout.Space(5);
                    }
                }

                EditorGUILayout.Space(15);
            }

            foreach (var netRef in networkTarget.NetworkIDCollection)
            {
                if (!conflicts.Any(c => c.IsMatch(netRef, this)))
                {
                    DrawNoConflict(netRef);
                    EditorGUILayout.Space(5);
                }
            }
        }
        
    }

    void Clear()
    {
        if (EditorUtility.DisplayDialog("Clear Scene IDs", "Do you wish to clear all recorded network IDs from the scene?", "Clear IDs", "No"))
        {
            networkTarget.NetworkIDCollection.Clear();
            conflicts.Clear();
            fileRefs.Clear();
            
            ((Component)networkTarget).gameObject.MarkDirty();
            PrefabUtility.RecordPrefabInstancePropertyModifications(((Component)networkTarget));
        }
    }

    void Regenerate()
    {
        if (EditorUtility.DisplayDialog("Generate New Scene IDs", "Do you wish to clear all recorded network IDs from the scene, and create new ones?", "Generate New IDs", "No"))
        {
            networkTarget.NetworkIDCollection.Clear();
            conflicts.Clear();
            fileRefs.Clear();

            var (_, newIDs) = VRC.SDKBase.Network.NetworkIDAssignment.ConfigureNetworkIDs(networkTarget, out List<VRC.SDKBase.Network.NetworkIDAssignment.SetErrorLocation> errors, VRC.SDKBase.Network.NetworkIDAssignment.SetError.IncompatibleTypes);
            if (errors.Count > 0)
            {
                Debug.LogError($"Ran into {errors.Count} errors while regenerating IDs.");
            }
            if (newIDs.Count() > 0)
            {
                ((Component)networkTarget).gameObject.MarkDirty();
                PrefabUtility.RecordPrefabInstancePropertyModifications(((Component)networkTarget));
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }
    }

    void Export()
    {
        var dict = networkTarget.NetworkIDCollection.ToDictionary(
            netRef => netRef.ID.ToString(),
            netRef => Path(netRef.gameObject));

        if (dict.Values.GroupBy(objPath => objPath).Any(group => group.Count() > 1)
            && !EditorUtility.DisplayDialog("Duplicate Paths Found", "Some networked behaviours share the same transform path, and so an ID export will not contain all objects. Should export continue?", "Contine Export", "No"))
            return;

        var activeScene = ((Component)networkTarget).gameObject.scene;
        string filePrefix;
        switch(networkTarget)
        {
            case VRC_SceneDescriptor sceneDesc:
                filePrefix = activeScene == null ? "" : activeScene.name + "_"; // Should never be null, of course.
                break;
            default:
                filePrefix = ((Component)networkTarget).gameObject.name;
                break;
        }

        string path = activeScene == null ? Application.dataPath : Application.dataPath + activeScene.path;
        string savePath = EditorUtility.SaveFilePanel("Save Network ID Associations", path, $"{filePrefix}_network_ids", "json");
        if (string.IsNullOrWhiteSpace(savePath))
            return;
        
        string json =  Json.Encode(dict);
        System.IO.File.WriteAllText(savePath, json, System.Text.Encoding.UTF8);
    }

    void Import()
    {
        string loadPath = EditorUtility.OpenFilePanelWithFilters("Open Network ID Associations", Application.dataPath, new string[] { "Network ID Dictionary", "json" });
        if (string.IsNullOrWhiteSpace(loadPath))
            return;
        
        string json = System.IO.File.ReadAllText(loadPath, System.Text.Encoding.UTF8);
        Json.Token tokenized = Json.Decode(json);

        Json.JObject obj = tokenized.TryGetObject();
        if (null == obj)
        {
            Debug.LogError($"Failed to load object, instead had {tokenized.Type}");
            return;
        }        

        Dictionary<int, NetworkObjectRef> loadedRefs = new Dictionary<int, NetworkObjectRef>();
        foreach (var key in obj.Keys)
        {
            if (!int.TryParse(key, out int ID))
            {
                Debug.LogError($"Failed to read object, could not parse key {key}");
                continue;
            }
            
            var value = obj[key];
            if (value.Type != Json.TokenType.String)
            {
                Debug.LogError($"Failed to read object, found value type {value.Type}");
                continue;
            }
            
            string path = value.StringInstance;

            // Want to avoid duplicate path entries            
            if (!loadedRefs.Values.Any(objRef => objRef.gameObjectPath == path))
                loadedRefs.Add(ID, new NetworkObjectRef {
                    ID = ID,
                    gameObject = networkTarget.FindNetworkIDGameObject(path),
                    gameObjectPath = path
                });
            else
                Debug.LogError($"Duplicate Path using ID {ID} was loaded from json: {path}");
        }

        fileRefs = loadedRefs;
        conflicts.Clear();
        DetectConflicts(loadedRefs, conflicts);
    }

    private struct ObjectPathEqualityComparer : IEqualityComparer<KeyValuePair<int, NetworkObjectRef>>
    {
        public bool Equals(KeyValuePair<int, NetworkObjectRef> x, KeyValuePair<int, NetworkObjectRef> y) => x.Value.gameObjectPath == y.Value.gameObjectPath;
        public int GetHashCode(KeyValuePair<int, NetworkObjectRef> obj) => obj.Value.gameObjectPath.GetHashCode();
    }

    void DetectConflicts(Dictionary<int, NetworkObjectRef> loadedRefs, List<Conflict> conflictList)
    {
        Dictionary<string, NetworkObjectRef> loadedPaths = new Dictionary<string, NetworkObjectRef>();
        foreach (var kvp in loadedRefs)
            if (!loadedPaths.ContainsKey(kvp.Value.gameObjectPath))
                loadedPaths.Add(kvp.Value.gameObjectPath, kvp.Value);
        
        var sceneRefs = networkTarget.NetworkIDCollection
            .OrderBy(nid => nid.ID)
            .Where(nid => nid.gameObject != null)
            .ToDictionary(
                nid => nid.ID,
                nid => new NetworkObjectRef {
                    ID = nid.ID,
                    gameObject = nid.gameObject,
                    gameObjectPath = Path(nid.gameObject),
                    typeNames = nid.SerializedTypeNames
                });
        var scenePaths = sceneRefs
            .Distinct(new ObjectPathEqualityComparer())
            .ToDictionary(kvp => kvp.Value.gameObjectPath, kvp => kvp.Value);

        // Loaded that match an ID or Path
        foreach (NetworkObjectRef sceneRef in sceneRefs.Values.OrderBy(t => t.ID))
        {
            // Is there a stored ID match?
            if (loadedRefs.TryGetValue(sceneRef.ID, out NetworkObjectRef loadedRefByID))
            {
                // Do they match refs?
                if (loadedRefByID.gameObject != sceneRef.gameObject)
                    RecordConflict(sceneRef, loadedRefByID, ConflictType.Object);
                
                // Do they match types?
                if (!DoTypesMatch(sceneRef, loadedRefByID))
                    RecordConflict(sceneRef, loadedRefByID, ConflictType.TypeMismatch);
            }

            // Is there a stored object match?
            if (loadedPaths.TryGetValue(Path(sceneRef.gameObject), out NetworkObjectRef loadedRefByPath))
            {
                // Do they match ids?
                if (loadedRefByPath.ID != sceneRef.ID)
                    RecordConflict(sceneRef, loadedRefByPath, ConflictType.ID);
                
                // Do they match types?
                if (!DoTypesMatch(sceneRef, loadedRefByPath))
                    RecordConflict(sceneRef, loadedRefByPath, ConflictType.TypeMismatch);
            }
        }

        // Loaded that match neither an ID nor a path
        foreach (var loadedRef in loadedRefs.Values.OrderBy(t => t.ID))
        {
            if (sceneRefs.ContainsKey(loadedRef.ID) || scenePaths.ContainsKey(loadedRef.gameObjectPath))                
                continue;
            
            if (loadedRef.gameObject == null)
                RecordConflict(null, loadedRef, ConflictType.NotFound);
            else
                RecordConflict(null, loadedRef, ConflictType.NewID);
        }
        
        bool DoTypesMatch(NetworkObjectRef scene, NetworkObjectRef loaded)
            => scene.typeNames.Count == 0 // Won't exist for old scenes
                || scene.typeNames.SequenceEqual(loaded.typeNames);
        
        IEnumerable<Conflict> FindConflicts(NetworkObjectRef objRef, ConflictType type) // Them's fight'n words!
            => conflictList.Where(conflict => conflict.Type == type && conflict.IsMatch(objRef));

        void RecordConflict(NetworkObjectRef sceneRef, NetworkObjectRef loadedRef, ConflictType type)
        {
            IEnumerable<Conflict> sceneConflicts = 
                sceneRef != null && !string.IsNullOrWhiteSpace(sceneRef.gameObjectPath) ? FindConflicts(sceneRef, type) : System.Array.Empty<Conflict>();
            IEnumerable<Conflict> loadedConflicts = 
                loadedRef != null && !string.IsNullOrWhiteSpace(loadedRef.gameObjectPath) ? FindConflicts(loadedRef, type) : System.Array.Empty<Conflict>();
            IEnumerable<Conflict> matchingConflicts = sceneConflicts.Concat(loadedConflicts);

            if (matchingConflicts.FirstOrDefault() == null)
            {
                Conflict newConflict = new Conflict(type);
                conflictList.Add(newConflict);
                matchingConflicts = matchingConflicts.Append(newConflict);
            }

            foreach (Conflict conflict in matchingConflicts)            
            {
                conflict.AddLoaded(loadedRef);
                conflict.AddScene(sceneRef);
            }
        }
    }

    void DrawNoConflict(VRC.SDKBase.Network.NetworkIDPair netRef)
    {
        using (new EditorGUILayout.HorizontalScope(noConflictStyle))
        {
            EditorGUILayout.LabelField("Descriptor", GUILayout.Width(70));
            EditorGUILayout.LabelField(netRef.ID.ToString(), GUILayout.Width(70));
            
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField(netRef.gameObject, typeof(VRC.SDKBase.INetworkID), true);
            
            GUILayout.Space(63);
        }
    }

    void DrawConflict(Conflict conflict)
    {
        string selectName = ConflictResolutionNames[ConflictResolution.SelectOne];
        string ignoreName = ConflictResolutionNames[ConflictResolution.IgnoreOne];

        IEnumerable<(NetworkObjectRef objRef, Action<NetworkObjectRef> draw)> unordered = 
            conflict.SceneRefs.Select(objRef => (objRef, (Action<NetworkObjectRef>)DrawSceneRef))
                .Concat(conflict.LoadedRefs.Select(objRef => (objRef, (Action<NetworkObjectRef>)DrawLoadedRef)));

        using (new EditorGUILayout.VerticalScope(conflictStyle))
        {
            IEnumerable<(NetworkObjectRef objRef, Action<NetworkObjectRef> draw)> ordered;
            switch (conflict.Type)
            {
                case ConflictType.ID:
                    ordered = unordered.OrderBy(tuple => tuple.objRef.gameObjectPath);
                    break;
                case ConflictType.Object:
                    ordered = unordered.OrderBy(tuple => tuple.objRef.ID);
                    break;
                default:
                    ordered = unordered;
                    break;
            }

            foreach ((NetworkObjectRef objRef, Action<NetworkObjectRef> draw) in ordered.ToArray())
                draw(objRef);
        }

        void DrawSceneRef(NetworkObjectRef objRef)
        {
            if (conflict.Type == ConflictType.TypeMismatch)
            {
                using (new EditorGUILayout.HorizontalScope())   
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(objRef.gameObject, typeof(VRC.SDKBase.INetworkID), true);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Descriptor", GUILayout.Width(70));
                EditorGUILayout.LabelField(objRef.ID.ToString(), GUILayout.Width(70));

                switch (conflict.Type)
                {
                    case ConflictType.TypeMismatch:
                        using (new EditorGUI.DisabledScope(true))
                            EditorGUILayout.LabelField(string.Join(", ", objRef.typeNames.Select(t => t.Substring(t.LastIndexOf('.') + 1))));
                        break;
                    default:
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(objRef.gameObject, typeof(VRC.SDKBase.INetworkID), true);
                
                if (GUILayout.Button(selectName, GUILayout.Width(60)))
                    UseRef(objRef);
                        break;
                }
            }
        }

        void DrawLoadedRef(NetworkObjectRef objRef)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Loaded", GUILayout.Width(70));
                EditorGUILayout.LabelField(objRef.ID.ToString(), GUILayout.Width(70));

                if (objRef.gameObject != null)
                {
                    switch (conflict.Type)
                    {
                        case ConflictType.TypeMismatch:
                            using (new EditorGUI.DisabledScope(true))
                                EditorGUILayout.LabelField(string.Join(", ", objRef.typeNames.Select(t => t.Substring(t.LastIndexOf('.') + 1))));

                            if (GUILayout.Button(selectName, GUILayout.Width(60)))
                                UseRef(objRef);
                            break;
                        default:
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField(objRef.gameObject, typeof(VRC.SDKBase.INetworkID), true);

                    if (GUILayout.Button(selectName, GUILayout.Width(60)))
                        UseRef(objRef);
                            break;
                    }
                }
                else
                {
                    switch (conflict.Type)
                    {
                        case ConflictType.NotFound:
                            DrawObjectSelector();
                            if (GUILayout.Button(ignoreName, GUILayout.Width(60)))
                            {
                                RemoveFileRef(objRef);
                                RemoveObjRef(objRef);
                            }
                        break;
                        case ConflictType.NewID:
                            DrawObjectSelector();
                            if (GUILayout.Button(selectName, GUILayout.Width(60)))
                                UseRef(objRef);
                        break;
                        case ConflictType.TypeMismatch:
                        break;
                        default:
                            GUILayout.Space(60);
                            break;
                    }

                }
            }

            void DrawObjectSelector()
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.TextField(objRef.gameObjectPath);

                    GameObject newTarget = EditorGUILayout.ObjectField(null, typeof(GameObject), true) as GameObject;
                    if (newTarget?.GetComponent<INetworkID>() != null)
                    {    
                        // Remove existing, add new
                        int id = objRef.ID;
                        string path = Path(newTarget.gameObject);
                        if(!string.IsNullOrEmpty(path))
                        {
                            RemoveFileRef(objRef);
                            RemoveObjRef(objRef);

                            var newRef = new NetworkObjectRef
                            {
                                ID = id,
                                gameObjectPath = path,
                            gameObject = newTarget.gameObject,
                            typeNames = VRC.SDKBase.Network.NetworkIDAssignment.GetSerializedTypes(newTarget.gameObject)
                            };

                            fileRefs.Add(id, newRef);
                            DetectConflicts(new Dictionary<int, NetworkObjectRef> { { id, newRef } }, conflicts);
                        }
                    }
            }
        }
    }

    void UseRef(NetworkObjectRef objRef)
    {
        if (objRef == null || objRef.gameObject == null || string.IsNullOrWhiteSpace(objRef.gameObjectPath))
            throw new ArgumentException("Expected a valid and resolved reference");

        var ID = objRef.ID;
        var desired = objRef.gameObject;
        var path = objRef.gameObjectPath;

        networkTarget.NetworkIDCollection = networkTarget.NetworkIDCollection
            .Where(pair => pair.ID != ID && pair.gameObject != desired)
            .Append(new VRC.SDKBase.Network.NetworkIDPair {
                ID = ID,
                gameObject = desired,
                SerializedTypeNames = objRef.typeNames
            }).ToList();
        
        
        ((Component)networkTarget).gameObject.MarkDirty();
        PrefabUtility.RecordPrefabInstancePropertyModifications(((Component)networkTarget));
        
        RemoveFileRef(objRef);
        
        conflicts.Clear();
        
        DetectConflicts(fileRefs, conflicts);
    }

    void RemoveFileRef(NetworkObjectRef objRef)
    {
        fileRefs = fileRefs
            .Where(kvp => kvp.Key != objRef.ID && kvp.Value.gameObject != objRef.gameObject && kvp.Value.gameObjectPath != objRef.gameObjectPath)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    void RemoveObjRef(NetworkObjectRef objRef)
    {
        foreach (Conflict conflict in conflicts)
        {
            conflict.SceneRefs.Remove(objRef);
            conflict.LoadedRefs.Remove(objRef);
        }

        conflicts.RemoveAll(conflict => conflict.SceneRefs.Count == 0 && conflict.LoadedRefs.Count == 0);
    }

    string Path(GameObject gameObject)
    {
        return gameObject == null 
            ? null 
            : networkTarget.GetNetworkIDGameObjectPath(gameObject);
    }

    void SetTarget(INetworkIDContainer target)
    {
        networkTarget = target;
        conflicts.Clear();
        fileRefs.Clear();
    }
    void FindNetworkTargets()
    {
        //Find
        networkTargets.Clear();
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach(var obj in activeScene.GetRootGameObjects())
        {
            networkTargets.AddRange(obj.GetComponentsInChildren<INetworkIDContainer>(true));
        }

        //Names
        networkTargetNames = new string[networkTargets.Count];
        for(int i=0; i<networkTargets.Count; i++)
            networkTargetNames[i] = ((Component)networkTargets[i]).gameObject.name;
    }
}
#endif