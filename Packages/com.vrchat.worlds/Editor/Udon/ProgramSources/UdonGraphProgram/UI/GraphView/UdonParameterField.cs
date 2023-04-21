using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.Udon.Graph;
using EditorUI = UnityEditor.UIElements;
using MenuAction = UnityEngine.UIElements.DropdownMenuAction;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonParameterField : GraphElement
    {
        private UdonGraph udonGraph;
        private UdonNodeData nodeData;
        public UdonNodeData Data => nodeData;
        
        private VisualElement m_ContentItem;
        private Pill m_Pill;
        private TextField m_TextField;
        private Label m_TypeLabel;

        private Action<VisualElement, string> EditTextRequested { get; set; }
        
        /// <summary>
        ///   <para>The text of this BlackboardField.</para>
        /// </summary>
        public string text
        {
            get => m_Pill.text;
            set => m_Pill.text = value;
        }

        /// <summary>
        ///   <para>The text that displays the data type of this BlackboardField.</para>
        /// </summary>
        public string typeText
        {
            get => m_TypeLabel.text;
            set => m_TypeLabel.text = value;
        }

        /// <summary>
        ///   <para>The icon of this BlackboardField.</para>
        /// </summary>
        public Texture icon
        {
            get => m_Pill.icon;
            set => m_Pill.icon = value;
        }

        /// <summary>
        ///   <para>The highlighted state of this BlackboardField.</para>
        /// </summary>
        public bool highlighted
        {
            get => m_Pill.highlighted;
            set => m_Pill.highlighted = value;
        }

        
        public UdonParameterField(UdonGraph udonGraph, UdonNodeData nodeData, Action<VisualElement, string> editTextRequested)
        {
            EditTextRequested += editTextRequested;
            
            VisualElement visualElement = (EditorGUIUtility.Load("UXML/GraphView/BlackboardField.uxml") as VisualTreeAsset).CloneTree();
            //this.AddStyleSheetPath(Blackboard.StyleSheetPath);
            visualElement.AddToClassList("mainContainer");
            visualElement.pickingMode = PickingMode.Ignore;
            m_ContentItem = visualElement.Q("contentItem");
            m_Pill = visualElement.Q<Pill>("pill");
            m_TypeLabel = visualElement.Q<Label>("typeLabel");
            m_TextField = visualElement.Q<TextField>("textField");
            m_TextField.style.display = DisplayStyle.None;
            m_TextField.Q(TextInputBaseField<string>.textInputUssName).RegisterCallback((EventCallback<FocusOutEvent>) (e => OnEditTextFinished()));
            Add(visualElement);
            RegisterCallback(new EventCallback<MouseDownEvent>(OnMouseDownEvent));
            capabilities |= Capabilities.Selectable | Capabilities.Deletable | Capabilities.Droppable | Capabilities.Renamable;
            ClearClassList();
            AddToClassList("blackboardField");
            text = text;
            icon = icon;
            typeText = typeText;
            this.AddManipulator(new SelectionDropper());
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            this.udonGraph = udonGraph;
            this.nodeData = nodeData;

            // Get Definition or exit early
            UdonNodeDefinition definition = UdonEditorManager.Instance.GetNodeDefinition(nodeData.fullName);
            if (definition == null)
            {
                Debug.LogWarning($"Couldn't create Parameter Field for {nodeData.fullName}");
                return;
            }

            text = (string) nodeData.nodeValues[(int) UdonParameterProperty.ValueIndices.Name].Deserialize();
            typeText = UdonGraphExtensions.PrettyString(definition.name).FriendlyNameify();

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            this.Q("icon").AddToClassList("parameter-" + definition.type);
            this.Q("icon").visible = true;

            var textField = (TextField) this.Q("textField");
            textField.isDelayed = true;
        }
        
        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Rename", a => OpenTextEditor(), MenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Delete", a => udonGraph.RemoveNodeAndData(nodeData), MenuAction.AlwaysEnabled);

            evt.StopPropagation();
        }
        
        private void OnEditTextFinished()
        {
            m_ContentItem.visible = true;
            m_TextField.style.display = DisplayStyle.None;
            if (text == m_TextField.text) return; // If the text didn't change, don't do anything
            
            EditTextRequested(this, m_TextField.text);
        }
        
        private void OnMouseDownEvent(MouseDownEvent e)
        {
            if (e.clickCount != 2 || e.button != 0 || !this.IsRenamable())
                return;
            OpenTextEditor();
            e.PreventDefault();
        }

        /// <summary>
        ///   <para>Opens a TextField to edit the text in a BlackboardField.</para>
        /// </summary>
        private void OpenTextEditor()
        {
            m_TextField.SetValueWithoutNotify(text);
            m_TextField.style.display = DisplayStyle.Flex;
            m_ContentItem.visible = false;
            m_TextField.Q(TextInputBaseField<string>.textInputUssName).Focus();
            m_TextField.SelectAll();
        }

    }
}