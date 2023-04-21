using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.Udon.Graph;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonGraphVariables : VisualElement, IUdonGraphElementDataProvider
    {
        private readonly CustomData _customData = new CustomData();
        private readonly UdonGraph _graph;
        private readonly VisualElement _list; 
        private readonly Label _placeholder;
        private Button _addButton;
        private readonly string _guid;
        private Dictionary<string, UdonVariableRow> _idToRow;
        private Action<VisualElement, string> EditTextRequested { get; set; }

        public UdonGraphVariables(UdonGraph graph, UdonSearchManager searchManager, Action<VisualElement, string> editTextRequested)
        {
            EditTextRequested = editTextRequested;
            _guid = GUID.Generate().ToString();
            _graph = graph;

            AddToClassList("baseBlock");
            
            var header = new VisualElement(); 
            header.AddToClassList("header");
            Add(header);
            
            header.RegisterCallback((MouseUpEvent e) =>
            {
                ToggleInClassList("collapsed");
            });

            var leftSide = new VisualElement();
            header.Add(leftSide);

            var collapseIcon = new VisualElement();
            collapseIcon.AddToClassList("collapseIcon");
            leftSide.Add(collapseIcon);

            var headerText = new Label("Variables");
            leftSide.Add(headerText);
            var eventAddBtn = new Button(() =>
            {
                var screenPosition = GUIUtility.GUIToScreenPoint(_graph.lastMousePosition);
                searchManager.OpenVariableSearch(screenPosition);
            })
            {
                text = "+"
            };
            eventAddBtn.AddToClassList("addEventBtn");
            header.Add(eventAddBtn);
            _list = new VisualElement();
            _list.AddToClassList("list");
            Add(_list);
            _placeholder = new Label("No Variables Added");
            _placeholder.AddToClassList("placeholder");
            _list.Add(_placeholder);
            _idToRow = new Dictionary<string, UdonVariableRow>();
            
            _list.RegisterCallback<KeyDownEvent>(_graph.OnKeyDown);
        }
        
        public void AddFromData(UdonNodeData nodeData)
        {
            // don't add internal variables, which start with __
            // Todo: handle all "__" variables instead, need to tell community first and let the word spread
            string newVariableName = (string)nodeData.nodeValues[(int)UdonParameterProperty.ValueIndices.Name].Deserialize();
            if (newVariableName.StartsWithCached("__returnValue"))
            {
                return;
            }
            
            UdonNodeDefinition definition = UdonEditorManager.Instance.GetNodeDefinition(nodeData.fullName);
            if (definition != null)
            {
                var row = new UdonVariableRow(new UdonParameterField(_graph, nodeData, EditTextRequested),
                    new UdonParameterProperty(definition, nodeData));
                row.AddToClassList("udonVariable");
                _list.Add(row);
                _idToRow.Add(nodeData.uid, row);
            }

            if (_list.Contains(_placeholder))
            {
                _placeholder.RemoveFromHierarchy();
            }
        }
        
        public void LoadData(UdonGraphElementData data)  
        { 
            JsonUtility.FromJsonOverwrite(data.jsonData, _customData);
            visible = _customData.visible;
        }
        
        private void SaveData()
        {
            _graph.SaveGraphElementData(this);
        }

        public UdonGraphElementData GetData()
        {
            return new UdonGraphElementData(UdonGraphElementType.VariablesWindow, _guid, JsonUtility.ToJson(_customData));
        }
        
        public void RemoveByID(string id)
        {
            if (!_idToRow.TryGetValue(id, out UdonVariableRow row)) return;
            _list.Remove(row);
            _idToRow.Remove(id);
        }

        public new void Clear()
        {
            _idToRow?.Clear();
            _list.Add(_placeholder);
            var oldVars = this.Query<UdonVariableRow>(null, "udonVariable").ToList();
            foreach (var oldVar in oldVars)
            {
                _list.Remove(oldVar);
            }
        }
        
        [Serializable]
        private class CustomData {
            public bool visible = true;
        }
    }
}
