using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using VRC.SDK3.Components;
using VRC.SDK3.Image;
using VRC.SDKBase.Editor.Source.Helpers;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.ProgramSources;

namespace VRC.Udon.Editor.ProgramSources
{
    public class UdonProgramAsset : AbstractUdonProgramSource, ISerializationCallbackReceiver
    {
        protected IUdonProgram program;

        [SerializeField]
        protected AbstractSerializedUdonProgramAsset serializedUdonProgramAsset;

        public override AbstractSerializedUdonProgramAsset SerializedProgramAsset
        {
            get
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out string guid, out long _);
                if(serializedUdonProgramAsset != null)
                {
                    if(serializedUdonProgramAsset.name == guid)
                    {
                        return serializedUdonProgramAsset;
                    }

                    string oldSerializedUdonProgramAssetPath = Path.Combine("Assets", "SerializedUdonPrograms", $"{serializedUdonProgramAsset.name}.asset");
                    AssetDatabase.DeleteAsset(oldSerializedUdonProgramAssetPath);
                }

                string serializedUdonProgramAssetPath = Path.Combine("Assets", "SerializedUdonPrograms", $"{guid}.asset");

                serializedUdonProgramAsset = (SerializedUdonProgramAsset)AssetDatabase.LoadAssetAtPath(
                    Path.Combine("Assets", "SerializedUdonPrograms", $"{guid}.asset"),
                    typeof(SerializedUdonProgramAsset)
                );

                if(serializedUdonProgramAsset != null)
                {
                    return serializedUdonProgramAsset;
                }

                serializedUdonProgramAsset = CreateInstance<SerializedUdonProgramAsset>();
                if(!AssetDatabase.IsValidFolder(Path.Combine("Assets", "SerializedUdonPrograms")))
                {
                    AssetDatabase.CreateFolder("Assets", "SerializedUdonPrograms");
                }

                AssetDatabase.CreateAsset(serializedUdonProgramAsset, serializedUdonProgramAssetPath);
                AssetDatabase.SaveAssets();

                RefreshProgram();
                AssetDatabase.SaveAssets();

                AssetDatabase.Refresh();

                return serializedUdonProgramAsset;
            }
        }

        private bool _lastAssembleFailed = false;

        public sealed override void RunEditorUpdate(UdonBehaviour udonBehaviour, ref bool dirty)
        {
            if(program == null && serializedUdonProgramAsset != null)
            {
                program = serializedUdonProgramAsset.RetrieveProgram();
            }

            if(program == null && !_lastAssembleFailed)
            {
                RefreshProgram();
            }

            DrawProgramSourceGUI(udonBehaviour, ref dirty);

            if(dirty)
            {
                EditorUtility.SetDirty(this);
            }
        }

        protected virtual void DrawProgramSourceGUI(UdonBehaviour udonBehaviour, ref bool dirty)
        {
            DrawPublicVariables(udonBehaviour, ref dirty);
            DrawProgramDisassembly();
        }

        public sealed override void RefreshProgram()
        {
            if(Application.isPlaying)
            {
                return;
            }

            RefreshProgramImpl();

            _lastAssembleFailed = program == null;
            
            if (_lastAssembleFailed)
            {
                return;
            }

            SerializedProgramAsset.StoreProgram(program);
            if (this != null)
            {
                EditorUtility.SetDirty(this);
            }
        }

        protected virtual void RefreshProgramImpl()
        {
        }

        [PublicAPI]
        protected void DrawInteractionArea(UdonBehaviour udonBehaviour)
        {
            if (program == null) return;
            ImmutableArray<string> exportedSymbols = program.EntryPoints.GetExportedSymbols();
            if (exportedSymbols.Contains("_interact"))
            {
                EditorGUILayout.LabelField("Interaction", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                if(udonBehaviour != null)
                {
                    udonBehaviour.interactText = EditorGUILayout.TextField("Interaction Text", udonBehaviour.interactText);
                    udonBehaviour.proximity = EditorGUILayout.Slider("Proximity", udonBehaviour.proximity, 0f, 100f);
                    udonBehaviour.interactTextPlacement = (Transform)EditorGUILayout.ObjectField("Text Placement", udonBehaviour.interactTextPlacement, typeof(Transform), true);
                }
                else
                {
                    using(new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.TextField("Interaction Text", "Use");
                        EditorGUILayout.Slider("Proximity", 2.0f, 0f, 100f);
                        EditorGUILayout.ObjectField("Text Placement", null, typeof(Transform), true);
                    }
                }
                
                
                
                EditorGUI.indentLevel--;
            }
        }

        [PublicAPI]
        protected void DrawPublicVariables(UdonBehaviour udonBehaviour, ref bool dirty)
        {
            IUdonVariableTable publicVariables = null;
            if(udonBehaviour != null)
            {
                publicVariables = udonBehaviour.publicVariables;
            }

            EditorGUILayout.LabelField("Public Variables", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if(program?.SymbolTable == null)
            {
                EditorGUILayout.LabelField("No public variables.");
                EditorGUI.indentLevel--;
                return;
            }

            IUdonSymbolTable symbolTable = program.SymbolTable;
            // Remove non-exported public variables
            if(publicVariables != null)
            {
                foreach(string publicVariableSymbol in publicVariables.VariableSymbols.ToArray())
                {
                    if(!symbolTable.HasExportedSymbol(publicVariableSymbol))
                    {
                        publicVariables.RemoveVariable(publicVariableSymbol);
                    }
                }
            }

            ImmutableArray<string> exportedSymbolNames = symbolTable.GetExportedSymbols();
            if(exportedSymbolNames.Length <= 0)
            {
                EditorGUILayout.LabelField("No public variables.");
                EditorGUI.indentLevel--;
                return;
            }

            foreach(string exportedSymbol in exportedSymbolNames)
            {
                Type symbolType = symbolTable.GetSymbolType(exportedSymbol);

                if(publicVariables == null)
                {
                    DrawPublicVariableField(exportedSymbol, GetPublicVariableDefaultValue(exportedSymbol, symbolType), symbolType, ref dirty, false);
                    continue;
                }

                if(!publicVariables.TryGetVariableType(exportedSymbol, out Type declaredType) || declaredType != symbolType)
                {
                    publicVariables.RemoveVariable(exportedSymbol);
                    if(!publicVariables.TryAddVariable(CreateUdonVariable(exportedSymbol, GetPublicVariableDefaultValue(exportedSymbol, declaredType), symbolType)))
                    {
                        EditorGUILayout.LabelField($"Error drawing field for symbol '{exportedSymbol}'.");
                        continue;
                    }
                }

                if(!publicVariables.TryGetVariableValue(exportedSymbol, out object variableValue))
                {
                    variableValue = GetPublicVariableDefaultValue(exportedSymbol, declaredType);
                }

                variableValue = DrawPublicVariableField(exportedSymbol, variableValue, symbolType, ref dirty, true);
                
                if (variableValue == null && symbolType.Equals(typeof(VRCUrlInputField)))
                {
                    var foundPlayer = udonBehaviour.GetComponentInChildren<VRCUrlInputField>(true);
                    if (foundPlayer != null)
                    {
                        variableValue = foundPlayer;
                        dirty = true;
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Your InputField isn't set - Try reloading the SDK if this is unexpected.", MessageType.Warning);
                        if (GUILayout.Button("Reload SDK"))
                        {
                            ReloadUtil.ReloadSDK(false);
                            ReloadUtil.ReloadCurrentScene();
                        }
                    }
                }
                
                if(!dirty)
                {
                    continue;
                }

                Undo.RecordObject(udonBehaviour, "Modify Public Variable");

                if(!publicVariables.TrySetVariableValue(exportedSymbol, variableValue))
                {
                    if(!publicVariables.TryAddVariable(CreateUdonVariable(exportedSymbol, variableValue, symbolType)))
                    {
                        Debug.LogError($"Failed to set public variable '{exportedSymbol}' value.");
                    }
                }

                EditorSceneManager.MarkSceneDirty(udonBehaviour.gameObject.scene);

                if(PrefabUtility.IsPartOfPrefabInstance(udonBehaviour))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(udonBehaviour);
                }
            }

            EditorGUI.indentLevel--;
        }

        private static IUdonVariable CreateUdonVariable(string symbolName, object value, Type declaredType)
        {
            Type udonVariableType = typeof(UdonVariable<>).MakeGenericType(declaredType);
            return (IUdonVariable)Activator.CreateInstance(udonVariableType, symbolName, value);
        }

        [PublicAPI]
        protected virtual object GetPublicVariableDefaultValue(string symbol, Type type)
        {
            return null;
        }

        [PublicAPI]
        protected void DrawProgramDisassembly()
        {
            try
            {
                EditorGUILayout.LabelField("Disassembled Program", EditorStyles.boldLabel);
                using(new EditorGUI.DisabledScope(true))
                {
                    string[] disassembledProgram = UdonEditorManager.Instance.DisassembleProgram(program);
                    EditorGUILayout.TextArea(string.Join("\n", disassembledProgram));
                }
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        [NonSerialized]
        private readonly Dictionary<string, bool> _arrayStates = new Dictionary<string, bool>();

        protected virtual object DrawPublicVariableField(string symbol, object variableValue, Type variableType, ref bool dirty, bool enabled)
        {
            using(new EditorGUI.DisabledScope(!enabled))
            {
                // ReSharper disable RedundantNameQualifier
                if(!variableType.IsInstanceOfType(variableValue))
                {
                    if(variableType.IsValueType)
                    {
                        variableValue = Activator.CreateInstance(variableType);
                    }
                    else
                    {
                        variableValue = null;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                if(typeof(UnityEngine.Object).IsAssignableFrom(variableType))
                {
                    UnityEngine.Object unityEngineObjectValue = (UnityEngine.Object)variableValue;
                    EditorGUI.BeginChangeCheck();
                    Rect fieldRect = EditorGUILayout.GetControlRect();
                    variableValue = EditorGUI.ObjectField(fieldRect, symbol, unityEngineObjectValue, variableType, true);

                    if(variableValue == null && (variableType == typeof(GameObject) || variableType == typeof(Transform) ||
                                                 variableType == typeof(UdonBehaviour)))
                    {
                        EditorGUI.LabelField(
                            fieldRect,
                            new GUIContent(symbol),
                            new GUIContent("Self (" + variableType.Name + ")", AssetPreview.GetMiniTypeThumbnail(variableType)),
                            EditorStyles.objectField);
                    }

                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(string))
                {
                    string stringValue = (string)variableValue;
                    EditorGUI.BeginChangeCheck();
                    variableValue = EditorGUILayout.TextField(symbol, stringValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(string[]))
                {
                    string[] valueArray = (string[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.TextField(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : "");
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(float))
                {
                    float floatValue = (float?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = EditorGUILayout.FloatField(symbol, floatValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(float[]))
                {
                    float[] valueArray = (float[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.FloatField(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : 0);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(int))
                {
                    int intValue = (int?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = EditorGUILayout.IntField(symbol, intValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(int[]))
                {
                    int[] valueArray = (int[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.IntField(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : 0);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(short))
                {
                    short intValue = (short?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = (short)EditorGUILayout.IntField(symbol, intValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(short[]))
                {
                    short[] valueArray = (short[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = (short)EditorGUILayout.IntField(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : 0);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(long))
                {
                    long intValue = (long?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = (long)EditorGUILayout.IntField(symbol, (int)intValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(long[]))
                {
                    long[] valueArray = (long[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.IntField(
                                    $"{i}:",
                                    valueArray.Length > i ? (int)valueArray[i] : 0);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(uint))
                {
                    uint intValue = (uint?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = (uint)EditorGUILayout.IntField(symbol, (int)intValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(uint[]))
                {
                    uint[] valueArray = (uint[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = (uint)EditorGUILayout.IntField(
                                    $"{i}:",
                                    valueArray.Length > i ? (int)valueArray[i] : 0);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(ushort))
                {
                    ushort intValue = (ushort?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = (ushort)EditorGUILayout.IntField(symbol, (int)intValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(ushort[]))
                {
                    ushort[] valueArray = (ushort[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = (ushort)EditorGUILayout.IntField(
                                    $"{i}:",
                                    valueArray.Length > i ? (int)valueArray[i] : 0);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(ulong))
                {
                    ulong intValue = (ulong?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = (ulong)EditorGUILayout.IntField(symbol, (int)intValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(ulong[]))
                {
                    ulong[] valueArray = (ulong[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = (ulong)EditorGUILayout.IntField(
                                    $"{i}:",
                                    valueArray.Length > i ? (int)valueArray[i] : 0);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(byte))
                {
                    byte intValue = (byte?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = (byte)EditorGUILayout.IntField(symbol, (int)intValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(byte[]))
                {
                    byte[] valueArray = (byte[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = (byte)EditorGUILayout.IntField(
                                    $"{i}:",
                                    valueArray.Length > i ? (int)valueArray[i] : 0);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(sbyte))
                {
                    sbyte intValue = (sbyte?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = (sbyte)EditorGUILayout.IntField(symbol, (int)intValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(sbyte[]))
                {
                    sbyte[] valueArray = (sbyte[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = (sbyte)EditorGUILayout.IntField(
                                    $"{i}:",
                                    valueArray.Length > i ? (int)valueArray[i] : 0);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(double))
                {
                    double intValue = (double?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = (double)EditorGUILayout.DoubleField(symbol, intValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(double[]))
                {
                    double[] valueArray = (double[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.DoubleField(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : 0);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(decimal))
                {
                    decimal intValue = (decimal?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = (decimal)EditorGUILayout.DoubleField(symbol, (double)intValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(decimal[]))
                {
                    decimal[] valueArray = (decimal[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = (decimal)EditorGUILayout.DoubleField(
                                    $"{i}:",
                                    valueArray.Length > i ? (double)valueArray[i] : 0);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(bool))
                {
                    bool boolValue = (bool?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = EditorGUILayout.Toggle(symbol, boolValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(bool[]))
                {
                   bool[] valueArray = (bool[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.Toggle(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : false);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(UnityEngine.Vector2))
                {
                    Vector2 vector2Value = (Vector2?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = EditorGUILayout.Vector2Field(symbol, vector2Value);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(Vector2[]))
                {
                    Vector2[] valueArray = (Vector2[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.Vector2Field(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : Vector2.zero);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(UnityEngine.Vector3))
                {
                    Vector3 vector3Value = (Vector3?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = EditorGUILayout.Vector3Field(symbol, vector3Value);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(Vector3[]))
                {
                    Vector3[] valueArray = (Vector3[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.Vector3Field(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : Vector3.zero);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(UnityEngine.Vector2Int))
                {
                    Vector2Int vector2IntValue = (Vector2Int?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    Vector2 vector2Value = EditorGUILayout.Vector2Field(symbol, vector2IntValue);
                    variableValue = new Vector2Int((int)vector2Value.x, (int)vector2Value.y);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(Vector2Int[]))
                {
                    Vector2Int[] valueArray = (Vector2Int[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                Vector2 vector2Value = EditorGUILayout.Vector2Field(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : Vector2.zero);
                                valueArray[i] = new Vector2Int((int)vector2Value.x, (int)vector2Value.y);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(UnityEngine.Vector3Int))
                {
                    Vector3Int vector3IntValue = (Vector3Int?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    Vector3 vector3Value = EditorGUILayout.Vector3Field(symbol, vector3IntValue);
                    variableValue = new Vector3Int((int)vector3Value.x, (int)vector3Value.y, (int)vector3Value.z);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(Vector3Int[]))
                {
                    Vector3Int[] valueArray = (Vector3Int[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                Vector3 vector3Value = EditorGUILayout.Vector3Field(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : Vector3.zero);
                                valueArray[i] = new Vector3Int((int)vector3Value.x, (int)vector3Value.y,
                                    (int)vector3Value.z);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(UnityEngine.Vector4))
                {
                    Vector4 vector4Value = (Vector4?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = EditorGUILayout.Vector4Field(symbol, vector4Value);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(Vector4[]))
                {
                    Vector4[] valueArray = (Vector4[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.Vector4Field(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : Vector4.zero);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(UnityEngine.Quaternion))
                {
                    Quaternion quaternionValue = (Quaternion?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    Vector4 quaternionVector4 = EditorGUILayout.Vector4Field(symbol, new Vector4(quaternionValue.x, quaternionValue.y, quaternionValue.z, quaternionValue.w));
                    variableValue = new Quaternion(quaternionVector4.x, quaternionVector4.y, quaternionVector4.z, quaternionVector4.w);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(Quaternion[]))
                {
                    Quaternion[] valueArray = (Quaternion[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                Vector4 vector4 = EditorGUILayout.Vector4Field(
                                    $"{i}:",
                                    valueArray.Length > i ? new Vector4(valueArray[i].x, valueArray[i].y, valueArray[i].z, valueArray[i].w) : Vector4.zero);

                                valueArray[i] = new Quaternion(vector4.x, vector4.y, vector4.z, vector4.w);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(Gradient))
                {
                    Gradient color2Value = variableValue as Gradient;
                    if (color2Value == null) color2Value = new Gradient();
                    EditorGUI.BeginChangeCheck();
                    variableValue = EditorGUILayout.GradientField(symbol, color2Value);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(Gradient[]))
                {
                    Gradient[] valueArray = (Gradient[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                Gradient g = valueArray.Length > i ? (valueArray[i]) : new Gradient();
                                if (g == null) g = new Gradient();
                                valueArray[i] = EditorGUILayout.GradientField($"{i}:", g);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(AnimationCurve))
                {
                    AnimationCurve curve2Value = variableValue as AnimationCurve;
                    if (curve2Value == null) curve2Value = new AnimationCurve();
                    EditorGUI.BeginChangeCheck();
                    variableValue = EditorGUILayout.CurveField(symbol, curve2Value);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(AnimationCurve[]))
                {
                    AnimationCurve[] valueArray = (AnimationCurve[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                AnimationCurve curve = valueArray.Length > i ? (valueArray[i]) : new AnimationCurve();
                                if (curve == null) curve = new AnimationCurve();
                                valueArray[i] = EditorGUILayout.CurveField($"{i}:", curve);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(UnityEngine.Color))
                {
                    Color color2Value = (Color?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = EditorGUILayout.ColorField(symbol, color2Value);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(Color[]))
                {
                    Color[] valueArray = (Color[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.ColorField(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : Color.white);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(UnityEngine.Color32))
                {
                    Color32 colorValue = (Color32?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    variableValue = (Color32)EditorGUILayout.ColorField(symbol, colorValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(Color32[]))
                {
                    Color32[] valueArray = (Color32[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    EditorGUILayout.BeginVertical();

                    EditorGUI.BeginChangeCheck();
                    // Show Foldout Header
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    // Save foldout state
                    _arrayStates[symbol] = showArray;
                    
                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.ColorField(
                                    $"{i}:",
                                    valueArray.Length > i ? valueArray[i] : (Color32)Color.white);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(ParticleSystem.MinMaxCurve))
                {
                    ParticleSystem.MinMaxCurve minMaxCurve = (ParticleSystem.MinMaxCurve?)variableValue ?? default;
                    EditorGUI.BeginChangeCheck();
                    float multiplier = minMaxCurve.curveMultiplier;
                    AnimationCurve minCurve = minMaxCurve.curveMin;
                    AnimationCurve maxCurve = minMaxCurve.curveMax;
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(symbol);
                    EditorGUI.indentLevel++;
                    multiplier = EditorGUILayout.FloatField("Multiplier", multiplier);
                    minCurve = EditorGUILayout.CurveField("Min Curve", minCurve);
                    maxCurve = EditorGUILayout.CurveField("Max Curve", maxCurve);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                    variableValue = new ParticleSystem.MinMaxCurve(multiplier, minCurve, maxCurve);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if(variableType == typeof(ParticleSystem.MinMaxCurve[]))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginVertical();
                    ParticleSystem.MinMaxCurve[] valueArray = (ParticleSystem.MinMaxCurve[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }
                    
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    _arrayStates[symbol] = showArray;

                    if(showArray)
                    {
                        EditorGUI.indentLevel++;

                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                ParticleSystem.MinMaxCurve minMaxCurve = (ParticleSystem.MinMaxCurve)valueArray[i];
                                float multiplier = minMaxCurve.curveMultiplier;
                                AnimationCurve minCurve = minMaxCurve.curveMin;
                                AnimationCurve maxCurve = minMaxCurve.curveMax;
                                EditorGUILayout.BeginVertical();
                                EditorGUI.indentLevel++;
                                multiplier = EditorGUILayout.FloatField("Multiplier", multiplier);
                                minCurve = EditorGUILayout.CurveField("Min Curve", minCurve);
                                maxCurve = EditorGUILayout.CurveField("Max Curve", maxCurve);
                                EditorGUI.indentLevel--;
                                EditorGUILayout.EndVertical();
                                valueArray[i] = new ParticleSystem.MinMaxCurve(multiplier, minCurve, maxCurve);
                                EditorGUILayout.Space();
                            }
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType.IsEnum)
                {
                    Enum enumValue = (Enum)variableValue;
                    GUI.SetNextControlName("NodeField");
                    EditorGUI.BeginChangeCheck();
                    variableValue = EditorGUILayout.EnumPopup(symbol, enumValue);
                    if(EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                // ReSharper disable once PossibleNullReferenceException
                else if(variableType.IsArray && variableType.GetElementType().IsEnum)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.LabelField(symbol);
                    EditorGUILayout.BeginVertical();
                    Enum[] valueArray = (Enum[])variableValue;
                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if(_arrayStates.ContainsKey(symbol))
                    {
                        showArray = _arrayStates[symbol];
                    }
                    else
                    {
                        _arrayStates.Add(symbol, false);
                    }

                    showArray = EditorGUILayout.Foldout(showArray, symbol);
                    _arrayStates[symbol] = showArray;

                    if(showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1
                        );
                        EditorGUILayout.Space();
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);
                        
                        if(valueArray != null && valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.EnumPopup(
                                    $"{i}:",
                                    valueArray[i]);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if(variableType == typeof(Type))
                {
                    Type typeValue = (Type)variableValue;
                    EditorGUILayout.LabelField(symbol, typeValue == null ? $"Type = null" : $"Type = {typeValue.Name}");
                }

                else if(variableType.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(variableType.GetElementType()))
                {
                    Type elementType = variableType.GetElementType();
                    Assert.IsNotNull(elementType);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginVertical();
                    GUI.SetNextControlName("NodeField");
                    
                    bool showArray = false;
                    if (_arrayStates.ContainsKey(symbol))
                        showArray = _arrayStates[symbol];
                    else
                        _arrayStates.Add(symbol, false);
                    showArray = EditorGUILayout.Foldout( showArray, symbol, true );
                    _arrayStates[symbol] = showArray;
                    
                    if(variableValue == null)
                    {
                        variableValue = Array.CreateInstance(elementType, 0);
                    }

                    UnityEngine.Object[] valueArray = (UnityEngine.Object[])variableValue;

                    if (showArray)
                    {
                        EditorGUI.indentLevel++;
                        
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray.Length > 0 ? valueArray.Length : 1);

                        Array.Resize(ref valueArray, newSize);
                        Assert.IsNotNull(valueArray);

                        if(valueArray.Length > 0)
                        {
                            for(int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                valueArray[i] = EditorGUILayout.ObjectField($"{i}:", valueArray.Length > i ? valueArray[i] : null, variableType.GetElementType(), true);
                            }
                        }
                        
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    if(EditorGUI.EndChangeCheck())
                    {
                        Array destinationArray = Array.CreateInstance(elementType, valueArray.Length);
                        Array.Copy(valueArray, destinationArray, valueArray.Length);

                        variableValue = destinationArray;

                        dirty = true;
                    }
                }
                else if (variableType == typeof(VRC.SDKBase.VRCUrl))
                {
                    if(variableValue == null)
                        variableValue = new VRC.SDKBase.VRCUrl("");

                    VRC.SDKBase.VRCUrl url = (VRC.SDKBase.VRCUrl)variableValue;
                    EditorGUI.BeginChangeCheck();
                    variableValue = new VRC.SDKBase.VRCUrl(EditorGUILayout.TextField(symbol, url.Get()));

                    if (EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if (variableType == typeof(VRC.SDKBase.VRCUrl[]))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginVertical();

                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if (_arrayStates.ContainsKey(symbol))
                        showArray = _arrayStates[symbol];
                    else
                        _arrayStates.Add(symbol, false);
                    showArray = EditorGUILayout.Foldout( showArray, symbol, true );
                    _arrayStates[symbol] = showArray;

                    VRC.SDKBase.VRCUrl[] valueArray = (VRC.SDKBase.VRCUrl[])variableValue;

                    if (showArray)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.Space();
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1);
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);

                        if (valueArray != null && valueArray.Length > 0)
                        {
                            for (int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");
                                if (valueArray[i] == null)
                                    valueArray[i] = new VRC.SDKBase.VRCUrl("");

                                valueArray[i] = new VRC.SDKBase.VRCUrl(
                                    EditorGUILayout.TextField(
                                        $"{i}:",
                                        valueArray.Length > i ? valueArray[i].Get() : ""));
                            }
                        }
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    if (EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else if (variableType == typeof(LayerMask))
                {
                    LayerMask maskValue = (LayerMask)variableValue;
                    GUI.SetNextControlName("NodeField");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.LabelField(symbol);
                    // Using workaround from http://answers.unity.com/answers/1387522/view.html
                    LayerMask tempMask = EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(maskValue), InternalEditorUtility.layers);
                    variableValue = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
                    if (EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                }
                else if (variableType == typeof(TextureInfo))
                {
                    TextureInfo colorValue = (TextureInfo)variableValue;
                    if(variableValue == null)
                        colorValue = new TextureInfo();
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(symbol);
                    EditorGUI.indentLevel++;
                    colorValue.GenerateMipMaps = EditorGUILayout.Toggle("GenerateMipMaps", colorValue.GenerateMipMaps);
                    colorValue.FilterMode = (FilterMode)EditorGUILayout.EnumPopup("FilterMode", colorValue.FilterMode);
                    colorValue.WrapModeU = (TextureWrapMode)EditorGUILayout.EnumPopup("WrapModeU", colorValue.WrapModeU);
                    colorValue.WrapModeV = (TextureWrapMode)EditorGUILayout.EnumPopup("WrapModeV", colorValue.WrapModeV);
                    colorValue.WrapModeW = (TextureWrapMode)EditorGUILayout.EnumPopup("WrapModeW", colorValue.WrapModeW);
                    colorValue.AnisoLevel = EditorGUILayout.IntSlider("AnisoLevel", colorValue.AnisoLevel, 0, 16);
                    colorValue.MaterialProperty = EditorGUILayout.TextField("MaterialProperty", colorValue.MaterialProperty);
                    variableValue = colorValue;
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                    if (EditorGUI.EndChangeCheck())
                    {
                        dirty = true;
                    }
                } 
                else if (variableType == typeof(TextureInfo[]))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.BeginVertical();

                    GUI.SetNextControlName("NodeField");
                    bool showArray = false;
                    if (_arrayStates.ContainsKey(symbol))
                        showArray = _arrayStates[symbol];
                    else
                        _arrayStates.Add(symbol, false);
                    showArray = EditorGUILayout.Foldout(showArray, symbol, true);
                    _arrayStates[symbol] = showArray;

                    TextureInfo[] valueArray = (TextureInfo[])variableValue;

                    if (valueArray == null) valueArray = new TextureInfo[0];

                    if (showArray)
                    {
                        EditorGUI.indentLevel++;
                        int newSize = EditorGUILayout.IntField(
                            "size:",
                            valueArray != null && valueArray.Length > 0 ? valueArray.Length : 1);
                        newSize = newSize >= 0 ? newSize : 0;
                        Array.Resize(ref valueArray, newSize);

                        if (valueArray != null && valueArray.Length > 0)
                        {
                            for (int i = 0; i < valueArray.Length; i++)
                            {
                                GUI.SetNextControlName("NodeField");

                                TextureInfo colorValue = valueArray[i];

                                if (colorValue == null)
                                    colorValue = new TextureInfo();

                                EditorGUILayout.BeginVertical();
                                EditorGUILayout.LabelField(symbol + " " + i);
                                EditorGUI.indentLevel++;
                                colorValue.GenerateMipMaps = EditorGUILayout.Toggle("GenerateMipMaps", colorValue.GenerateMipMaps);
                                colorValue.FilterMode = (FilterMode)EditorGUILayout.EnumPopup("FilterMode", colorValue.FilterMode);
                                colorValue.WrapModeU = (TextureWrapMode)EditorGUILayout.EnumPopup("WrapModeU", colorValue.WrapModeU);
                                colorValue.WrapModeV = (TextureWrapMode)EditorGUILayout.EnumPopup("WrapModeV", colorValue.WrapModeV);
                                colorValue.WrapModeW = (TextureWrapMode)EditorGUILayout.EnumPopup("WrapModeW", colorValue.WrapModeW);
                                colorValue.AnisoLevel = EditorGUILayout.IntSlider("AnisoLevel", colorValue.AnisoLevel, 0, 16);
                                colorValue.MaterialProperty = EditorGUILayout.TextField("MaterialProperty", colorValue.MaterialProperty);
                                EditorGUI.indentLevel--;
                                EditorGUILayout.EndVertical();

                                valueArray[i] = colorValue;
                            }
                        }
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    if (EditorGUI.EndChangeCheck())
                    {
                        variableValue = valueArray;
                        dirty = true;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(symbol + " no defined editor for type of " + variableType);
                }
                // ReSharper restore RedundantNameQualifier

                IUdonSyncMetadata sync = program.SyncMetadataTable.GetSyncMetadataFromSymbol(symbol);
                if(sync != null)
                {
                    GUILayout.Label($"sync{sync.Properties[0].InterpolationAlgorithm.ToString()}", GUILayout.Width(80));
                }
            }

            EditorGUILayout.EndHorizontal();

            return variableValue;
        }

        #region Serialization Methods

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (this == null)
            {
                return;
            }
            OnAfterDeserialize();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (this == null)
            {
                return;
            }
            OnBeforeSerialize();
        }

        [PublicAPI]
        protected virtual void OnAfterDeserialize()
        {
        }

        [PublicAPI]
        protected virtual void OnBeforeSerialize()
        {
        }

        #endregion
    }
}
