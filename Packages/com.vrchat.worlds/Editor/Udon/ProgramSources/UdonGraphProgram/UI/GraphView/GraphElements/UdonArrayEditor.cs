#if UNITY_2019_3_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using System;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonArrayEditor : VisualElement
    {
        private IArrayProvider _inspector;
        private Button _editArrayButton;
        private Action<object> _setValueCallback;
        private Type _type;
        private object _value;
        private bool _inspectorOpen = false;

        public UdonArrayEditor(Type t, Action<object> valueChangedAction, object value)
        {
            _setValueCallback = valueChangedAction;
            _value = value;
            _type = t.GetElementType();
            
            _editArrayButton =  new Button(EditArray)
            {
                text = "Edit",
                name = "array-editor",
            };
            
            Add(_editArrayButton);
        }

        private void EditArray()
        {
            if (_inspector == null)
            {
                // Create it new
                Type typedArrayInspector = (typeof(UdonArrayInspector<>)).MakeGenericType(_type);
                _inspector = (Activator.CreateInstance(typedArrayInspector, null, _value) as IArrayProvider);

                AddInspector();
                _inspectorOpen = true;
                _editArrayButton.text = "Save";
                return;
            }
            else
            {
                // Update Values when 'Save' is clicked
                if(_inspectorOpen)
                {
                    // Update Values
                    var values = _inspector.GetValues();
                    _setValueCallback(values);

                    // Remove Inspector
                    _inspector.RemoveFromHierarchy();

                    // Update Button Text
                    _editArrayButton.text = "Edit";
                    _inspectorOpen = false;
                    return;
                }
                else
                {
                    // Inspector exists, it's just removed
                    _inspectorOpen = true;
                    AddInspector();
                    _editArrayButton.text = "Save";
                }
            }
        }
        
        private void AddInspector()
        {
            if (parent.GetType() == typeof(UdonPort))
            {
                parent.parent.Add(_inspector as VisualElement);
            }
            else
            {
                Add(_inspector as VisualElement);   
            }
        }
    }
}