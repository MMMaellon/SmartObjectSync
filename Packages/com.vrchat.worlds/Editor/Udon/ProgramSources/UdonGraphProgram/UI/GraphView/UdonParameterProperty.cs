using System;
using UnityEngine.UIElements;
using EditorUI = UnityEngine.UIElements;
using EngineUI = UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using VRC.Udon.Graph;
using VRC.Udon.Serialization;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonParameterProperty : VisualElement
	{
        private readonly UdonNodeData nodeData;
        private readonly UdonNodeDefinition definition;

        private Toggle IsPublic { get; }
        private Toggle IsSynced { get; }
        private VisualElement DefaultValueContainer { get; }
        private EditorUI.PopupField<string> SyncField { get; }
		private VisualElement _inputField;

        public enum ValueIndices
        {
            Value = 0,
            Name = 1,
            IsPublic = 2,
            IsSynced = 3,
            SyncType = 4,
        }

        // ReSharper disable once UnusedMember.Local
        private static SerializableObjectContainer[] GetDefaultNodeValues()
        {
            return new[]
            {
                SerializableObjectContainer.Serialize("", typeof(string)),
                SerializableObjectContainer.Serialize("newVariableName", typeof(string)),
                SerializableObjectContainer.Serialize(false, typeof(bool)),
                SerializableObjectContainer.Serialize(false, typeof(bool)),
                SerializableObjectContainer.Serialize("none", typeof(string))
            };
        }

        // 0 = Value, 1 = name, 2 = public, 3 = synced, 4 = syncType
        public UdonParameterProperty(UdonNodeDefinition definition, UdonNodeData nodeData)
        {
            this.definition = definition;
            this.nodeData = nodeData;

            string friendlyName = UdonGraphExtensions.FriendlyTypeName(definition.type).FriendlyNameify();
            
            // Public Toggle
            IsPublic = new Toggle
            {
                text = "public",
                value = (bool) GetValue(ValueIndices.IsPublic)
            };
            IsPublic.RegisterValueChangedCallback(
                e => { SetNewValue(e.newValue, ValueIndices.IsPublic); });
            Add(IsPublic);

            if(UdonNetworkTypes.CanSync(definition.type))
            {
                // Is Synced Field
                IsSynced = new Toggle
                {
                    text = "synced",
                    value = (bool)GetValue(ValueIndices.IsSynced),
                    name = "syncToggle",
                };
                
                IsSynced.RegisterValueChangedCallback(
                    e =>
                    {
                        SetNewValue(e.newValue, ValueIndices.IsSynced);
                        SyncField.visible = e.newValue;
                    });
                
                // Sync Field, add to isSynced
                List<string> choices = new List<string>
                {
                    "none"
                };

                if(UdonNetworkTypes.CanSyncLinear(definition.type))
                {
                    choices.Add("linear");
                }

                if(UdonNetworkTypes.CanSyncSmooth(definition.type))
                {
                    choices.Add("smooth");
                }

                SyncField = new EditorUI.PopupField<string>(choices, 0)
                {
                    visible = IsSynced.value,
                };
                SyncField.Insert(0, new Label("smooth:"));

                SyncField.RegisterValueChangedCallback(
                    e =>
                    {
                        SetNewValue(e.newValue, ValueIndices.SyncType);
                    });

                // Only show sync smoothing dropdown if there are choices to be made
                if (choices.Count > 1)
                {
                    IsSynced.Add(SyncField);
                }
                
                Add(IsSynced);
            }
            else
            {
                // Cannot Sync
                SetNewValue(false, ValueIndices.IsSynced);
                Add(new Label($"{friendlyName} cannot be synced."));
            }

            // Container to show/edit Default Value
            DefaultValueContainer = new VisualElement();
            DefaultValueContainer.Add(
                new Label("default value") {name = "default-value-label"});

            // Generate Default Value Field
            TryGetValueObject(out object result);
            _inputField = UdonFieldFactory.CreateField(
                definition.type,
                result,
                newValue => SetNewValue(newValue, ValueIndices.Value)
            );
            if (_inputField != null)
            {
                DefaultValueContainer.Add(_inputField);
                Add(DefaultValueContainer);
            }
        }

        private object GetValue(ValueIndices index)
        {
            if ((int)index < nodeData.nodeValues.Length) return nodeData.nodeValues[(int)index].Deserialize();
            Debug.LogWarning($"Can't get {index} from {definition.name} variable");
            return null;

        }

        private bool TryGetValueObject(out object result)
        {
            result = null;

            var container = nodeData.nodeValues[0];
            if (container == null)
            {
                return false;
            }

            result = container.Deserialize();
            if (result == null)
            {
                return false;
            }

            return true;
        }

        private void SetNewValue(object newValue, ValueIndices index)
        {
            nodeData.nodeValues[(int) index] = SerializableObjectContainer.Serialize(newValue);
        }

        // Convenience wrapper for field types that don't need special initialization
        // ReSharper disable once UnusedMember.Local
        private VisualElement SetupField<TField, TType>()
            where TField : VisualElement, INotifyValueChanged<TType>, new()
        {
            var field = new TField();
            return SetupField<TField, TType>(field, ValueIndices.Name);
        }

        // Works for any TextValueField types, needs to know fieldType and object type
        private VisualElement SetupField<TField, TType>(TField field, ValueIndices index)
            where TField : VisualElement, INotifyValueChanged<TType>
        {
            field.AddToClassList("portField");
            if (TryGetValueObject(out object result))
            {
                field.value = (TType) result;
            }
            field.RegisterValueChangedCallback(
                e => SetNewValue(e.newValue, index));
            _inputField = field;
            return _inputField;
        }
    }
}
