#if UNITY_2019_3_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonArrayInspector<T> : VisualElement, IArrayProvider
    {
        private ScrollView _scroller;
        private List<VisualElement> _fields = new List<VisualElement>();
        private IntegerField _sizeField;
        private Action<object> _setValueCallback;

        public UdonArrayInspector(Action<object> valueChangedAction, object value)
        {
            _setValueCallback = valueChangedAction;
            
            AddToClassList("input-inspector");
            var resizeContainer = new VisualElement()
            {
                name = "resize-container",
            };
            resizeContainer.Add(new Label("size"));

            _sizeField = new IntegerField();
            _sizeField.isDelayed = true;
            #if UNITY_2019_3_OR_NEWER
            _sizeField.RegisterValueChangedCallback((evt) =>
            #else
            _sizeField.OnValueChanged((evt) =>
            #endif
            {
                ResizeTo(evt.newValue);
            });
            resizeContainer.Add(_sizeField);
            Add(resizeContainer);

            _scroller = new ScrollView()
            {
                name = "array-scroll",
            };
            Add(_scroller);

            if (value == null)
            {
                // Can't show if array is empty
                _sizeField.value = 0;
                return;
            }
            else
            {
                IEnumerable<T> values = (value as IEnumerable<T>);
                if (values == null)
                {
                    Debug.LogError($"Couldn't convert {value} to {typeof(T).ToString()} Array");
                    return;
                }

                // Populate fields and their values from passed-in array
                _fields = new List<VisualElement>();
                foreach (var item in values)
                {
                    var field = GetValueField(item);
                    _fields.Add(field);

                    _scroller.contentContainer.Add(field);
                }

                _sizeField.value = values.Count();
            }
            
        }

        private void ResizeTo(int newValue)
        {
            _sizeField.value = newValue;

            // Create from scratch if currentFields are null
            if(_fields == null)
            {
                Debug.Log($"Creating from Scratch");
                _fields = new List<VisualElement>();
                for (int i = 0; i < newValue; i++)
                {
                    var field = GetValueField(null);
                    _fields.Add(field);
                    _scroller.contentContainer.Add(field as VisualElement);
                }
                return;
            }

            // Shrink list if new value is less than old one
            if(newValue < _fields.Count)
            {
                for (int i = _fields.Count - 1; i >= newValue; i--)
                {
                    (_fields[i] as VisualElement).RemoveFromHierarchy();
                    _fields.RemoveAt(i);
                }
                MarkDirtyRepaint();
                return;
            }

            // Expand list if new value is more than old one.
            if(newValue > _fields.Count)
            {
                int numberToAdd = newValue - _fields.Count;
                for (int i = 0; i < numberToAdd; i++)
                {
                    var field = GetValueField(null);
                    if (field == null)
                    {
                        Debug.LogWarning($"Sorry, can't edit object of type {typeof(T).ToString()} yet.");
                        return;
                    }
                    _fields.Add(field);

                    _scroller.contentContainer.Add(field as VisualElement);
                }
                MarkDirtyRepaint();
                return;
            }
        }

        private VisualElement GetValueField(object value)
        {
            return UdonFieldFactory.CreateField(typeof(T), value, _setValueCallback);
        }

        public object GetValues()
        {
            var result = new List<T>();
            for (int i = 0; i < _fields.Count; i++)
            {
                var f = (_fields[i] as INotifyValueChanged<T>);
                result.Add(f.value);
            }
            return result.ToArray();
        }

    }

    public interface IArrayProvider
    {
        object GetValues();
        void RemoveFromHierarchy(); // in VisualElement
    }
}