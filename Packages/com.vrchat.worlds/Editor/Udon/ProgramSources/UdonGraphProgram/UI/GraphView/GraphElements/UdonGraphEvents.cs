using UnityEngine;
using UnityEngine.UIElements;
using VRC.Udon.Graph;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonGraphEvents : VisualElement
    {
        private readonly UdonGraph _graph;
        private UdonNodeData[] _events;
        private readonly VisualElement _list;
        private readonly Label _placeholder;

        public UdonGraphEvents(UdonGraph graph, UdonSearchManager searchManager)
        {
            _graph = graph;
            name = "UdonGraphEvents";
            
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
            
            var headerText = new Label("Events");
            leftSide.Add(headerText);
            var eventAddBtn = new Button(() =>
            {
                var screenPosition = GUIUtility.GUIToScreenPoint(_graph.lastMousePosition);
                searchManager.OpenEventSearch(screenPosition);
            })
            {
                text = "+"
            };
            eventAddBtn.AddToClassList("addEventBtn");
            header.Add(eventAddBtn);
            _list = new VisualElement();
            _list.AddToClassList("list");
            Add(_list);
            
            _placeholder = new Label("No Events Added");
            _placeholder.AddToClassList("placeholder");
            _list.Add(_placeholder);
        }

        public UdonNodeData[] Events
        {
            set
            {
                _events = value;
                var oldEvents = this.Query<Button>(null, "udonEvent").ToList();
                foreach (var e in oldEvents)
                {
                    _list.Remove(e);
                }

                if (_events.Length == 0)
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
                foreach (var e in _events)
                {
                    var eventButton = new Button(() =>
                    {
                        _graph.ClearSelection();
                        _graph.AddToSelection(_graph.GetNodeByGuid(e.uid));
                        _graph.FrameSelection();
                    })
                    {
                        name = $"jumpTo_{e.fullName}"
                    };
                    eventButton.AddToClassList("udonEvent");
                    var eventName = e.fullName.Replace("Event_", "");
                    switch (eventName)
                    {
                        case "Custom":
                        {
                            var customName =  e.nodeValues[0].Deserialize() as string;
                            if (!string.IsNullOrEmpty(customName))
                            {
                                eventButton.text = e.nodeValues[0].Deserialize() as string;
                            }
                            else
                            {
                                eventButton.text = "Custom Event";
                            }
                            eventButton.AddToClassList("customEvent");
                            _list.Insert(0, eventButton);
                            break;
                        }
                        case "OnVariableChange":
                            eventButton.text = $"{_graph.GetVariableName(e.nodeValues[0].Deserialize() as string)} Change";
                            eventButton.AddToClassList("variableChange");
                            _list.Add(eventButton);
                            break;
                        default:
                            eventButton.text = eventName;
                            _list.Add(eventButton);
                            break;
                    }
                }
            }
        }
    }
}
