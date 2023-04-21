using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using EditorUI = UnityEditor.UIElements;
using EngineUI = UnityEngine.UIElements;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Udon.Graph;
using VRC.Udon.Serialization;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    [Serializable]
    public class UdonPort : Port
    {
        private UdonNodeData _udonNodeData;
        private int _nodeValueIndex;

        private VisualElement _inputField;
        private VisualElement _inputFieldTypeLabel;

        private IArrayProvider _inspector;

        protected UdonPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) :
            base(portOrientation, portDirection, portCapacity, type)
        {
        }

        public static Port Create(string portName, Direction portDirection, IEdgeConnectorListener connectorListener,
            Type type, UdonNodeData data, int index, Orientation orientation = Orientation.Horizontal)
        {

            Capacity capacity = Capacity.Single;
            if (portDirection == Direction.Input && type == null || portDirection == Direction.Output && type != null)
            {
                capacity = Capacity.Multi;
            }

            UdonPort port = new UdonPort(orientation, portDirection, capacity, type)
            {
                m_EdgeConnector = new EdgeConnector<Edge>(connectorListener),
                portName = portName,
                _udonNodeData = data,
                _nodeValueIndex = index
            };

            port.SetupPort();
            return port;
        }

        public int GetIndex()
        {
            return _nodeValueIndex;
        }

        private bool _isSendChangePort;
        
        private void SetupPort()
        {
            _isSendChangePort = portName == "sendChange";
            if (_isSendChangePort)
            {
                m_EdgeConnector = null;
            }
            else
            {
                this.AddManipulator(m_EdgeConnector);   
            }

            tooltip = UdonGraphExtensions.FriendlyTypeName(portType);

            if (portType == null || direction == Direction.Output)
            {
                return;
            }

            if (TryGetValueObject(out object result, portType))
            {
                var field = UdonFieldFactory.CreateField(
                    portType,
                    result,
                    SetNewValue
                );

                if (field != null)
                {
                    SetupField(field);
                }
            }

            if (_udonNodeData.fullName.StartsWithCached("Const"))
            {
                RemoveConnectorAndLabel();
            }
            else if (_udonNodeData.fullName.StartsWithCached("Set_Variable") && _nodeValueIndex == 2)
            {
                _isSendChangePort = true;
                RemoveConnector();
                AddToClassList("send-change");
            }
            
            AddToClassList(portName);

            UpdateLabel(connected);
        }

        // Made its own method for now as we have issues auto-converting between string and char in a TextField
        // TODO: refactor SetupField so we can do just the field.value part separately to combine with this
        private VisualElement SetupCharField()
        {
            TextField field = new TextField();
            field.AddToClassList("portField");
            if (TryGetValueObject(out object result))
            {
                field.value = UdonGraphExtensions.UnescapeLikeALiteral((char) result);
            }

            field.isDelayed = true;

            // Special handling for escaping char value
            field.RegisterValueChangedCallback(
                e =>
                {
                    if (e.newValue[0] == '\\' && e.newValue.Length > 1)
                    {
                        SetNewValue(UdonGraphExtensions.EscapeLikeALiteral(e.newValue.Substring(0, 2)));
                    }
                    else
                    {
                        SetNewValue(e.newValue[0]);
                    }
                });
            _inputField = field;

            // Add label, shown when input is connected. Not shown by default
            var friendlyName = UdonGraphExtensions.FriendlyTypeName(typeof(char)).FriendlyNameify();
            var label = new Label(friendlyName);
            _inputFieldTypeLabel = label;

            return _inputField;
        }

        private void SetupField(VisualElement field)
        {
            // Custom Event fields need their event names sanitized after input and their connectors removed
            if (string.Compare(_udonNodeData.fullName, "Event_Custom", StringComparison.Ordinal) == 0)
            {
                var tField = (TextField) field;
                tField.RegisterValueChangedCallback(
                    (e) =>
                    {
                        string newValue = e.newValue.SanitizeVariableName();
                        tField.value = newValue;
                        SetNewValue(newValue);
                    });
                RemoveConnectorAndLabel();
            }

            // Add label, shown when input is connected. Not shown by default
            var friendlyName = UdonGraphExtensions.FriendlyTypeName(portType).FriendlyNameify();
            var label = new Label(friendlyName);
            _inputFieldTypeLabel = label;
            field.AddToClassList("portField");

            _inputField = field;
            Add(_inputField);
        }

        private void RemoveConnectorAndLabel()
        {
            RemoveConnector();
            this.Q(null, "connectorText")?.RemoveFromHierarchy();
        }
        
        private void RemoveConnector()
        {
            this.Q("connector")?.RemoveFromHierarchy();
        }

#pragma warning disable 0649 // variable never assigned
        // ReSharper disable once UnassignedField.Local
        private Button _editArrayButton;
#pragma warning restore 0649

        private void EditArray(Type elementType)
        {
            // Update Values when 'Save' is clicked
            if (_inspector != null)
            {
                // Update Values
                SetNewValue(_inspector.GetValues());

                // Remove Inspector
                _inspector.RemoveFromHierarchy();
                _inspector = null;

                // Update Button Text
                _editArrayButton.text = "Edit";
                return;
            }

            // Otherwise set up the inspector
            _editArrayButton.text = "Save";

            // Get value object, null is ok
            TryGetValueObject(out object value);

            // Create it new
            Type typedArrayInspector = (typeof(UdonArrayInspector<>)).MakeGenericType(elementType);
            _inspector = (Activator.CreateInstance(typedArrayInspector, value) as IArrayProvider);

            parent.Add(_inspector as VisualElement);
        }

        // Update elements on connect
        public override void Connect(Edge edge)
        {
            AddToClassList("connected");
            base.Connect(edge);
            
            Undo.RecordObject(((UdonNode)node).Graph.graphProgramAsset, "Connect Edge");

            // The below logic is just for Output ports
            if (edge.input.Equals(this)) return;

            // hide field, show label
            var input = ((UdonPort) edge.input);
            input.UpdateLabel(true);

            if (IsReloading())
            {
                return;
            }

            // update data
            if (portType == null)
            {
                // We are a flow port
                SetFlowUid(((UdonNode) input.node).uid);
                this.Compile();
            }
            else
            {
                // We are a value port, we need to send our info over to the OTHER node
                string myNodeUid = ((UdonNode) node).uid;
                input.SetDataFromNewConnection($"{myNodeUid}|{_nodeValueIndex}", input.GetIndex());
            }

            if (_isSendChangePort)
            {
                DisconnectAll();
                this.Reload();
            }
        }

        public override void OnStopEdgeDragging()
        {
            base.OnStopEdgeDragging();

            if (edgeConnector?.edgeDragHelper?.draggedPort == this)
            {
                if (capacity == Capacity.Single && connections.Any())
                {
                    // This port could only have one connection. Fixed in Reserialize, need to reload to show the change
                    this.Reload();
                }
            }
            else
            {
                this.Reload();
            }
        }

        private void SetFlowUid(string newValue)
        {
            if (_udonNodeData.flowUIDs.Length <= _nodeValueIndex)
            {
                // If we don't have space for this flow value, create a new array
                // TODO: handle this elsewhere?
                var newFlowArray = new string[_nodeValueIndex + 1];
                for (int i = 0; i < _udonNodeData.flowUIDs.Length; i++)
                {
                    newFlowArray[i] = _udonNodeData.flowUIDs[i];
                }

                _udonNodeData.flowUIDs = newFlowArray;

                _udonNodeData.flowUIDs.SetValue(newValue, _nodeValueIndex);
            }
            else
            {
                _udonNodeData.flowUIDs.SetValue(newValue, _nodeValueIndex);
            }
        }

        public bool IsReloading()
        {
            return node is UdonNode && ((UdonNode) node).Graph.IsReloading;
        }

        public void SetDataFromNewConnection(string uidAndPort, int index)
        {
            // can't do this for Reg stack nodes yet so skipping for demo
            if (_udonNodeData == null) return;

            if (_udonNodeData.nodeUIDs.Length <= _nodeValueIndex)
            {
                Debug.Log("Couldn't set it");
            }
            else
            {
                _udonNodeData.nodeUIDs.SetValue(uidAndPort, index);
            }
        }

        // Update elements on disconnect
        public override void Disconnect(Edge edge)
        {
            RemoveFromClassList("connected");
            if (node == null) return;
            Undo.RecordObject(((UdonNode)node).Graph.graphProgramAsset, "Connect Edge");
            base.Disconnect(edge);

            // hide label, show field
            if (direction == Direction.Input)
            {
                UpdateLabel(false);
            }

            if (IsReloading())
            {
                return;
            }

            // update data
            if (direction == Direction.Output && portType == null)
            {
                // We are a flow port
                SetFlowUid("");
                this.Compile();
            }
            else if (direction == Direction.Input && portType != null)
            {
                // Direction is input
                // We are a value port
                SetDataFromNewConnection("", GetIndex());
            }
        }

        public void UpdateLabel(bool isConnected)
        {
            // Port has a 'connected' bool but it doesn't seem to update, so passing 'isConnected' for now

            if (isConnected)
            {
                if (_inputField != null && Contains(_inputField))
                {
                    _inputField.RemoveFromHierarchy();
                }

                if (_inputFieldTypeLabel != null && !Contains(_inputFieldTypeLabel))
                {
                    Add(_inputFieldTypeLabel);
                }

                if (_editArrayButton != null && Contains(_editArrayButton))
                {
                    _editArrayButton.RemoveFromHierarchy();
                }
            }
            else
            {
                if (_inputField != null && !Contains(_inputField))
                {
                    Add(_inputField);
                }

                if (_inputFieldTypeLabel != null && Contains(_inputFieldTypeLabel))
                {
                    _inputFieldTypeLabel.RemoveFromHierarchy();
                }

                if (_editArrayButton != null && !Contains(_editArrayButton))
                {
                    Add(_editArrayButton);
                }
            }
        }

        private bool TryGetValueObject(out object result, Type type = null)
        {
            // Initialize out object
            result = null;

            // get container from node values
            SerializableObjectContainer container = _udonNodeData.nodeValues[_nodeValueIndex];

            // Null check, failure
            if (container == null)
                return false;

            // Deserialize into result, return failure on null
            result = container.Deserialize();

            // Strings will deserialize as null, that's ok
            if (type == null || type == typeof(string))
            {
                return true;
            }
            // any other type is not ok to be null
            else if (result == null)
            {
                return false;
            }

            // Success - return true
            return type.IsInstanceOfType(result);
        }

        private void SetNewValue(object newValue)
        {
            _udonNodeData.nodeValues[_nodeValueIndex] = SerializableObjectContainer.Serialize(newValue, portType);
        }
    }
}
