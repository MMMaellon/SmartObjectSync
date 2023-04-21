using UnityEditor.Experimental.GraphView;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Udon.Graph.Interfaces;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonSearchManager
    {
        private readonly UdonGraph _view;
        private readonly UdonGraphWindow _window;

        // Search Windows
        private UdonFocusedSearchWindow _focusedSearchWindow;
        private UdonRegistrySearchWindow _registrySearchWindow;
        private UdonFullSearchWindow _fullSearchWindow;
        private UdonVariableTypeWindow _variableSearchWindow;
        private UdonPortSearchWindow _portSearchWindow;
        private UdonEventTypeWindow _eventSearchWindow;

        public UdonSearchManager(UdonGraph view, UdonGraphWindow window)
        {
            _view = view;
            _window = window;

            SetupSearchTypes();

            view.nodeCreationRequest += OnRequestNodeCreation;
        }

        private void SetupSearchTypes()
        {
            if (_registrySearchWindow == null)
                _registrySearchWindow = ScriptableObject.CreateInstance<UdonRegistrySearchWindow>();
            _registrySearchWindow.Initialize(_window, _view, this);

            if (_fullSearchWindow == null)
                _fullSearchWindow = ScriptableObject.CreateInstance<UdonFullSearchWindow>();
            _fullSearchWindow.Initialize(_window, _view);

            if (_focusedSearchWindow == null)
                _focusedSearchWindow = ScriptableObject.CreateInstance<UdonFocusedSearchWindow>();
            _focusedSearchWindow.Initialize(_window, _view);

            if (_variableSearchWindow == null)
                _variableSearchWindow = ScriptableObject.CreateInstance<UdonVariableTypeWindow>();
            _variableSearchWindow.Initialize(_window, _view);

            if (_portSearchWindow == null)
                _portSearchWindow = ScriptableObject.CreateInstance<UdonPortSearchWindow>();
            _portSearchWindow.Initialize(_window, _view);

            if (_eventSearchWindow == null)
                _eventSearchWindow = ScriptableObject.CreateInstance<UdonEventTypeWindow>();
            _eventSearchWindow.Initialize(_window, _view);
        }

        private void OnRequestNodeCreation(NodeCreationContext context)
        {
            // started on empty space
            if (context.target == null)
            {
                // If we have a node selected (but not set as context.target because that's a container for new nodes to go into), search within that node's registry
                if (Settings.SearchOnSelectedNodeRegistry && _view.selection.Count > 0 &&
                    _view.selection.First() is UdonNode)
                {
                    _focusedSearchWindow.targetRegistry = (_view.selection.First() as UdonNode)?.Registry;
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition, 360, 360),
                        _focusedSearchWindow);
                }
                else
                {
                    // Create Search Window that only searches Top-Level Registries
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition, 360, 360),
                        _registrySearchWindow);
                }
            }
            else if (context.target is UdonGraph)
            {
                // Slightly hacky method to figure out that we want a full-search window
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition, 360, 360), _fullSearchWindow);
            }
        }

        public void OpenVariableSearch(Vector2 screenMousePosition)
        {
            // offset search window to appear next to mouse
            screenMousePosition.x += 217;
            screenMousePosition.y += 0;
            SearchWindow.Open(new SearchWindowContext(screenMousePosition, 360, 360), _variableSearchWindow);
        }

        public void OpenEventSearch(Vector2 screenMousePosition)
        {
            screenMousePosition.x += 217;
            screenMousePosition.y += 0;
            SearchWindow.Open(new SearchWindowContext(screenMousePosition, 360, 360), _eventSearchWindow);
        }

        public void OpenPortSearch(Type type, Vector2 screenMousePosition, UdonPort port, Direction direction)
        {
            // offset search window to appear next to mouse
            screenMousePosition = _portSearchWindow._editorWindow.position.position + screenMousePosition;
            screenMousePosition.x += 140;
            screenMousePosition.y += 0;
            _portSearchWindow.typeToSearch = type;
            _portSearchWindow.startingPort = port;
            _portSearchWindow.direction = direction;
            SearchWindow.Open(new SearchWindowContext(screenMousePosition, 360, 360), _portSearchWindow);
        }

        private Vector2 _searchWindowPosition;

        public void QueueOpenFocusedSearch(INodeRegistry registry, Vector2 position)
        {
            _searchWindowPosition = position;
            _focusedSearchWindow.targetRegistry = registry;
            EditorApplication.update += TryOpenFocusedSearch;
        }

        private void TryOpenFocusedSearch()
        {
            if (SearchWindow.Open(new SearchWindowContext(_searchWindowPosition, 360, 360), _focusedSearchWindow))
            {
                EditorApplication.update -= TryOpenFocusedSearch;
            }
        }
    }
}