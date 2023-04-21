using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonGroup : Group, IUdonGraphElementDataProvider
    {
        public Action UpdateGraphGroups;
        private string Uid { get; set; }

        private readonly CustomData _customData = new CustomData();
        private readonly UdonGraph _graph;
        private const int GROUP_LAYER = -1;

        public static UdonGroup Create(string value, Rect position, UdonGraph graph)
        {
            var group = new UdonGroup("", graph)
            {
                Uid = Guid.NewGuid().ToString()
            };

            // make sure rect size is not 0
            position.width = position.width > 0 ? position.width : 128;
            position.height = position.height > 0 ? position.height : 128;

            group._customData.uid = group.Uid;
            group._customData.layout = position;
            group._customData.title = value;
            group._customData.layer = GROUP_LAYER;
            group._customData.elementTypeColor = Color.black;

            return group;
        }

        // Called in Reload > RestoreElementFromData
        public static UdonGroup Create(UdonGraphElementData elementData, UdonGraph graph)
        {
            return new UdonGroup(elementData.jsonData, graph);
        }

        // current order of operations issue when creating a group from the context menu means this isn't set until first save. This allows us to force it.
        public void UpdateDataId()
        {
            _customData.uid = Uid;
        }

        // Build a Group from jsonData, save to userData
        private UdonGroup(string jsonData, UdonGraph graph)
        {
            title = "Group";
            _graph = graph;
            layer = GROUP_LAYER;

            if (!string.IsNullOrEmpty(jsonData))
            {
                EditorJsonUtility.FromJsonOverwrite(jsonData, _customData);
            }
            
            // listen for changes on child elements
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public sealed override string title
        {
            get => base.title;
            set
            {
                base.title = value;
                UpdateGraphGroups?.Invoke();
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            _customData.layout = GraphElementExtension.GetSnappedRect(GetPosition());
        }

        public void Initialize()
        {
            if (_customData != null)
            {
                // Propagate data to useful places
                title = _customData.title;
                layer = _customData.layer;
                if (string.IsNullOrEmpty(_customData.uid))
                {
                    _customData.uid = Guid.NewGuid().ToString();
                }

                Uid = _customData.uid;

                // Add all elements from graph to self
                var childUIDs = _customData.containedElements;
                if (childUIDs.Count > 0)
                {
                    foreach (var item in childUIDs)
                    {
                        GraphElement element = _graph.GetElementByGuid(item);
                        if (element != null)
                        {
                            if (ContainsElement(element)) continue;
                            AddElement(element);
                            if (element is UdonComment c)
                            {
                                c.group = this;
                            }
                            else if (element is UdonNode n)
                            {
                                n.group = this;
                            }
                        }
                    }
                }
                else
                {
                    // No children, so restore the saved position
                    SetPosition(_customData.layout);
                }
            }
        }

        public override void SetPosition(Rect newPos)
        {
            newPos = GraphElementExtension.GetSnappedRect(newPos);
            base.SetPosition(newPos);
        }

        public void SaveNewData()
        {
            _graph.SaveGraphElementData(this);
        }

        // Save data to asset after new position set
        public override void UpdatePresenterPosition()
        {
            base.UpdatePresenterPosition();
            Rect snappedRect = GraphElementExtension.GetSnappedRect(GetPosition());
            base.SetPosition(snappedRect);
            SaveNewData();
        }

        public void SelectGroup()
        {
            _graph.ClearSelection();
            // groups exist on a separate layer and fairly opaque from an actual graph view, so it doesn't report them in the `GetElementByGuid`
            // so we force-select them via UI Elements ways
            _graph.AddToSelection(_graph.contentViewContainer.Query<UdonGroup>().Where(g => g.Equals(this)).First());
            var childUIDs = _customData.containedElements;
            if (childUIDs.Count > 0)
            {
                foreach (var item in childUIDs)
                {
                    _graph.AddToSelection( _graph.GetElementByGuid(item));
                }
            }
            _graph.FrameSelection();
        }

        // Save data to asset after rename
        protected override void OnGroupRenamed(string oldName, string newName)
        {
            // limit name to 100 characters
            title = newName.Substring(0, Mathf.Min(newName.Length, 100));
            _customData.title = title;
            SaveNewData();
        }

        protected override void OnElementsAdded(IEnumerable<GraphElement> elements)
        {
            //Enumerate first to prevent multiple-enumeration 
            IEnumerable<GraphElement> graphElements = elements as GraphElement[] ?? elements.ToArray();
            base.OnElementsAdded(graphElements);
            foreach (var element in graphElements)
            {
                if (!_customData.containedElements.Contains(element.GetUid()))
                {
                    _customData.containedElements.Add(element.GetUid());
                }

                switch (element)
                {
                    // Set group variable on UdonNodes
                    case UdonNode node:
                        node.group = this;
                        node.BringToFront();
                        break;
                    case UdonComment comment:
                        comment.group = this;
                        break;
                }
            }
            SaveNewData();
        }

        protected override void OnElementsRemoved(IEnumerable<GraphElement> elements)
        {
            //Enumerate first to prevent multiple-enumeration
            IEnumerable<GraphElement> graphElements = elements as GraphElement[] ?? elements.ToArray();
            base.OnElementsRemoved(graphElements);
            foreach (var element in graphElements)
            {
                if (!_customData.containedElements.Contains(element.GetUid())) continue;
                _customData.containedElements.Remove(element.GetUid());
                switch (element)
                {
                    case UdonNode node:
                        node.group = null;
                        break;
                    case UdonComment comment:
                        comment.group = null;
                        break;
                }
            }

            SaveNewData();
        }

        public UdonGraphElementData GetData()
        {
            return new UdonGraphElementData(UdonGraphElementType.UdonGroup, Uid, EditorJsonUtility.ToJson(_customData));
        }

        // CustomData is serialized in user assets, so we can't rename/modify/remove any of these variables 
        // ReSharper disable InconsistentNaming
        // ReSharper disable NotAccessedField.Local
        // ReSharper disable FieldCanBeMadeReadOnly.Local
        private class CustomData
        {
            public string uid;
            public Rect layout;
            public List<string> containedElements = new List<string>();
            public string title;
            public int layer;
            public Color elementTypeColor;
        }
        // ReSharper enable InconsistentNaming
        // ReSharper enable NotAccessedField.Local
        // ReSharper enable FieldCanBeMadeReadOnly.Local
    }
}
