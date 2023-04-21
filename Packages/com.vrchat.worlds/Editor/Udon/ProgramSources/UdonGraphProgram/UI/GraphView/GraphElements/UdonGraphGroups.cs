using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonGraphGroups : VisualElement
    {
        private readonly VisualElement _list;
        private readonly Label _placeholder;
        private UdonGroup[] _groups;

        public UdonGraphGroups(UdonGraph graph)
        {
            UdonGraph graph1 = graph;
            name = "UdonGraphGroups";
            
            AddToClassList("baseBlock");

            var header = new VisualElement(); 
            header.AddToClassList("header");
            header.RegisterCallback((MouseUpEvent e) =>
            {
                ToggleInClassList("collapsed");
            });
            Add(header);
            
            var leftSide = new VisualElement();
            header.Add(leftSide);

            var collapseIcon = new VisualElement();
            collapseIcon.AddToClassList("collapseIcon");
            leftSide.Add(collapseIcon);
            
            var headerText = new Label("Groups");
            leftSide.Add(headerText);
            var eventAddBtn = new Button(() =>
            {
                var groupRect = graph1.GetRectFromMouse();
                groupRect.x += 200;
                UdonGroup group = UdonGroup.Create("Group", groupRect, graph1);
                Undo.RecordObject(graph1.graphProgramAsset, "Add Group");
                graph1.AddElement(group);
                group.UpdateDataId();

                foreach (ISelectable item in graph1.selection)
                {
                    switch (item)
                    {
                        case UdonNode node:
                            group.AddElement(node);
                            break;
                        case UdonComment comment:
                            group.AddElement(comment);
                            break;
                    }
                }
                group.Initialize();
                graph1.SaveGraphElementData(group);
                AddGroup(group);
            })
            {
                text = "+"
            };
            eventAddBtn.AddToClassList("addEventBtn");
            header.Add(eventAddBtn);
            _list = new VisualElement();
            _list.AddToClassList("list");
            Add(_list);
            
            _placeholder = new Label("No Groups Added");
            _placeholder.AddToClassList("placeholder");
            _list.Add(_placeholder);
        }

        public void AddGroup(UdonGroup group)
        {
            if (Groups != null)
            {
                var newGroups = Groups.ToList();
                newGroups.Add(group);
                Groups = newGroups.ToArray();
            }
            else
            {
                Groups = new[] { group };
            }
        }

        public new void Clear()
        {
            Groups = Array.Empty<UdonGroup>();
        }

        private UdonGroup[] Groups
        {
            get => _groups;
            set
            {
                _groups = value;
                UpdateGroups(value);
            }
        }

        private void UpdateGroups(UdonGroup[] value)
        {
            var oldGroups = this.Query(null, "udonGroup").ToList();
            foreach (var group in oldGroups)
            {
                _list.Remove(group);
            }

            if (value.Length == 0)
            {
                if (!_list.Contains(_placeholder))
                {
                    _list.Add(_placeholder);
                }
            }
            else
            {
                if (_list.Contains(_placeholder))
                {
                    _placeholder.RemoveFromHierarchy();
                }
            }

            foreach (var group in value)
            {
                group.UpdateGraphGroups = UpdateGroupsDelegate;
                var groupBtn = new Button(() => { group.SelectGroup(); })
                {
                    name = $"jumpTo_{group.name}",
                    text = group.title
                };
                groupBtn.AddToClassList("udonGroup");
                _list.Add(groupBtn);
            }
        }

        private void UpdateGroupsDelegate()
        {
            UpdateGroups(_groups);
        }
    }
}
