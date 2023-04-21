using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VRC.Udon.Editor.ProgramSources;
using VRC.Udon.Editor.ProgramSources.Attributes;
using VRC.Udon.Editor.ProgramSources.UdonGraphProgram;

namespace VRC.Udon.Editor
{
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomUdonBehaviourInspectorAttribute : Attribute
    {
        internal readonly Type InspectedProgramAssetType;
        
        public CustomUdonBehaviourInspectorAttribute(Type inspectedProgramAssetType)
        {
            InspectedProgramAssetType = inspectedProgramAssetType;
        }
    }
    
    [CustomEditor(typeof(UdonBehaviour))]
    public class UdonBehaviourEditor : UnityEditor.Editor
    {
        private const string VRC_UDON_NEW_PROGRAM_TYPE_PREF_KEY = "VRC.Udon.NewProgramType";

        private SerializedProperty _programSourceProperty;
        private SerializedProperty _serializedProgramAssetProperty;
        private int _newProgramType = 1;

        private void OnEnable()
        {
            _programSourceProperty = serializedObject.FindProperty("programSource");
            _serializedProgramAssetProperty = serializedObject.FindProperty("serializedProgramAsset");
            _newProgramType = EditorPrefs.GetInt(VRC_UDON_NEW_PROGRAM_TYPE_PREF_KEY, 1);

            UdonEditorManager.Instance.WantRepaint += Repaint;
        }

        private void OnDisable()
        {
            UdonEditorManager.Instance.WantRepaint -= Repaint;
            UserInspectorManager.DestroyEditor(this);
        }

        private void OnDestroy()
        {
            UserInspectorManager.DestroyEditor(this);
        }

        public override void OnInspectorGUI()
        {
            if (UserInspectorManager.DoOnInspectorGUI(this))
                return;
            
            UdonBehaviour udonTarget = (UdonBehaviour)target;

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                bool dirty = false;

                EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox));
                {
                    // We skip the first option, Unknown, as it's reserved for older scenes.
                    VRC.SDKBase.Networking.SyncType method = (VRC.SDKBase.Networking.SyncType)(1 + EditorGUILayout.Popup("Synchronization", (int)udonTarget.SyncMethod - 1, Enum.GetNames(typeof(VRC.SDKBase.Networking.SyncType)).Skip(1).ToArray()));

                    if (method != udonTarget.SyncMethod)
                    {
                        udonTarget.SyncMethod = method;
                        dirty = true;
                    }

                    switch (method)
                    {
                        case VRC.SDKBase.Networking.SyncType.None:
                            EditorGUILayout.LabelField("Replication will be disabled.", EditorStyles.wordWrappedLabel);
                            break;
                        case VRC.SDKBase.Networking.SyncType.Continuous:
                            EditorGUILayout.LabelField("Continuous replication is intended for frequently-updated variables of small size, and will be tweened. Ideal for physics objects and objects that must be in sync with players.", EditorStyles.wordWrappedLabel);
                            break;
                        case VRC.SDKBase.Networking.SyncType.Manual:
                            EditorGUILayout.LabelField("Manual replication is intended for infrequently-updated variables of small or large size, and will not be tweened. Ideal for infrequently modified abstract data.", EditorStyles.wordWrappedLabel);
                            break;
                        default:
                            EditorGUILayout.LabelField("What have you done?!", EditorStyles.wordWrappedLabel);
                            break;
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Udon");

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                _programSourceProperty.objectReferenceValue = EditorGUILayout.ObjectField(
                    "Program Source",
                    _programSourceProperty.objectReferenceValue,
                    typeof(AbstractUdonProgramSource),
                    false
                );

                if (EditorGUI.EndChangeCheck())
                {
                    if (_programSourceProperty.objectReferenceValue == null)
                    {
                        _serializedProgramAssetProperty.objectReferenceValue = null;
                    }

                    dirty = true;
                    serializedObject.ApplyModifiedProperties();
                }

                if (_programSourceProperty.objectReferenceValue == null)
                {
                    List<(string displayName, Type newProgramType)> programSourceTypesForNewMenu = GetProgramSourceTypesForNewMenu();
                    if (GUILayout.Button("New Program"))
                    {
                        (string displayName, Type newProgramType) = programSourceTypesForNewMenu.ElementAt(_newProgramType);

                        string udonBehaviourName = udonTarget.name;
                        Scene scene = udonTarget.gameObject.scene;
                        if (string.IsNullOrEmpty(scene.path))
                        {
                            Debug.LogError("You need to save the scene before you can create new Udon program assets!");
                        }
                        else
                        {
                            AbstractUdonProgramSource newProgramSource = CreateUdonProgramSourceAsset(newProgramType, displayName, scene, udonBehaviourName);
                            _programSourceProperty.objectReferenceValue = newProgramSource;
                            _serializedProgramAssetProperty.objectReferenceValue = newProgramSource.SerializedProgramAsset;
                            serializedObject.ApplyModifiedProperties();
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    EditorGUI.BeginChangeCheck();
                    _newProgramType = EditorGUILayout.Popup(
                        "",
                        Mathf.Clamp(_newProgramType, 0, programSourceTypesForNewMenu.Count),
                        programSourceTypesForNewMenu.Select(t => t.displayName).ToArray(),
                        GUILayout.ExpandWidth(false)
                    );

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetInt(VRC_UDON_NEW_PROGRAM_TYPE_PREF_KEY, _newProgramType);
                    }
                }
                else
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.ObjectField(
                            "Serialized Udon Program Asset ID: ",
                            _serializedProgramAssetProperty.objectReferenceValue,
                            typeof(AbstractSerializedUdonProgramAsset),
                            false
                        );

                        EditorGUI.indentLevel--;
                    }

                    AbstractUdonProgramSource programSource = (AbstractUdonProgramSource)_programSourceProperty.objectReferenceValue;
                    AbstractSerializedUdonProgramAsset serializedUdonProgramAsset = programSource.SerializedProgramAsset;
                    if (_serializedProgramAssetProperty.objectReferenceValue != serializedUdonProgramAsset)
                    {
                        _serializedProgramAssetProperty.objectReferenceValue = serializedUdonProgramAsset;
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                EditorGUILayout.EndHorizontal();

                udonTarget.RunEditorUpdate(ref dirty);
                if (dirty && !Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(udonTarget.gameObject.scene);
                }
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (UserInspectorManager.DoCreateInspectorGUI(this, out var element))
                return element;

            return null;
        }

        private void OnSceneGUI()
        {
            UserInspectorManager.DoOnSceneGUI(this);
        }

        public override bool RequiresConstantRepaint()
        {
            if (UserInspectorManager.DoRequiresConstantRepaint(this, out bool needsRepaint))
                return needsRepaint;
            
            return false;
        }

        public override bool UseDefaultMargins()
        {
            if (UserInspectorManager.DoUseDefaultMargins(this, out bool useDefaultMargins))
                return useDefaultMargins;

            return true;
        }

        private static AbstractUdonProgramSource CreateUdonProgramSourceAsset(Type newProgramType, string displayName, Scene scene, string udonBehaviourName)
        {
            string scenePath = Path.GetDirectoryName(scene.path) ?? "Assets";

            string folderName = $"{scene.name}_UdonProgramSources";
            string folderPath = Path.Combine(scenePath, folderName);

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(scenePath, folderName);
            }

            string assetPath = Path.Combine(folderPath, $"{udonBehaviourName} {displayName}.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AbstractUdonProgramSource asset = (AbstractUdonProgramSource)CreateInstance(newProgramType);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        private static List<(string displayName, Type newProgramType)> GetProgramSourceTypesForNewMenu()
        {
            Type abstractProgramSourceType = typeof(AbstractUdonProgramSource);
            Type attributeNewMenuAttributeType = typeof(UdonProgramSourceNewMenuAttribute);

            List<(string displayName, Type newProgramType)> programSourceTypesForNewMenu = new List<(string displayName, Type newProgramType)>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                UdonProgramSourceNewMenuAttribute[] udonProgramSourceNewMenuAttributes;
                try
                {
                    udonProgramSourceNewMenuAttributes = (UdonProgramSourceNewMenuAttribute[])assembly.GetCustomAttributes(attributeNewMenuAttributeType, false);
                }
                catch
                {
                    udonProgramSourceNewMenuAttributes = Array.Empty<UdonProgramSourceNewMenuAttribute>();
                }

                foreach (UdonProgramSourceNewMenuAttribute udonProgramSourceNewMenuAttribute in udonProgramSourceNewMenuAttributes)
                {
                    if (udonProgramSourceNewMenuAttribute == null)
                    {
                        continue;
                    }

                    if (!abstractProgramSourceType.IsAssignableFrom(udonProgramSourceNewMenuAttribute.Type))
                    {
                        continue;
                    }

                    programSourceTypesForNewMenu.Add((udonProgramSourceNewMenuAttribute.DisplayName, udonProgramSourceNewMenuAttribute.Type));
                }
            }

            programSourceTypesForNewMenu.Sort(
                (left, right) => string.Compare(
                    left.displayName,
                    right.displayName,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            return programSourceTypesForNewMenu;
        }
    }

    [InitializeOnLoad]
    internal static class UserInspectorManager
    {
        private static Dictionary<Type, Type> _programAssetTypeToInspectorTypeMap = new Dictionary<Type, Type>();
        private static Dictionary<UnityEditor.Editor, UnityEditor.Editor> _behaviourEditors = new Dictionary<UnityEditor.Editor, UnityEditor.Editor>();

        static UserInspectorManager()
        {
            InitInspectorMap();
        }

        private static readonly HashSet<Type> _blacklistedInspectorTypes = new HashSet<Type>()
        {
            typeof(AbstractUdonProgramSource),
            typeof(UdonProgramAsset),
            typeof(UdonAssemblyProgramAsset),
        };

        private static void InitInspectorMap()
        {
            var inspectorTypes = TypeCache.GetTypesWithAttribute<CustomUdonBehaviourInspectorAttribute>();

            foreach (Type inspectorType in inspectorTypes)
            {
                if (!inspectorType.IsSubclassOf(typeof(UnityEditor.Editor)))
                {
                    Debug.LogError($"'{inspectorType}' does not inherit from UnityEditor.Editor, but has a CustomUdonBehaviourInspector attribute");
                    continue;
                }
                
                var customInspectorAttribute = inspectorType.GetCustomAttribute<CustomUdonBehaviourInspectorAttribute>();

                Type inspectedType = customInspectorAttribute.InspectedProgramAssetType;

                if (inspectedType == null)
                {
                    Debug.LogError($"Inspected program asset type for '{inspectorType}' is null");
                    continue;
                }

                if (!inspectedType.IsSubclassOf(typeof(AbstractUdonProgramSource)))
                {
                    Debug.LogError("Inspected type must be a subclass of AbstractUdonProgramSource");
                    continue;
                }

                if (_blacklistedInspectorTypes.Contains(inspectedType))
                {
                    Debug.LogError($"Cannot provide a custom inspector for built-in Udon program asset type '{inspectedType}'");
                    continue;
                }

                if (_programAssetTypeToInspectorTypeMap.ContainsKey(inspectedType))
                {
                    Debug.LogError("Cannot have multiple UdonBehaviour inspectors assigned to the same Udon program asset type");
                    continue;
                }
                
                _programAssetTypeToInspectorTypeMap.Add(inspectedType, inspectorType);
            }
        }

        public static bool DoOnInspectorGUI(UnityEditor.Editor udonBehaviourEditor)
        {
            var editor = GetCustomEditor(udonBehaviourEditor);

            if (editor == null)
                return false;
            
            editor.OnInspectorGUI();

            return true;
        }

        public static bool DoCreateInspectorGUI(UnityEditor.Editor udonBehaviourEditor, out VisualElement element)
        {
            element = null;
            
            var editor = GetCustomEditor(udonBehaviourEditor);

            if (editor == null)
                return false;

            MethodInfo createInspectorGuiMethod = editor.GetType().GetMethod("CreateInspectorGUI", BindingFlags.Public | BindingFlags.Instance);

            if (createInspectorGuiMethod == null)
                return false;

            element = (VisualElement)createInspectorGuiMethod.Invoke(editor, Array.Empty<object>());

            return element != null;
        }

        public static bool DoOnSceneGUI(UnityEditor.Editor udonBehaviourEditor)
        {
            var editor = GetCustomEditor(udonBehaviourEditor);
            
            if (editor == null)
                return false;
            
            MethodInfo onSceneGuiMethod = editor.GetType().GetMethod("OnSceneGUI", BindingFlags.Public | BindingFlags.Instance);

            if (onSceneGuiMethod == null)
                return false;

            onSceneGuiMethod.Invoke(editor, Array.Empty<object>());
            
            return true;
        }

        public static bool DoRequiresConstantRepaint(UnityEditor.Editor udonBehaviourEditor, out bool needsRepaint)
        {
            needsRepaint = false;
            
            var editor = GetCustomEditor(udonBehaviourEditor);
            
            if (editor == null)
                return false;
            
            MethodInfo needsConstantRepaintMethod = editor.GetType().GetMethod("NeedsConstantRepaint", BindingFlags.Public | BindingFlags.Instance);

            if (needsConstantRepaintMethod == null)
                return false;
            
            needsRepaint = (bool)needsConstantRepaintMethod.Invoke(editor, Array.Empty<object>());

            return true;
        }
        
        public static bool DoUseDefaultMargins(UnityEditor.Editor udonBehaviourEditor, out bool useDefaultMargins)
        {
            useDefaultMargins = true;
            
            var editor = GetCustomEditor(udonBehaviourEditor);
            
            if (editor == null)
                return false;
            
            MethodInfo useDefaultMarginsMethod = editor.GetType().GetMethod("UseDefaultMargins", BindingFlags.Public | BindingFlags.Instance);

            if (useDefaultMarginsMethod == null)
                return false;
            
            useDefaultMargins = (bool)useDefaultMarginsMethod.Invoke(editor, Array.Empty<object>());

            return true;
        }

        private static UnityEditor.Editor GetCustomEditor(UnityEditor.Editor udonBehaviourEditor)
        {
            if (udonBehaviourEditor == null)
                return null;

            UdonBehaviour targetBehaviour = udonBehaviourEditor.target as UdonBehaviour;
            
            if (targetBehaviour == null || targetBehaviour.programSource == null)
                return null;
            
            if (!_programAssetTypeToInspectorTypeMap.TryGetValue(targetBehaviour.programSource.GetType(), out var editorType))
                return null;

            if (_behaviourEditors.TryGetValue(udonBehaviourEditor, out var foundEditor))
            {
                if (foundEditor.GetType() == editorType)
                    return foundEditor;
                
                // Program asset type has changed so we need to check for a new editor type
                DestroyEditor(udonBehaviourEditor);
            }

            var userEditor = UnityEditor.Editor.CreateEditor(targetBehaviour, editorType);
            
            _behaviourEditors.Add(udonBehaviourEditor, userEditor);

            return userEditor;
        }

        public static void DestroyEditor(UnityEditor.Editor parentEditor)
        {
            if (!_behaviourEditors.TryGetValue(parentEditor, out var foundEditor)) 
                return;

            _behaviourEditors.Remove(parentEditor);
            
            if (foundEditor != null)
                UnityEngine.Object.DestroyImmediate(foundEditor);
        }
    }
}
