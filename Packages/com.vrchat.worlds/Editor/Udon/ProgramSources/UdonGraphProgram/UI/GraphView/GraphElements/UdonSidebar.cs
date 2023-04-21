using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonSidebar : GraphElement
    {
        private readonly UdonGraph _graph;
        public readonly UdonGraphVariables GraphVariables;
        public readonly UdonGraphEvents EventsList;
        private readonly ScrollView _scrollView;
        public readonly UdonGraphGroups GroupsList;
        private readonly VisualElement _searchBlock;
        private readonly TextField _searchField;

        private GraphElement[] _searchResults;
        private int _focusedResult;
        private bool _isBorderHeld;
        private float _sidebarWidth = 234f;

        public UdonSidebar(UdonGraph graph, UdonSearchManager searchManager,  Action<VisualElement, string> editTextRequested)
        {
            _graph = graph;
            name = "UdonSidebar";

            _graph.OnMouseMoveCallback += OnGraphMouseMove;
            _graph.OnMouseUpCallback += OnGraphMouseUp;

            var scrollerPlaceholder = new VisualElement();
            scrollerPlaceholder.AddToClassList("scrollerPlaceholder");
            Add(scrollerPlaceholder);

            VisualElement draggableBorder = new VisualElement();
            draggableBorder.AddToClassList("draggableBorder");
            Add(draggableBorder);
            
            draggableBorder.RegisterCallback<MouseDownEvent>(evt =>
            {
                _isBorderHeld = true;
            });

            _searchBlock = new VisualElement();
            _searchBlock.AddToClassList("udonGraphSearch");
            Add(_searchBlock);

            _searchField = new TextField();
            _searchField.AddToClassList("udonGraphSearchInput");
            _searchBlock.Add(_searchField);

            Label searchPlaceholder = new Label("Search");
            searchPlaceholder.AddToClassList("udonGraphSearchPlaceholder");
            searchPlaceholder.SetEnabled(false);
            _searchField.Add(searchPlaceholder);

            _searchField.RegisterValueChangedCallback(e =>
            {
                string searchText = e.newValue.ToLower();
                searchPlaceholder.EnableInClassList("hidden", searchText.Length > 0);
                if (searchText.Length > 2)
                {
                    PerformSearch(searchText);
                }
            });
            _searchField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    NextSearchResult();
                    _searchField.Focus();
                }
            });

            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            Add(_scrollView);

            GraphVariables = new UdonGraphVariables(graph, searchManager, editTextRequested);
            _scrollView.Add(GraphVariables);

            EventsList = new UdonGraphEvents(graph, searchManager);
            _scrollView.Add(EventsList);
            
            GroupsList = new UdonGraphGroups(graph);
            _scrollView.Add(GroupsList);
            
            // block dragging/moving
            RegisterCallback((EventCallback<DragUpdatedEvent>) (e => e.StopPropagation()));
            RegisterCallback((EventCallback<WheelEvent>) (e => e.StopPropagation()));
            RegisterCallback((EventCallback<MouseDownEvent>) (e => e.StopPropagation()));
            RegisterCallback<KeyDownEvent>(_graph.OnKeyDown);
        }

        public void FocusSearch()
        {
            _searchField.Q("unity-text-input").Focus();
        }

        private void PerformSearch(string term)
        {
            _searchResults = null;
            var matching = _graph.graphElements.ToList().Where(i =>
            {
                if (!(i is UdonNode udonNode))
                {
                    return i.title.ToLower().Contains(term);
                }
                string nodeName = udonNode.definition.fullName.Split(' ').FirstOrDefault();
                if (nodeName != null && nodeName.ToLower()
                        .Contains(term))
                {
                    return true;
                }

                return i.title.ToLower().Contains(term);
            }).ToArray();

            if (matching.Length <= 0) return;
            _searchResults = matching;
            _graph.ClearSelection();
            foreach (var data in matching)
            {
                _graph.AddToSelection(data);
            }

            _graph.FrameSelection();
            _focusedResult = 0;
        }

        private void NextSearchResult()
        {
            if (_searchResults == null) return;
            _focusedResult += 1;
            if (_focusedResult > _searchResults.Length)
            {
                _focusedResult = 1;
            }
            _graph.ClearSelection();
            _graph.AddToSelection(_searchResults[_focusedResult - 1]);
            _graph.FrameSelection();
        }

        private void OnGraphMouseMove(object sender, MouseMoveEvent evt)
        {
            if (!_isBorderHeld) return;
            _sidebarWidth = evt.mousePosition.x + 4f;
            contentContainer.style.width = new StyleLength(_sidebarWidth);
            _scrollView.style.width = new StyleLength(_sidebarWidth - 4f);
            _searchBlock.style.width = new StyleLength(_sidebarWidth - 17f);
            GraphVariables.style.width = new StyleLength(_sidebarWidth - 17f);
            EventsList.style.width = new StyleLength(_sidebarWidth - 17f);
            GroupsList.style.width = new StyleLength(_sidebarWidth - 17f);
            _graph.OnSidebarResize.Invoke(_graph, evt);
        }

        private void OnGraphMouseUp(object sender, MouseUpEvent evt)
        {
            _isBorderHeld = false;
        }
    }
}
