using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Editor.ProgramSources.Attributes;
using VRC.Udon.Editor.ProgramSources.UdonGraphProgram;
using VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView;
using VRC.Udon.Graph;
using VRC.Udon.Graph.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;
using Object = UnityEngine.Object;

[assembly: UdonProgramSourceNewMenu(typeof(UdonGraphProgramAsset), "Udon Graph Program Asset")]

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram
{
    [CreateAssetMenu(menuName = "VRChat/Udon/Udon Graph Program Asset", fileName = "New Udon Graph Program Asset")]
    public class UdonGraphProgramAsset : UdonAssemblyProgramAsset, IUdonGraphDataProvider
    {
        [SerializeField]
        public UdonGraphData graphData = new UdonGraphData();

        [SerializeField]
        public UdonGraphElementData[] graphElementData = Array.Empty<UdonGraphElementData>();
        
        [SerializeField]
        // ReSharper disable once NotAccessedField.Global
        public string version = "1.0.0";

        [SerializeField]
        private bool showAssembly;

        [NonSerialized, OdinSerialize]
        private Dictionary<string, (object value, Type type)> heapDefaultValues = new Dictionary<string, (object value, Type type)>();

        protected override void DrawProgramSourceGUI(UdonBehaviour udonBehaviour, ref bool dirty)
        {
            if (GUILayout.Button("Open Udon Graph", "LargeButton"))
            {
                var w = EditorWindow.GetWindow<UdonGraphWindow>("Udon Graph", true, typeof(SceneView));
                w.LoadGraphFromAsset(this, udonBehaviour);
            }

            DrawInteractionArea(udonBehaviour);
            DrawPublicVariables(udonBehaviour, ref dirty);
            DrawAssemblyErrorTextArea();
            DrawAssemblyTextArea(false, ref dirty);
        }

        protected override void RefreshProgramImpl()
        {
            if(graphData == null)
            {
                return;
            }

            CompileGraph();
            base.RefreshProgramImpl();
            ApplyDefaultValuesToHeap();
        }

        protected override void DrawAssemblyTextArea(bool allowEditing, ref bool dirty)
        {
            EditorGUI.BeginChangeCheck();
            bool newShowAssembly = EditorGUILayout.Foldout(showAssembly, "Compiled Graph Assembly");
            if(EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Toggle Assembly Foldout");
                showAssembly = newShowAssembly;
            }

            if(!showAssembly)
            {
                return;
            }

            EditorGUI.indentLevel++;
            base.DrawAssemblyTextArea(allowEditing, ref dirty);
            EditorGUI.indentLevel--;
        }

        [PublicAPI]
        protected void CompileGraph()
        {
            udonAssembly = UdonEditorManager.Instance.CompileGraph(graphData, null, out Dictionary<string, (string uid, string fullName, int index)> _, out heapDefaultValues);
        }

        [PublicAPI]
        protected void ApplyDefaultValuesToHeap()
        {
            IUdonSymbolTable symbolTable = program?.SymbolTable;
            IUdonHeap heap = program?.Heap;
            if(symbolTable == null || heap == null)
            {
                return;
            }

            foreach(KeyValuePair<string, (object value, Type type)> defaultValue in heapDefaultValues)
            {
                if(!symbolTable.HasAddressForSymbol(defaultValue.Key))
                {
                    continue;
                }

                uint symbolAddress = symbolTable.GetAddressFromSymbol(defaultValue.Key);
                (object value, Type declaredType) = defaultValue.Value;
                if(typeof(Object).IsAssignableFrom(declaredType))
                {
                    if(value != null && !declaredType.IsInstanceOfType(value))
                    {
                        heap.SetHeapVariable(symbolAddress, null, declaredType);
                        continue;
                    }

                    if((Object)value == null)
                    {
                        heap.SetHeapVariable(symbolAddress, null, declaredType);
                        continue;
                    }
                }

                if(value != null)
                {
                    if(!declaredType.IsInstanceOfType(value))
                    {
                        value = declaredType.IsValueType ? Activator.CreateInstance(declaredType) : null;
                    }
                }

                if(declaredType == null)
                {
                    declaredType = typeof(object);
                }
                heap.SetHeapVariable(symbolAddress, value, declaredType);
            }
        }

        protected override object GetPublicVariableDefaultValue(string symbol, Type type)
        {
            IUdonSymbolTable symbolTable = program?.SymbolTable;
            IUdonHeap heap = program?.Heap;
            if(symbolTable == null || heap == null)
            {
                return null;
            }

            if(!heapDefaultValues.ContainsKey(symbol))
            {
                return null;
            }

            (object value, Type declaredType) = heapDefaultValues[symbol];
            if(!typeof(Object).IsAssignableFrom(declaredType))
            {
                return value;
            }

            return (Object)value == null ? null : value;
        }

        protected override object DrawPublicVariableField(string symbol, object variableValue, Type variableType, ref bool dirty,
            bool enabled)
        {
            EditorGUILayout.BeginHorizontal();
            variableValue = base.DrawPublicVariableField(symbol, variableValue, variableType, ref dirty, enabled);
            object defaultValue = null;
            if(heapDefaultValues.ContainsKey(symbol))
            {
                defaultValue = heapDefaultValues[symbol].value;
            }

            if(variableValue == null || !variableValue.Equals(defaultValue))
            {
                if(defaultValue != null)
                {
                    if(!dirty && GUILayout.Button("Reset to Default Value"))
                    {
                        variableValue = defaultValue;
                        dirty = true;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            return variableValue;
        }

        #region Serialization Methods

        protected override void OnAfterDeserialize()
        {
            foreach(UdonNodeData node in graphData.nodes)
            {
                node.SetGraph(graphData);
            }
        }

        #endregion

        public UdonGraphData GetGraphData()
        {
            return graphData;
        }
    }
}
