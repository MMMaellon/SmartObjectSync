using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using UnityEditor;
using UnityEngine;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public sealed class UdonComment : UdonGraphElement, IUdonGraphElementDataProvider
    {
        private readonly VisualElement _mainContainer;
        private readonly Label _label;
        private readonly TextField _textField;
        private readonly CustomData _customData = new CustomData();
        private readonly UdonGraph _graph;
        public UdonGroup group;

        // Called from Context menu and Reload
        public static UdonComment Create(string value, Rect position, UdonGraph graph)
        {
            var comment = new UdonComment("", graph);

            // make sure rect size is not 0
            position.width = position.width > 0 ? position.width : 128;
            position.height = position.height > 0 ? position.height : 40;

            comment._customData.layout = position;
            comment._customData.title = value;
            
            comment.UpdateFromData();
            UdonGraph.MarkSceneDirty();

            return comment;
        }

        public static UdonComment Create(UdonGraphElementData elementData, UdonGraph graph)
        {
            var comment = new UdonComment(elementData.jsonData, graph);
            
            comment.UpdateFromData();
            UdonGraph.MarkSceneDirty();
            
            return comment;
        }

        private UdonComment(string jsonData, UdonGraph graph)
        {
            name = "comment";
            _graph = graph;

            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable |
                            Capabilities.Resizable;
            pickingMode = PickingMode.Ignore;

            type = UdonGraphElementType.UdonComment;

            if(!string.IsNullOrEmpty(jsonData))
            {
                EditorJsonUtility.FromJsonOverwrite(jsonData, _customData);
            }

            _mainContainer = new VisualElement();
            _mainContainer.StretchToParentSize();
            _mainContainer.AddToClassList("mainContainer");
            Add(_mainContainer);

            _label = new Label();
            _label.RegisterCallback<MouseDownEvent>(OnLabelClick);
            _mainContainer.Add(_label);

            _textField = new TextField(1000, true, false, '*')
            {
                isDelayed = true
            };

            // Support IME
            _textField.RegisterCallback<FocusInEvent>(evt =>{ Input.imeCompositionMode = IMECompositionMode.On;});
            _textField.RegisterCallback<FocusOutEvent>(evt =>
            {
                SetText(_textField.text);
                Input.imeCompositionMode = IMECompositionMode.Auto;
                SwitchToEditMode(false);
            });

            _textField.RegisterValueChangedCallback(evt =>
            {
                SetText(evt.newValue);
                SwitchToEditMode(false);
            });
        }
        
        private void SaveNewData()
        {
            _graph.SaveGraphElementData(this);
        }

        private void UpdateFromData()
        {
            if(_customData != null)
            {
                layer = _customData.layer;
                if(string.IsNullOrEmpty(_customData.uid))
                {
                    _customData.uid = Guid.NewGuid().ToString();
                }

                uid = _customData.uid;

                SetPosition(_customData.layout);
                SetText(_customData.title);
            }
        }
        protected override void OnCustomStyleResolved(ICustomStyle istyle)
        {
            base.OnCustomStyleResolved(istyle);
            // Something is forcing style! Resetting a few things here.

            style.borderBottomWidth = 1;
            
            var resizer = this.Q(null, "resizer");
            if (resizer == null) return;
            resizer.style.paddingTop = 0;
            resizer.style.paddingLeft = 0;
        }

        public override void SetPosition(Rect newPos)
        {
            newPos = GraphElementExtension.GetSnappedRect(newPos);
            base.SetPosition(newPos);
        }

        public override void UpdatePresenterPosition()
        {
            base.UpdatePresenterPosition();
            _customData.layout = GraphElementExtension.GetSnappedRect(GetPosition());
            SaveNewData();
            group?.SaveNewData();
        }

        private double lastClickTime;
        private const double DOUBLE_CLICK_SPEED = 0.5;

        private void OnLabelClick(MouseDownEvent evt)
        {
            var newTime = EditorApplication.timeSinceStartup;
            if(newTime - lastClickTime < DOUBLE_CLICK_SPEED)
            {
                SwitchToEditMode(true);
            }

            lastClickTime = newTime;
        }

        private void SwitchToEditMode(bool switchingToEdit)
        {
            if (switchingToEdit)
            {
                _mainContainer.Remove(_label);
                _textField.value = _label.text;
                _mainContainer.Add(_textField);
                _textField.delegatesFocus = true;
                _textField.Focus();
            }
            else
            {
                _mainContainer.Remove(_textField);
                _mainContainer.Add(_label);
            }

            MarkDirtyRepaint();
        }

        private void SetText(string value)
        {
            Undo.RecordObject(_graph.graphProgramAsset, "Rename Comment");
            value = value.TrimEnd();
            _customData.title = value;
            _label.text = value;
            SaveNewData();
            MarkDirtyRepaint();
        }
        
        public UdonGraphElementData GetData()
        {
            return new UdonGraphElementData(UdonGraphElementType.UdonComment, uid,
                EditorJsonUtility.ToJson(_customData));
        }

        // CustomData is serialized in user assets, so we can't rename/modify/remove any of these variables 
        // ReSharper disable InconsistentNaming
        // ReSharper disable NotAccessedField.Local
        // ReSharper disable FieldCanBeMadeReadOnly.Local
        // ReSharper disable UnusedMember.Global
        // ReSharper disable UnassignedField.Global
#pragma warning disable CS0649
        private class CustomData
        {
            public string uid;
            public Rect layout;
            public string title = "Comment";
            public int layer;
            public Color elementTypeColor;
            
        }
#pragma warning restore CS0649
        // ReSharper enable UnassignedField.Global
        // ReSharper enable UnusedMember.Global
        // ReSharper enable InconsistentNaming
        // ReSharper enable NotAccessedField.Local
        // ReSharper enable FieldCanBeMadeReadOnly.Local
    }
}
