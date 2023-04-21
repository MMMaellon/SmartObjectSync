using UnityEditor.Experimental.GraphView;
using UnityGraph = UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using MenuAction = UnityEngine.UIElements.DropdownMenuAction;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon.Graph;
using VRC.Udon.Serialization;


namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public static class UdonGraphCommands
    {
        public const string Reload = "Reload";
        public const string Compile = "Compile";
    }

    public class UdonGraph : UnityGraph.GraphView
    {
        private GridBackground _background;
        private UdonSidebar _sidebar;
        private UdonGraphToolbar _toolbar;

        // copied over from Legacy.UdonGraph
        // ReSharper disable InconsistentNaming
        public UdonGraphProgramAsset graphProgramAsset;
        public UdonBehaviour udonBehaviour;
        // ReSharper enable InconsistentNaming
        
        public EventHandler<MouseMoveEvent> OnMouseMoveCallback;
        public EventHandler<MouseUpEvent> OnMouseUpCallback;
        public EventHandler<MouseMoveEvent> OnSidebarResize;

        // copied over from Legacy.UdonGraph
        // ReSharper disable once InconsistentNaming
        public UdonGraphData graphData
        {
            get => graphProgramAsset.graphData;
            private set
            {
                graphProgramAsset.graphData = value;
                EditorUtility.SetDirty(graphProgramAsset);
            }
        }
        
        public List<UdonNodeData> VariableNodes { get; private set; } = new List<UdonNodeData>();
        public ImmutableList<string> VariableNames { get; private set; }

        public bool IsReloading { get; private set; }
        
        // Tracking variables
        public Vector2 lastMousePosition;
        private readonly VisualElement mouseTipContainer;
        private readonly TextElement mouseTip;
        private readonly Vector2 mouseTipOffset = new Vector2(20, -22);
        private bool _dragging;
        private KeyCode _lastPressedKey;
        private bool _keyPressed;
        private UdonNode _selectedRootNode;
        private readonly UdonSearchManager _searchManager;
        private bool _waitingToReload;
        
        // ReSharper disable once UnusedMember.Global
        public bool IsReservedName(string n)
        {
            return n.StartsWith("__");
        }

        public UdonGraph(UdonGraphWindow window)
        {
            this.StretchToParentSize();
            SetupBackground();
            
            _searchManager = new UdonSearchManager(this, window);

            SetupToolbar();
            SetupSidebar();
            SetupZoom(0.2f, 3);
            SetupDragAndDrop();

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            mouseTipContainer = new VisualElement
            {
                name = "mouse-tip-container"
            };
            Add(mouseTipContainer);
            mouseTip = new TextElement
            {
                name = "mouse-tip",
                visible = true
            };
            SetMouseTip("");
            mouseTipContainer.Add(mouseTip);

            // This event is used to send commands from updated port fields
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);

            // Save last known mouse position for better pasting. Is there a performance hit for this?
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<KeyUpEvent>(OnKeyUp);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            
            graphViewChanged = OnViewChanged;
            serializeGraphElements = OnSerializeGraphElements;
            unserializeAndPaste = OnUnserializeAndPaste;
            canPasteSerializedData = CheckCanPaste;
            viewTransformChanged = OnViewTransformChanged;
        }

        private void OnViewTransformChanged(UnityGraph.GraphView graphView)
        {
            if(graphProgramAsset == null)
                return;
            EditorUtility.SetDirty(graphProgramAsset);
        }

        private static bool CheckCanPaste(string pasteData)
        {
            //Unity's graph api wants a boolean callback isolated from where we actually do copy pasting,
            //so we can't just do this try-catch in our Copy-Paste method and have to do it redundantly here instead
            try
            {
                //Make sure the data is valid by opening it in a try-catch, so we don't need to handle it later on
                // ReSharper disable once UnusedVariable
                UdonNodeData[] copiedNodeDataArray = JsonUtility
                    .FromJson<SerializableObjectContainer.ArrayWrapper<UdonNodeData>>(
                        UdonGraphExtensions.UnZipString(pasteData))
                    .value;
            }
            catch
            {
                //oof ouch that's not valid data
                return false;
            }

            return true;
        }

        public void Initialize(UdonGraphProgramAsset asset, UdonBehaviour behaviour)
        {
            if (graphProgramAsset != null)
            {
                SaveGraphToDisk();
            }

            graphProgramAsset = asset;
            if (behaviour != null)
            {
                udonBehaviour = behaviour;
            }
            
            graphData = new UdonGraphData(graphProgramAsset.GetGraphData())
            {
                updateOrder = graphProgramAsset.graphData.updateOrder
            };

            DoDelayedReload();
            EditorApplication.update += DelayedRestoreViewFromData;

            // When pressing ctrl-s, we save the graph
            EditorSceneManager.sceneSaved += _ => SaveGraphToDisk();
        }

        private void DelayedRestoreViewFromData()
        {
            EditorApplication.update -= DelayedRestoreViewFromData;
            //Todo: restore from saved data instead of FrameAll
            FrameAll();
        }

        public UdonNode AddNodeFromSearch(UdonNodeDefinition definition, Vector2 position)
        {
            UdonNode node = UdonNode.CreateNode(definition, this);
            AddElement(node);

            node.SetPosition(new Rect(position, Vector2.zero));
            node.Select(this, false);
            _sidebar.EventsList.Events = graphData.EventNodes;

            return node;
        }

        public void ConnectNodeTo(UdonNode node, UdonPort startingPort, Direction direction, Type typeToSearch)
        {
            // Find port to connect to
            var collection = direction == Direction.Input ? node.portsIn : node.portsOut;
            UdonPort endPort = collection.FirstOrDefault(p => p.Value.portType == typeToSearch).Value;
            // If found, add edge and serialize the connection in the programAsset
            if (endPort == null) return;
            // Important not to create and add this edge, we'll restore it below instead
            startingPort.ConnectTo(endPort);
            (startingPort.node as UdonNode)?.RestoreConnections();
            (endPort.node as UdonNode)?.RestoreConnections();
            Compile();
        }

        private void ConnectNodeToFlow(UdonNode node, Port startingPort, Direction direction, int destinationPort)
        {
            // Find port to connect to
            var collection = direction == Direction.Input ? node.portsFlowIn : node.portsFlowOut;
            UdonPort endPort = collection?[destinationPort];
            // If found, add edge and serialize the connection in the programAsset
            if (endPort == null) return;
            // Important not to create and add this edge, we'll restore it below instead
            startingPort.ConnectTo(endPort);
            (startingPort.node as UdonNode)?.RestoreConnections();
            (endPort.node as UdonNode)?.RestoreConnections();
            Compile();
        }

        public void SaveGraphToDisk()
        {
            if (graphProgramAsset == null)
                return;
            
            EditorUtility.SetDirty(graphProgramAsset);
        }

        public void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.target == this)
            {
                _keyPressed = true;
                if (evt.keyCode != KeyCode.None)
                {
                    _lastPressedKey = evt.keyCode;
                }
            }
            if (evt.target == this && evt.keyCode == KeyCode.Tab && !evt.ctrlKey)
            {
                var screenPosition = GUIUtility.GUIToScreenPoint(evt.originalMousePosition);
                nodeCreationRequest(new NodeCreationContext { screenMousePosition = screenPosition, target = this });
                evt.StopImmediatePropagation();
            }

            if (evt.keyCode == KeyCode.F && evt.ctrlKey)
            {
                _sidebar.FocusSearch();
            }

            switch (evt.keyCode)
            {
                case KeyCode.A when evt.shiftKey && selection.Count > 0:
                    AlignNodes();
                    break;
                case KeyCode.A:
                {
                    if (evt.ctrlKey)
                    {
                        // Select every graph element
                        ClearSelection();
                        foreach (var element in graphElements.ToList())
                        {
                            AddToSelection(element);
                        }
                    }

                    break;
                }
                case KeyCode.G when evt.shiftKey:
                    Undo.RecordObject(graphProgramAsset, "Changed Name");
                    graphProgramAsset.graphData.name = Guid.NewGuid().ToString();
                    break;
                case KeyCode.G:
                {
                    if (evt.target == this && selection.Count > 0)
                    {
                        CreateGroup();
                    }

                    break;
                }
            }
        }

        private void OnKeyUp(KeyUpEvent evt)
        {
            _keyPressed = false;
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            OnMouseUpCallback.Invoke(this, evt);
            if (selection.Count == 0)
            {
                OnNodeSelected(null);
            }
            if (evt.target != this || evt.button != 0 || !_keyPressed) return;
            var graphMousePosition = contentViewContainer.WorldToLocal(evt.mousePosition);
            switch (_lastPressedKey)
            {
                case KeyCode.Equals:
                    if (evt.shiftKey)
                    {
                        MakeOpNode<float, float, float>(graphMousePosition, "op_Addition", 0f, 0f);
                    }
                    else
                    {
                        MakeOpNode<float, float, bool>(graphMousePosition, "op_Equality", 0f, 0f);
                    }
                    break;
                case KeyCode.Minus:
                    MakeOpNode<float, float, float>(graphMousePosition, "op_Subtraction", 0f, 0f);
                    break;
                case KeyCode.B:
                    if (selection.Count > 0) break;
                    MakeSpecialNode(evt.shiftKey ? SpecialNodeType.Block : SpecialNodeType.Branch, graphMousePosition);
                    break;
                case KeyCode.F:
                    if (evt.shiftKey)
                    {
                        if (selection.Count == 0)
                        {
                            MakeSpecialNode(SpecialNodeType.For, graphMousePosition);
                        } else if (selection.Count == 1)
                        {
                            CreateForEachLoop(selection[0] as UdonNode);
                        }
                    }
                    break;
                case KeyCode.C:
                    if (selection.Count != 1) break;
                    if (selection[0] is UdonNode udonNode)
                    {
                        if (udonNode.definition.fullName.StartsWith("Const_"))
                        {
                            if (udonNode.definition.type != null)
                            {
                                ConvertConstantToVariable(udonNode);
                            }
                        }
                    }
                    break;
                case KeyCode.L:
                    LogSelection();
                    break;
                case KeyCode.Alpha1: 
                    MakeConstantNode(graphMousePosition, 0f);
                    break;
                case KeyCode.Alpha2:
                    CreateConstructableType<Vector2, float, Vector2>(graphMousePosition, evt.shiftKey, Vector2.zero, 0f, 0f);
                    break;
                case KeyCode.Alpha3:
                    CreateConstructableType<Vector3, float, Vector3>(graphMousePosition, evt.shiftKey, Vector3.zero, 0f, 0f, 0f);
                    break;
                case KeyCode.Alpha4:
                    CreateConstructableType<Vector4, float, Vector4>(graphMousePosition, evt.shiftKey, Vector4.zero, 0f, 0f, 0f, 0f);
                    break;
            }
        }

        public void OnHighlightFlowChanged()
        {
            if (!Settings.HighlightFlow)
            {
                if (_selectedRootNode == null) return;

                HandleFlowUnhighlight();
                return;
            }

            if (_selectedRootNode == null) return;
            HandleFlowHighlight();
        }

        public void OnNodeSelected(UdonNode node)
        {
            if (node == _selectedRootNode) return;
            _selectedRootNode = node;
            if (node == null)
            {
                HandleFlowUnhighlight();
                return;
            }
            if (!Settings.HighlightFlow) return;
            HandleFlowHighlight();
        }

        private void HandleFlowUnhighlight()
        {
            var allNodes = nodes.ToList();
            var allEdges = edges.ToList();
            foreach (var singleNode in allNodes)
            {
                singleNode.RemoveFromClassList("highlighted");
                singleNode.RemoveFromClassList("muted");
            }
        
            foreach (var edge in allEdges)
            {
                edge.RemoveFromClassList("highlighted");
                edge.RemoveFromClassList("muted");
            }
        }

        private void HandleFlowHighlight()
        {
            var allNodes = nodes.ToList();
            var allEdges = edges.ToList();
            if (_selectedRootNode.portsFlowIn == null && _selectedRootNode.portsFlowOut == null)
            {
                _selectedRootNode = null;
                foreach (var singleNode in allNodes)
                {
                    singleNode.RemoveFromClassList("highlighted");
                    singleNode.RemoveFromClassList("muted");
                }
            
                foreach (var edge in allEdges)
                {
                    edge.RemoveFromClassList("highlighted");
                    edge.RemoveFromClassList("muted");
                }

                return;
            }
            if(_selectedRootNode.portsFlowIn == null) return;
            if (_selectedRootNode.portsFlowIn != null && !_selectedRootNode.portsFlowIn.Any(port => port.connected) && !_selectedRootNode.portsFlowOut.Any(port => port.connected)) return;
            
            var highlightedNodes = new HashSet<string>();
            var highlightedEdges = new HashSet<string>();
            highlightedNodes.Add(_selectedRootNode.GetUid());
            allEdges = edges.ToList().Where(edge => edge.input.portType == null && edge.output.portType == null).ToList();
            if (_selectedRootNode.portsFlowIn.Any(port => port.connected))
            {
                var inputEdges = allEdges.Where(edge => edge.input.node.GetUid() == _selectedRootNode.uid).ToList();
                foreach (var inputEdge in inputEdges)
                {
                    highlightedEdges.Add(inputEdge.GetUid());
                }
                RecursivelyParseFlow(inputEdges, Direction.Input, allEdges, ref highlightedNodes, ref highlightedEdges);
            }

            if (_selectedRootNode.portsFlowOut.Any(port => port.connected))
            {
                var outputEdges = allEdges.Where(edge => edge.output.node.GetUid() == _selectedRootNode.uid).ToList();
                foreach (var outputEdge in outputEdges)
                {
                    highlightedEdges.Add(outputEdge.GetUid());
                }
                RecursivelyParseFlow(outputEdges, Direction.Output, allEdges, ref highlightedNodes,
                    ref highlightedEdges);
            }

            allNodes = nodes.ToList();
            foreach (var node in allNodes)
            {
                var isHighlighted = highlightedNodes.Contains(node.GetUid());
                node.EnableInClassList("highlighted", isHighlighted);
                node.EnableInClassList("muted", !isHighlighted);
            }

            allEdges = edges.ToList();
            foreach (var edge in allEdges)
            {
                var isHighlighted = highlightedEdges.Contains(edge.GetUid());
                edge.EnableInClassList("highlighted", isHighlighted);
                edge.EnableInClassList("muted", !isHighlighted);
            }
        }

        private static void RecursivelyParseFlow(List<Edge> nextEdges, Direction direction, List<Edge> allEdges,
            ref HashSet<string> highlightedNodeUIDs, ref HashSet<string> highlightedEdgeUIDs)
        {
            var nextEdge = nextEdges[0];
            highlightedEdgeUIDs.Add(nextEdge.GetUid());
            var foundEdges = new List<Edge>();
    
            switch (direction)
            {
                case Direction.Input:
                {
                    highlightedNodeUIDs.Add(nextEdge.output.node.GetUid());
                    foreach (var edge in allEdges)
                    {
                        if (edge.input.node.GetUid() == nextEdge.output.node.GetUid())
                        {
                            foundEdges.Add(edge);
                        }
                    }

                    break;
                }
                case Direction.Output:
                {
                    highlightedNodeUIDs.Add(nextEdge.input.node.GetUid());
                    foreach (var edge in allEdges)
                    {
                        if (edge.output.node.GetUid() == nextEdge.input.node.GetUid())
                        {
                            foundEdges.Add(edge);
                        }
                    }

                    break;
                }
            }

            nextEdges.RemoveAt(0);

            if (foundEdges.Count > 0)
            {
                RecursivelyParseFlow(foundEdges.Concat(nextEdges).ToList(), direction, allEdges, ref highlightedNodeUIDs,
                    ref highlightedEdgeUIDs);
                return;
            }

            if (nextEdges.Count > 0)
            {
                RecursivelyParseFlow(nextEdges, direction, allEdges, ref highlightedNodeUIDs,
                    ref highlightedEdgeUIDs);
            }
        }
        
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is UnityGraph.GraphView || evt.target is UdonNode)
            {
                var selectedItems = selection.Where(i=>i is UdonNode || i is UdonComment).ToList();
                var addedGraphActions = false;
                if (evt.target is UdonNode selectedNode)
                {
                    if (selectedNode.portsOut.Count > 0)
                    {
                        contentViewContainer.WorldToLocal(evt.mousePosition);
                        evt.menu.AppendAction("Log Value [L]", m =>
                        {
                            LogSelection();
                        });
                    }

                    if (selectedNode.data.fullName.StartsWith("Const_"))
                    {
                        if (selectedNode.definition.type != null)
                        {
                            evt.menu.AppendAction("Covert To Variable [C]",
                                m => { ConvertConstantToVariable(selectedNode); });
                        }
                    }

                    if (selectedNode.portsOut.Count == 1 && selectedNode.portsOut[0].portType.IsArray)
                    {
                        evt.menu.AppendAction("Generate ForEach Loop [Shift+F]", m =>
                        {
                            CreateForEachLoop(selectedNode);
                        });
                    }

                    addedGraphActions = true;
                }
                if (selectedItems.Count > 0)
                {
                    var rootNode = evt.target as UdonNode;
                    evt.menu.AppendAction("Align Nodes [Shift+A]", m =>
                    {
                        AlignNodes(rootNode);
                    });
                    addedGraphActions = true;
                }

                if (addedGraphActions)
                {
                    evt.menu.AppendSeparator();
                }
                
                // Create a Group, enclosing any selected nodes
                evt.menu.AppendAction("Create Group", (m) =>
                {
                    CreateGroup();
                }, MenuAction.AlwaysEnabled);
                if (selectedItems.Count > 0)
                {
                    evt.menu.AppendAction("Remove From Group", (m) =>
                    {
                        Undo.RecordObject(graphProgramAsset, "Remove Items from Group");
                        int count = selectedItems.Count;
                        for (int i = count - 1; i >=0; i--)
                        {
                            switch (selectedItems.ElementAt(i))
                            {
                                case UdonNode node:
                                {
                                    node.group?.RemoveElement(node);
                                    break;
                                }
                                case UdonComment comment:
                                {
                                    comment.group?.RemoveElement(comment);
                                    break;
                                }
                            }
                        }
                        
                    }, MenuAction.AlwaysEnabled);
                }

                // Create a Comment
                evt.menu.AppendAction("Create Comment", m =>
                {
                    UdonComment comment = UdonComment.Create("Comment", GetRectFromMouse(), this);
                    Undo.RecordObject(graphProgramAsset, "Add Comment");
                    AddElement(comment);
                }, MenuAction.AlwaysEnabled);

                evt.menu.AppendSeparator();
            }

            base.BuildContextualMenu(evt);
        }

        public Rect GetRectFromMouse()
        {
            return new Rect(contentViewContainer.WorldToLocal(lastMousePosition), Vector2.zero);
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            lastMousePosition = evt.mousePosition;
            MoveMouseTip(lastMousePosition);
            OnMouseMoveCallback.Invoke(this, evt);
        }

        private void MoveMouseTip(Vector2 position)
        {
            if (!mouseTipContainer.visible) return;
            var newLayout = mouseTipContainer.layout;
            newLayout.position = position + mouseTipOffset;
            mouseTipContainer.transform.position = newLayout.position;
        }

        public bool IsDuplicateEventNode(string fullName, string uid = "")
        {
            if (fullName.StartsWith("Event_") &&
                fullName != "Event_Custom"
            )
            {
                if (this.Query(fullName).ToList().Count <= 0) return false;
                Debug.LogWarning(
                    $"Can't create more than one {fullName} node, try managing your flow with a Block node instead!");
                return true;
            }
            if(fullName.StartsWith(Common.VariableChangedEvent.EVENT_PREFIX))
            {
                bool isDuplicate = graphData.EventNodes.Any(d =>
                    d.nodeValues.Length > 0 && d.nodeValues[0].Deserialize().ToString() == uid);
                if (isDuplicate)
                {
                    Debug.LogWarning(
                        $"Can't create more than one Change Event for {GetVariableName(uid)}, try managing your flow with a Block node instead!");
                }
                return isDuplicate;
            }

            return false;
        }

        private string OnSerializeGraphElements(IEnumerable<GraphElement> selectedElements)
        {
            Bounds bounds = new Bounds();
            bool startedBounds = false;
            List<UdonNodeData> nodeData = new List<UdonNodeData>();
            List<UdonNodeData> variables = new List<UdonNodeData>();
            foreach (var item in selectedElements)
            {
                // Only serializing UdonNode for now
                if (!(item is UdonNode node)) continue;
                // Calculate bounding box to enclose all items
                if (!startedBounds)
                {
                    bounds = new Bounds(node.data.position, Vector3.zero);
                    startedBounds = true;
                }
                else
                {
                    bounds.Encapsulate(node.data.position);
                }

                // Handle Get/Set Variables
                if (node.data.fullName == "Get_Variable" || node.data.fullName == "Set_Variable" || node.data.fullName == "Set_ReturnValue")
                {
                    // make old-school get-variable node data from existing variable
                    var targetUid = node.data.nodeValues[0].Deserialize();
                    var matchingNode = VariableNodes.First(v => v.uid == (string)targetUid);
                    if (!variables.Contains(matchingNode))
                    {
                        variables.Add(matchingNode);
                    }
                }

                nodeData.Add(new UdonNodeData(node.data));
            }

            // Add variables at beginning of list so they get created first
            nodeData.InsertRange(0, variables);

            // Go through each item and offset its position by the center of the group (normalizes the coordinates around 0,0)
            var offset = new Vector2(bounds.center.x, bounds.center.y);
            foreach (UdonNodeData data in nodeData)
            {
                data.position -= offset;
            }

            string result = UdonGraphExtensions.ZipString(JsonUtility.ToJson(
                new SerializableObjectContainer.ArrayWrapper<UdonNodeData>(nodeData.ToArray())));

            return result;
        }

        private void OnUnserializeAndPaste(string operationName, string pasteData)
        {
            ClearSelection();

            UdonNodeData[] copiedNodeDataArray;
            // Note: CheckCanPaste already does this check but it doesn't cost much to do it twice
            try
            {
                copiedNodeDataArray = JsonUtility
                    .FromJson<SerializableObjectContainer.ArrayWrapper<UdonNodeData>>(
                        UdonGraphExtensions.UnZipString(pasteData))
                    .value;
            }
            catch
            {
                //oof ouch that's not valid data
                return;
            }

            var copiedNodeDataList = new List<UdonNodeData>();
            // Add new variables if needed
            foreach (UdonNodeData nodeData in copiedNodeDataArray)
            {
                if (nodeData.fullName.StartsWith("Variable_"))
                {
                    if (!graphData.nodes.Exists(n => n.uid == nodeData.uid))
                    {
                        // set graph to this one in case it was pasted from somewhere else
                        nodeData.SetGraph(graphData);
                        
                        // check for conflicting variable names
                        int nameIndex = (int)UdonParameterProperty.ValueIndices.Name;
                        string varName = (string)nodeData.nodeValues[nameIndex].Deserialize();
                        if (VariableNames.Contains(varName))
                        {
                            // if we already have a variable with that name, find a new name and serialize it into the data
                            varName = GetUnusedVariableNameLike(varName);
                            nodeData.nodeValues[nameIndex] =
                                SerializableObjectContainer.Serialize(varName);
                        }

                        _sidebar.GraphVariables.AddFromData(nodeData);
                        graphData.nodes.Add(nodeData);
                    }
                }
                else if(IsDuplicateEventNode(nodeData.fullName))
                {
                    // don't add duplicate event nodes
                }
                else
                {
                    copiedNodeDataList.Add(nodeData);
                }
            }

            // Remove duplicate events
            RefreshVariables(false);

            // copy modified list back to array
            copiedNodeDataArray = copiedNodeDataList.ToArray();

            IsReloading = true;
            var graphMousePosition = GetRectFromMouse().position;
            List<UdonNode> pastedNodes = new List<UdonNode>();
            Dictionary<string, string> uidMap = new Dictionary<string, string>();
            UdonNodeData[] newNodeDataArray = new UdonNodeData[copiedNodeDataArray.Length];

            for (int i = 0; i < copiedNodeDataArray.Length; i++)
            {
                UdonNodeData nodeData = copiedNodeDataArray[i];
                newNodeDataArray[i] = new UdonNodeData(graphData, nodeData.fullName)
                {
                    position = nodeData.position + graphMousePosition,
                    uid = Guid.NewGuid().ToString(),
                    nodeUIDs = new string[nodeData.nodeUIDs.Length],
                    nodeValues = nodeData.nodeValues,
                    flowUIDs = new string[nodeData.flowUIDs.Length]
                };

                uidMap.Add(nodeData.uid, newNodeDataArray[i].uid);
            }

            for (int i = 0; i < copiedNodeDataArray.Length; i++)
            {
                UdonNodeData nodeData = copiedNodeDataArray[i];
                UdonNodeData newNodeData = newNodeDataArray[i];
                
                // Set the new node to point at this graph if it came from a different one
                newNodeData.SetGraph(graphData);

                for (int j = 0; j < newNodeData.nodeUIDs.Length; j++)
                {
                    if (uidMap.ContainsKey(nodeData.nodeUIDs[j].Split('|')[0]))
                    {
                        newNodeData.nodeUIDs[j] = uidMap[nodeData.nodeUIDs[j].Split('|')[0]];
                    }
                }

                for (int j = 0; j < newNodeData.flowUIDs.Length; j++)
                {
                    if (uidMap.ContainsKey(nodeData.flowUIDs[j].Split('|')[0]))
                    {
                        newNodeData.flowUIDs[j] = uidMap[nodeData.flowUIDs[j].Split('|')[0]];
                    }
                }

                UdonNode udonNode = UdonNode.CreateNode(newNodeData, this);
                if (udonNode != null)
                {
                    graphData.nodes.Add(newNodeData);
                    AddElement(udonNode);
                    pastedNodes.Add(udonNode);
                }
            }

            // Select all newly-pasted nodes after reload
            foreach (var item in pastedNodes)
            {
                item.RestoreConnections();
                item.BringToFront();
                AddToSelection(item);
            }

            IsReloading = false;
            Compile();
        }

        // This is needed to properly clear the selection in some cases (like deleting a stack node) even though it doesn't appear to do anything
        // ReSharper disable once RedundantOverriddenMember
        public override void ClearSelection()
        {
            base.ClearSelection();
        }

        public static void MarkSceneDirty()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        private GraphViewChange OnViewChanged(GraphViewChange changes)
        {
            bool dirty = false;
            bool needsVariableRefresh = false;
            // Remove node from Data when removed from Graph
            if (!IsReloading && changes.elementsToRemove != null && changes.elementsToRemove.Count > 0)
            {

                foreach (var element in changes.elementsToRemove)
                {
                    switch (element)
                    {
                        case UdonNode node:
                        {
                            var nodeData = node.data;
                            RemoveNodeAndData(nodeData);
                            continue;
                        }
                        case Edge _:
                            Undo.RecordObject(graphProgramAsset, $"delete-{element.name}");
                            continue;
                        case UdonParameterField field:
                        {
                            needsVariableRefresh = true;
                        
                            var pField = field;
                            if (graphData.nodes.Contains(pField.Data))
                            {
                                RemoveNodeAndData(pField.Data);
                            }

                            break;
                        }
                        case IUdonGraphElementDataProvider dataProvider:
                        {
                            Undo.RecordObject(graphProgramAsset, $"delete-{element.name}");
                            var provider = dataProvider;
                            DeleteGraphElementData(provider, false);
                            _sidebar.Reload();
                            RemoveElement(element);
                            break;
                        }
                    }
                }

                ClearSelection();
                dirty = true;
            }

            _sidebar.EventsList.Events = graphData.EventNodes;

            if (dirty)
            {
                MarkSceneDirty();
                SaveGraphToDisk();
            }

            if (needsVariableRefresh)
            {
                RefreshVariables();
            }

            return changes;
        }

        // ReSharper disable once UnusedMember.Global
        public void DoDelayedCompile()
        {
            EditorApplication.update += DelayedCompile;
        }

        private void DelayedCompile()
        {
            EditorApplication.update -= DelayedCompile;
            graphProgramAsset.RefreshProgram();
        }
        
        private void DoDelayedReload()
        {
            if (!_waitingToReload && !IsReloading)
            {
                _waitingToReload = true;
                EditorApplication.update += DelayedReload;
            }
        }

        private void DelayedReload()
        {
            _waitingToReload = false;
            EditorApplication.update -= DelayedReload;
            Reload();
        }

        private void SetupBackground()
        {
            _background = new GridBackground
            {
                name = "bg"
            };
            Insert(0, _background);
            _background.StretchToParentSize();
        }

        private void SetupSidebar()
        {
            _sidebar = new UdonSidebar(this, _searchManager, SidebarEditVariableName);
            Add(_sidebar);  
        }

        private void SetupToolbar()
        {
            _toolbar = new UdonGraphToolbar(this);
            Add(_toolbar);
        }

        private void SidebarEditVariableName(VisualElement v, string newValue)
        {
            UdonParameterField field = (UdonParameterField) v;
            Undo.RecordObject(graphProgramAsset, "Rename Variable");
            
            // Sanitize value for variable name
            string newVariableName = newValue.SanitizeVariableName();
            newVariableName = GetUnusedVariableNameLike(newVariableName);
            field.Data.nodeValues[(int)UdonParameterProperty.ValueIndices.Name] = SerializableObjectContainer.Serialize(newVariableName);
            field.text = newVariableName;
            
            // Find all nodes that are getters/setters for this variable
            // Change their title text by hand
            nodes.ForEach(node =>
            {
                UdonNode udonNode = (UdonNode) node;
                if (udonNode != null && udonNode.IsVariableNode)
                {
                    udonNode.RefreshTitle();
                }
            });
            
            RefreshVariables();
        }

        public void OpenPortSearch(Type type, Vector2 position, UdonPort output, Direction direction)
        {
            _searchManager.OpenPortSearch(type, position, output, direction);
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            switch (evt.commandName)
            {
                case UdonGraphCommands.Reload:
                    DoDelayedReload();
                    break;
                case UdonGraphCommands.Compile:
                    Compile();
                    break;
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var result = ports.ToList().Where(
                    port => port.direction != startPort.direction
                    && port.node != startPort.node
                    && port.portType.IsReallyAssignableFrom(startPort.portType)
                    && (port.capacity == Port.Capacity.Multi || !port.connections.Any())
                ).ToList();
            return result;
        }
        
        private readonly StyleSheet neonStyle = (StyleSheet) Resources.Load("UdonGraphNeonStyle");


        public void Reload()
        {
            if (IsReloading || (graphProgramAsset != null && !string.IsNullOrWhiteSpace(graphProgramAsset.AssemblyError))) return;
            
            IsReloading = true;

            if (Settings.UseNeonStyle && !styleSheets.Contains(neonStyle))
            {
                styleSheets.Add(neonStyle);
            }
            else if (!Settings.UseNeonStyle && !styleSheets.Contains(neonStyle))
            {
                styleSheets.Remove(neonStyle);
            }
            Undo.undoRedoPerformed -=
                OnUndoRedo; //Remove old handler if present to prevent duplicates, doesn't cause errors if not present
            Undo.undoRedoPerformed += OnUndoRedo;

            // Clear out Blackboard here
            _sidebar.GraphVariables.Clear();
            
            // Clear out Udon Groups here
            _sidebar.GroupsList.Clear();
            
            // clear existing elements, probably need to update to only clear nodes and edges
            DeleteElements(graphElements.ToList());

            RefreshVariables(false);

            List<UdonNodeData> nodesToDelete = new List<UdonNodeData>();
            // add all nodes to graph
            // This can't be a foreach loop because the collection is dynamically modified
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < graphData.nodes.Count; i++)
            {
                UdonNodeData nodeData = graphData.nodes[i];
                // Check for Node type - create nodes, separate out Variables
                if (nodeData.fullName.StartsWithCached("Variable_"))
                {
                    _sidebar.GraphVariables.AddFromData(nodeData);
                }
                else if (nodeData.fullName.StartsWithCached("Comment"))
                {
                    // one way conversion from Comment Node > Comment Group
                    var commentString = nodeData.nodeValues[0].Deserialize();
                    if (commentString != null)
                    {
                        var comment = UdonComment.Create((string)commentString,
                            new Rect(nodeData.position, Vector2.zero), this);
                        AddElement(comment);
                        SaveGraphElementData(comment);
                    }

                    // Remove from data, no longer a node
                    nodesToDelete.Add(nodeData);
                }
                else
                {
                    try
                    {
                        UdonNode udonNode = UdonNode.CreateNode(nodeData, this);
                        AddElement(udonNode);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error Loading Node {nodeData.fullName} : {e.Message}");
                        nodesToDelete.Add(nodeData);
                    }
                }
            }

            // Delete old comments and data that could not be turned into an UdonNode
            foreach (UdonNodeData nodeData in nodesToDelete)
            {
                if (graphData.nodes.Remove(nodeData))
                {
                    Debug.Log($"removed node {nodeData.fullName}");
                }
            }

            // reconnect nodes
            nodes.ForEach((genericNode) =>
            {
                UdonNode udonNode = (UdonNode)genericNode;
                udonNode.RestoreConnections();
            });
            
            // Add all Graph Elements
            if (graphProgramAsset.graphElementData != null)
            {
                var orderedElements = graphProgramAsset.graphElementData.ToList().OrderByDescending(e => e.type);
                foreach (var elementData in orderedElements)
                {
                    GraphElement element = RestoreElementFromData(elementData);
                    if (element == null) continue;
                    AddElement(element);
                    if (!(element is UdonGroup group)) continue;
                    group.Initialize();
                    _sidebar.GroupsList.AddGroup(group);
                }
            }

            IsReloading = false;
            Compile();
        }

        // TODO: create generic to restore any supported element from UdonGraphElementData?
        private GraphElement RestoreElementFromData(UdonGraphElementData data)
        {
            switch (data.type)
            {
                case UdonGraphElementType.GraphElement:
                    {
                        return null;
                    }

                case UdonGraphElementType.UdonGroup:
                    {
                        return UdonGroup.Create(data, this);
                    }

                case UdonGraphElementType.UdonComment:
                    {
                        return UdonComment.Create(data, this);
                    }
                case UdonGraphElementType.VariablesWindow:
                    {
                        _sidebar.GraphVariables.LoadData(data);
                        return null;
                    }
                case UdonGraphElementType.UdonStackNode:
                default:
                    return null;
            }
        }

        private void OnUndoRedo()
        {
            Reload();
        }

        public void RefreshVariables(bool recompile = true)
        {
            // we want internal variables at the end of the list so they can be trivially filtered out
            VariableNodes = graphData.nodes
                .Where(n => n.fullName.StartsWithCached("Variable_"))
                .Where(n => n.nodeValues.Length > 1 && n.nodeValues[1] != null)
                .OrderBy(n => ((string)n.nodeValues[1].Deserialize()).StartsWith("__"))
                .ToList();
            VariableNames = ImmutableList.Create(
                VariableNodes.Select(s => (string) s.nodeValues[1].Deserialize()).ToArray()
            );

            // Refresh variable options in popup
            nodes.ForEach(node =>
            {
                if (node is UdonNode udonNode && udonNode.IsVariableNode)
                {
                    udonNode.RefreshVariablePopup();
                }
            });
            
            // We usually want to compile after a Refresh
            if(recompile)
                Compile();
            DoDelayedReload();
        }

        // Returns UID of newly created variable
        public string AddNewVariable(string typeName = "Variable_SystemString", string variableName = "",
            bool isPublic = false)
        {
            // Figure out unique variable name to use
            string newVariableName = string.IsNullOrEmpty(variableName) ? "newVariable" : variableName;
            newVariableName = GetUnusedVariableNameLike(newVariableName);

            string newVarUid = Guid.NewGuid().ToString();
            UdonNodeData newNodeData = new UdonNodeData(graphData, typeName)
            {
                uid = newVarUid,
                nodeUIDs = new string[5],
                nodeValues = new[]
                                {
                    SerializableObjectContainer.Serialize(default),
                    SerializableObjectContainer.Serialize(newVariableName, typeof(string)),
                    SerializableObjectContainer.Serialize(isPublic, typeof(bool)),
                    SerializableObjectContainer.Serialize(false, typeof(bool)),
                    SerializableObjectContainer.Serialize("none", typeof(string))
                },
                position = Vector2.zero
            };

            graphData.nodes.Add(newNodeData);
            _sidebar.GraphVariables.AddFromData(newNodeData);
            RefreshVariables();
            return newVarUid;
        }

        public void RemoveNodeAndData(UdonNodeData nodeData)
        {
            Undo.RecordObject(graphProgramAsset, $"Removing {nodeData.fullName}");

            if (nodeData.fullName.StartsWithCached("Variable_"))
            {
                var allVariableNodes = new HashSet<Node>();
                // Find all get/set variable nodes that reference this node
                nodes.ForEach(graphNode =>
                {
                    if (!(graphNode is UdonNode udonNode) || !udonNode.IsVariableNode) return;
                    // Get variable uid and recursively remove all nodes that refer to it
                    var values = udonNode.data.nodeValues[0].stringValue.Split('|');
                    if (values.Length <= 1) return;
                    string targetVariable = values[1];
                    if (string.Compare(targetVariable, nodeData.uid, StringComparison.Ordinal) != 0) return;
                    // We have a match! Delete this node
                    allVariableNodes.Add(graphNode);
                    RemoveNodeAndData(udonNode.data);
                });

                // remove each edge connected to a Get/Set Variable node which will be deleted
                edges.ForEach(edge =>
                {
                    if (!allVariableNodes.Contains(edge.input.node) &&
                        !allVariableNodes.Contains(edge.output.node)) return;
                    (edge.output as UdonPort)?.Disconnect(edge);
                    (edge.input as UdonPort)?.Disconnect(edge);
                    RemoveElement(edge);
                });
                
                // remove from existing blackboard
                _sidebar.GraphVariables.RemoveByID(nodeData.uid);
                RefreshVariables();
            }
            
            UdonNode node = (UdonNode)GetNodeByGuid(nodeData.uid);
            node?.RemoveFromHierarchy();

            if (graphData.nodes.Contains(nodeData))
            {
                graphData.nodes.Remove(nodeData);
            }
        }

        private void Compile()
        {
            UdonEditorManager.Instance.QueueAndRefreshProgram(graphProgramAsset);
            _sidebar.EventsList.Events = graphData.EventNodes;
        }

        private bool ShouldUpdateAsset => !IsReloading && graphProgramAsset != null;

        private readonly HashSet<UdonGraphElementType> singleElementTypes = new HashSet<UdonGraphElementType>
        {
            UdonGraphElementType.VariablesWindow
        };
        
        public void SaveGraphElementData(IUdonGraphElementDataProvider provider)
        {
            if (!ShouldUpdateAsset) return;
            UdonGraphElementData newData = provider.GetData();
            if (graphProgramAsset.graphElementData == null)
            {
                graphProgramAsset.graphElementData = Array.Empty<UdonGraphElementData>();
            }

            // Some elements like the variables window should only ever have one entry, so find by type
            int index = singleElementTypes.Contains(newData.type) ? Array.FindIndex(graphProgramAsset.graphElementData, e => e.type == newData.type) : 
                Array.FindIndex(graphProgramAsset.graphElementData, e => e.uid == newData.uid);
            if (index > -1)
            {
                // Update
                graphProgramAsset.graphElementData[index] = newData;
            }
            else
            {
                // Add
                int arrayLength = graphProgramAsset.graphElementData.Length;
                Array.Resize(ref graphProgramAsset.graphElementData, arrayLength+1);
                graphProgramAsset.graphElementData[arrayLength] = newData;
            }
            SaveGraphToDisk();
        }

        private void DeleteGraphElementData(IUdonGraphElementDataProvider provider, bool save = true)
        {
            int index = Array.FindIndex(graphProgramAsset.graphElementData, e => e.uid == provider.GetData().uid);
            // remove if found
            if (index > -1)
            {
                graphProgramAsset.graphElementData = graphProgramAsset.graphElementData.Where((source, i) => i != index).ToArray();
            }

            if (save)
            {
                SaveGraphToDisk();
            }
        }

        #region Drag and Drop Support

        private void SetupDragAndDrop()
        {
            RegisterCallback<DragEnterEvent>(OnDragEnter);
            RegisterCallback<DragPerformEvent>(OnDragPerform, TrickleDown.TrickleDown);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragExitedEvent>(OnDragExited);
            RegisterCallback<DragLeaveEvent>(e=>OnDragExited(null));
        }

        private void OnDragEnter(DragEnterEvent e)
        {
            OnDragEnter(e.mousePosition, e.ctrlKey, e.altKey);
        }

        private void OnDragEnter(Vector2 mousePosition, bool ctrlKey, bool altKey)
        {
            MoveMouseTip(mousePosition);

            var dragData = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            _dragging = false;

            if (dragData != null)
            {
                // Handle drag from exposed parameter view
                if (dragData.OfType<UdonParameterField>().Any())
                {
                    _dragging = true;
                    string tip = "Get Variable\n+Ctrl: Set Variable\n+Alt: On Var Change";
                    if (ctrlKey)
                    {
                        tip = "Set Variable";
                    } else if (altKey)
                    {
                        tip = "On Variable Changed";
                    }
                    SetMouseTip(tip);
                }
            }

            if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] != null)
            {
                var target = DragAndDrop.objectReferences[0];
                switch (target)
                {
                    case GameObject _:
                    case Component _:
                    {
                        string type = GetDefinitionNameForType(target.GetType());
                        if (UdonEditorManager.Instance.GetNodeDefinition(type) != null)
                        {
                            _dragging = true;
                        }
                        break;
                    }
                }
            }

            if (_dragging)
            {
                DragAndDrop.visualMode = ctrlKey ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Copy;
            }
        }

        private void OnDragUpdated(DragUpdatedEvent e)
        {
            if (_dragging)
            {
                MoveMouseTip(e.mousePosition);
                DragAndDrop.visualMode = e.ctrlKey ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Copy;
            }
            else
            {
                OnDragEnter(e.mousePosition, e.ctrlKey, e.altKey);
            }
        }

        private void OnDragPerform(DragPerformEvent e)
        {
            if (!_dragging) return;
            var graphMousePosition = contentViewContainer.WorldToLocal(e.mousePosition);

            if (DragAndDrop.GetGenericData("DragSelection") is List<ISelectable> draggedVariables)
            {
                // Handle Drop of Variables
                var parameters = draggedVariables.OfType<UdonParameterField>();
                //Enumerate here to avoid multiple-enumeration later
                var udonParameterFields = parameters as UdonParameterField[] ?? parameters.ToArray();
                if (udonParameterFields.Any())
                {
                    RefreshVariables(false);
                    VariableNodeType nodeType = VariableNodeType.Getter;
                    if (e.ctrlKey) nodeType = VariableNodeType.Setter;
                    else if (e.altKey) nodeType = VariableNodeType.Change;
                    foreach (var parameter in udonParameterFields)
                    {
                        UdonNode udonNode = MakeVariableNode(parameter.Data.uid, graphMousePosition, nodeType);
                        if (udonNode != null)
                        {
                            AddElement(udonNode);
                        }
                    }
                    RefreshVariables();
                }
            }

            // Handle Drop of single GameObjects and Assets
            if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] != null)
            {
                var target = DragAndDrop.objectReferences[0];
                switch (target)
                {
                    case Component c:
                        SetupDraggedObject(c, graphMousePosition);
                        break;

                    case GameObject g:
                        SetupDraggedObject(g, graphMousePosition);
                        break;
                }
            }

            _dragging = false;
        }

        private void OnDragExited(DragExitedEvent e)
        {
            SetMouseTip("");
            _dragging = false;
        }

        #endregion
        public enum VariableNodeType
        {
            Getter,
            Setter,
            Return,
            Change,
        }

        private enum SpecialNodeType
        {
            Branch,
            Block,
            For
        }

        private static readonly Dictionary<SpecialNodeType, string> SpecialNodesMap =
            new Dictionary<SpecialNodeType, string>
            {
                { SpecialNodeType.Branch, "Branch" },
                { SpecialNodeType.Block, "Block" },
                { SpecialNodeType.For, "For" }
            };
        
        public string GetVariableName(string uid)
        {
            var targetNode = VariableNodes.FirstOrDefault(n => n.uid == uid);
            if (VariableNodes.Count == 0)
            {
                return "";
            }
            try
            {
                if (targetNode != null) return targetNode.nodeValues[1].Deserialize() as string;
            }
            catch (Exception e)
            {
                Debug.LogError($"Couldn't find variable name for {uid}: {e.Message}");
                return "";
            }
            return "";
        }

        public UdonNode MakeVariableNode(string selectedUid, Vector2 graphMousePosition, VariableNodeType nodeType)
        {
            string definitionName = "";
            switch (nodeType)
            {
                case VariableNodeType.Getter:
                    definitionName = "Get_Variable";
                    break;
                case VariableNodeType.Setter:
                    definitionName = "Set_Variable";
                    break;
                case VariableNodeType.Return:
                    definitionName = "Set_ReturnValue";
                    break;
                case VariableNodeType.Change:
                    definitionName = "Event_OnVariableChange";
                    break;
            }

            if (nodeType == VariableNodeType.Change)
            {
                string variableName = GetVariableName(selectedUid);
                if (!string.IsNullOrWhiteSpace(variableName))
                {
                    string eventName = UdonGraphExtensions.GetVariableChangeEventName(variableName);
                    if (IsDuplicateEventNode(eventName, selectedUid))
                    {
                        return null;
                    }
                }
            }
            
            Undo.RecordObject(graphProgramAsset, "Add Variable");

            var definition = UdonEditorManager.Instance.GetNodeDefinition(definitionName);
            var nodeData = graphData.AddNode(definition.fullName);
            nodeData.nodeValues = new SerializableObjectContainer[2];
            nodeData.nodeUIDs = new string[1];
            nodeData.nodeValues[0] = SerializableObjectContainer.Serialize(selectedUid);
            nodeData.position = graphMousePosition;

            var udonNode = UdonNode.CreateNode(nodeData, this);
            return udonNode;
        }

        private UdonNode MakeSpecialNode(SpecialNodeType nodeType, Vector2 position, SerializableObjectContainer[] initialNodeValues = null)
        {
            var identifier = SpecialNodesMap[nodeType];
            Undo.RecordObject(graphProgramAsset, $"Add {identifier} Node");
            
            var definition = UdonEditorManager.Instance.GetNodeDefinition(identifier);
            var nodeData = graphData.AddNode(definition.fullName);
            nodeData.nodeValues = initialNodeValues ?? new[] { SerializableObjectContainer.Serialize(null), SerializableObjectContainer.Serialize(null), SerializableObjectContainer.Serialize(null) };
            nodeData.position = position;
            var udonNode = UdonNode.CreateNode(nodeData, this);
            AddElement(udonNode);
            return udonNode;
        }

        private void MakeConstantNode<T>(Vector2 position, params T[] values)
        {
            var identifier = typeof(T).FullName?.Replace(".", "");
            Undo.RecordObject(graphProgramAsset, $"Add {identifier} Node");
            
            var definition = UdonEditorManager.Instance.GetNodeDefinition($"Const_{identifier}");
            var nodeData = graphData.AddNode(definition.fullName);
            nodeData.nodeValues = new SerializableObjectContainer[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                nodeData.nodeValues[i] = SerializableObjectContainer.Serialize(values[i], values[i]?.GetType());
            }
            nodeData.position = position;
            var udonNode = UdonNode.CreateNode(nodeData, this);
            AddElement(udonNode);
        }

        private void MakeOpNode<TRootType, TParamsType, TReturnType>(Vector2 position, string opName,
            params TParamsType[] paramValues)
        {
            var rootIdentifier = typeof(TRootType).FullName?.Replace(".", "").Replace("[]", "Array");
            var paramsIdentifier = typeof(TParamsType).FullName?.Replace(".", "").Replace("[]", "Array");
            var paramsTypesList = string.Join("_", new string[paramValues.Length].Select(s => paramsIdentifier));
            var returnIdentifier = typeof(TReturnType).FullName?.Replace(".", "");

            var identifier = $"{rootIdentifier}.__{opName}__";
            if (paramValues.Length > 0)
            {
                identifier += $"{paramsTypesList}__";
            }
            identifier += returnIdentifier;
            Undo.RecordObject(graphProgramAsset, $"Add {identifier} Node");
            
            var definition = UdonEditorManager.Instance.GetNodeDefinition(identifier);
            var nodeData = graphData.AddNode(definition.fullName);
            nodeData.nodeValues = new SerializableObjectContainer[paramValues.Length];
            for (int i = 0; i < paramValues.Length; i++)
            {
                nodeData.nodeValues[i] = SerializableObjectContainer.Serialize(paramValues[i], paramValues[i]?.GetType());
            }
            nodeData.position = position;
            var udonNode = UdonNode.CreateNode(nodeData, this);
            AddElement(udonNode);
        }

        private UdonNode MakeOpNode<TRootType, TParamsType>(Vector2 position, string opName,
            params TParamsType[] paramValues)
        {
            var rootIdentifier = typeof(TRootType).FullName?.Replace(".", "").Replace("[]", "Array");
            var paramsIdentifier = typeof(TParamsType).FullName?.Replace(".", "").Replace("[]", "Array");
            var paramsTypesList = string.Join("_", new string[paramValues.Length].Select(s => paramsIdentifier));
            const string returnIdentifier = "SystemVoid";
            
            var identifier = $"{rootIdentifier}.__{opName}__{paramsTypesList}__{returnIdentifier}";
            Undo.RecordObject(graphProgramAsset, $"Add {identifier} Node");
            
            var definition = UdonEditorManager.Instance.GetNodeDefinition(identifier);
            var nodeData = graphData.AddNode(definition.fullName);
            nodeData.nodeValues = new SerializableObjectContainer[paramValues.Length];
            for (int i = 0; i < paramValues.Length; i++)
            {
                nodeData.nodeValues[i] = SerializableObjectContainer.Serialize(paramValues[i], paramValues[i]?.GetType());
            }
            nodeData.position = position;
            var udonNode = UdonNode.CreateNode(nodeData, this);
            AddElement(udonNode);
            return udonNode;
        }

        private UdonNode MakeManualNode(Vector2 position, string identifier, params object[] paramValues)
        {
            Undo.RecordObject(graphProgramAsset, $"Add {identifier} Node");
            
            var definition = UdonEditorManager.Instance.GetNodeDefinition(identifier);
            var nodeData = graphData.AddNode(definition.fullName);
            nodeData.nodeValues = new SerializableObjectContainer[paramValues.Length];
            for (int i = 0; i < paramValues.Length; i++)
            {
                nodeData.nodeValues[i] = SerializableObjectContainer.Serialize(paramValues[i], paramValues[i]?.GetType());
            }
            nodeData.position = position;
            var udonNode = UdonNode.CreateNode(nodeData, this);
            AddElement(udonNode);
            return udonNode;
        }

        private string GetUnusedVariableNameLike(string newVariableName)
        {
            RefreshVariables(false);

            while (VariableNames.Contains(newVariableName))
            {
                char lastChar = newVariableName[newVariableName.Length - 1];
                if(char.IsDigit(lastChar))
                {
                    string newLastChar = (int.Parse(lastChar.ToString()) + 1).ToString();
                    newVariableName = newVariableName.Substring(0, newVariableName.Length - 1) + newLastChar;
                } 
                else
                {
                    newVariableName = $"{newVariableName}_1";   
                }
            }

            return newVariableName;
        }

        private void SetMouseTip(string message)
        {
            if (mouseTipContainer.visible)
            {
                mouseTip.text = message;
            }
        }

        private void LinkAfterCompile(string variableName, object target)
        {
            void Listener(bool success, string assembly)
            {
                if (!success) return;

                //TODO: get actual variable name in case it was auto-changed on add
                var result = udonBehaviour.publicVariables.TrySetVariableValue(variableName, target);
                if (result)
                {
                    graphProgramAsset.OnAssemble -= Listener;
                }
            }

            graphProgramAsset.OnAssemble += Listener;
            EditorUtility.SetDirty(graphProgramAsset);
            AssetDatabase.SaveAssets();
            graphProgramAsset.RefreshProgram();
        }

        private string GetDefinitionNameForType(Type t)
        {
            //TODO: why isn't this returning array types correctly?
            string variableType = $"Variable_{t}".SanitizeVariableName();
            variableType = variableType.Replace("UdonBehaviour", "CommonInterfacesIUdonEventReceiver");
            return variableType;
        }

        private void SetupDraggedObject(UnityEngine.Object o, Vector2 graphMousePosition)
        {
            // Ensure variable type is allowed
            
            // create new Component variable and add to graph
            string variableType = GetDefinitionNameForType(o.GetType());
            string variableName = GetUnusedVariableNameLike(o.name.SanitizeVariableName());

            SetMouseTip($"Made {variableName}");

            string uid = AddNewVariable(variableType, variableName, true);
            RefreshVariables(false);

            object target = o;
            // Cast component to expected type
            if (o is Component) target = Convert.ChangeType(o, o.GetType());
            var variableNode = MakeVariableNode(uid, graphMousePosition, VariableNodeType.Getter);
            AddElement(variableNode);

            LinkAfterCompile(variableName, target);
        }

        #region Shortcuts

        private void CreateConstructableType<TRootType, TParamsType, TReturnType>(Vector2 graphMousePosition, bool constructor, TRootType value, params TParamsType[] paramValues)
        {
            if (constructor)
            {
                MakeOpNode<TRootType, TParamsType, TReturnType>(graphMousePosition, "ctor", paramValues);
                return;
            }

            MakeConstantNode(graphMousePosition, value);
        }

        private void LogSelection()
        {
            if (selection.Count == 0) return;
            foreach (var selected in selection.ToArray())
            {
                if (!(selected is UdonNode node)) continue;
                var totalHeight = node.portsOut.Count * 100f;
                var portIndex = 0;
                foreach (var portOut in node.portsOut)
                {
                    var logRect = node.GetPosition();
                    logRect.x = logRect.xMax + 20;
                    logRect.y += node.portsOut[0].GetPosition().y;
                    logRect.y -= totalHeight / 2;
                    logRect.y += portIndex * 130f;
                    var logNode = MakeOpNode<Debug, System.Object>(logRect.position, "Log", new object[] {null});
                    ConnectNodeTo(logNode, portOut.Value, Direction.Input, typeof(object));
                    portIndex++;
                }
            }
        }

        private void ConvertConstantToVariable(UdonNode selectedNode)
        {
            var nodeDefinition = UdonEditorManager.Instance.GetNodeDefinition(selectedNode.definition.fullName);
            string variableType = GetDefinitionNameForType(nodeDefinition.type); 
            string variableName = GetUnusedVariableNameLike(variableType.SanitizeVariableName());

            string uid = AddNewVariable(variableType, variableName, true);
            RefreshVariables(false);
                            
            var variableNode = MakeVariableNode(uid, selectedNode.GetPosition().position, VariableNodeType.Getter);
            AddElement(variableNode);

            foreach (var graphNode in graphData.nodes)
            {
                var index = graphNode.DataNodes.ToList().IndexOf(selectedNode.data);
                if (index == -1) continue;
                graphNode.RemoveNode(index);
                graphNode.AddNode(variableNode.data, index, 0);
            }
            RemoveNodeAndData(selectedNode.data);

            EditorUtility.SetDirty(graphProgramAsset);
            AssetDatabase.SaveAssets();
            graphProgramAsset.RefreshProgram();
        }

        private void AlignNodes(GraphElement selectedNode = null)
        {
            var selectedItems = selection.OfType<UdonNode>().ToList();
            Undo.RecordObject(graphProgramAsset, "Aligned Nodes");
            var rootPos = selectedNode?.GetPosition() ?? selectedItems[0].GetPosition();
            foreach (var t in selectedItems)
            {
                if (t.Equals(selectedNode)) continue;
                var newPos = t.GetPosition();
                newPos.y = rootPos.y;
                t.SetPosition(newPos);
            }
            EditorUtility.SetDirty(graphProgramAsset);
            AssetDatabase.SaveAssets();
            graphProgramAsset.RefreshProgram();
        }

        private void CreateGroup()
        {
            UdonGroup group = UdonGroup.Create("Group", GetRectFromMouse(), this);
            Undo.RecordObject(graphProgramAsset, "Add Group");
            AddElement(group);
            group.UpdateDataId();

            for (int i = selection.Count - 1; i >= 0; i--)
            {
                ISelectable item = selection[i];
                switch (item)
                {
                    case UdonNode node:
                        group.AddElement(node);
                        break;
                    case UdonComment comment:
                        group.AddElement(comment);
                        break;
                    case UdonGroup groupItem:
                        DeleteElements(new GraphElement[] { groupItem });
                        break;
                }
            }

            group.Initialize();
            SaveGraphElementData(group);
            _sidebar.GroupsList.AddGroup(group);
        }

        private void CreateForEachLoop(UdonNode selectedNode)
        {
            var rootPos = selectedNode.data.position;
            var portType = selectedNode.portsOut[0].portType;
            if (portType == null || !portType.HasElementType)
            {
                return;
            }
            var portElType = portType.GetElementType()?.FullName?.Replace(".", "");

            // Set initial step value to 1 to prevent infinite loops
            var forValues = new[] { SerializableObjectContainer.Serialize(null), SerializableObjectContainer.Serialize(null), SerializableObjectContainer.Serialize(1) };
            UdonNode forNode = MakeSpecialNode(SpecialNodeType.For, new Vector2(rootPos.x + 400, rootPos.y - 150), forValues);

            var getNode = MakeManualNode(new Vector2(rootPos.x + 670, rootPos.y), $"{portElType}Array.__Get__SystemInt32__{portElType}", null, 0);
            var lengthNode = MakeManualNode(new Vector2(rootPos.x + 220, rootPos.y - 40), $"{portElType}Array.__get_Length__SystemInt32", new object[] { null });
            var flowInNode = MakeSpecialNode(SpecialNodeType.Block, new Vector2(rootPos.x, rootPos.y - 150));
            var flowOutNode = MakeSpecialNode(SpecialNodeType.Block, new Vector2(rootPos.x + 940, rootPos.y - 290));
            var logNode = MakeOpNode<Debug, System.Object>(new Vector2(rootPos.x + 900, rootPos.y - 170), "Log", new object[] {null});

            ConnectNodeTo(lengthNode, selectedNode.portsOut[0], Direction.Input, portType);
            ConnectNodeTo(lengthNode, forNode.portsIn[1], Direction.Output, typeof(int));
            ConnectNodeToFlow(forNode, flowInNode.portsFlowOut[0], Direction.Input, 0);
            ConnectNodeToFlow(logNode, forNode.portsFlowOut[0], Direction.Input, 0);
            ConnectNodeTo(getNode, forNode.portsOut[0], Direction.Input, typeof(int));
            ConnectNodeTo(getNode, selectedNode.portsOut[0], Direction.Input, portType); 
            ConnectNodeTo(logNode, getNode.portsOut[0], Direction.Input, typeof(object));
            ConnectNodeToFlow(flowOutNode, forNode.portsFlowOut[1], Direction.Input, 0);
        }
        
        #endregion
    }
}
