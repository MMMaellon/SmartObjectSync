using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI.GraphView
{
    public class UdonVariableRow : VisualElement
    {
        private VisualElement m_Root; 
        private Button m_ExpandButton;
        private VisualElement m_ItemContainer;
        private VisualElement m_PropertyViewContainer;
        private UdonParameterField m_item;
        private UdonParameterProperty m_propertyView;
        private bool m_Expanded = true;
        
        /// <summary>
        ///   <para>Indicates whether the BlackboardRow is expanded.</para>
        /// </summary>
        public bool expanded
        {
            get => m_Expanded;
            set
            {
                if (m_Expanded == value)
                  return;
                m_Expanded = value;
                if (m_Expanded)
                {
                  m_Root.Add(m_PropertyViewContainer);
                  AddToClassList(nameof (expanded));
                }
                else
                {
                  m_Root.Remove(m_PropertyViewContainer);
                  RemoveFromClassList(nameof (expanded));
                }
            }
        }

        /// <summary>
        ///   <para>Constructs a BlackboardRow from a VisualElement and its associated property view. The VisualElement is usually a BlackboardField.</para>
        /// </summary>
        /// <param name="item">The item that fills the content of this BlackboardRow.</param>
        /// <param name="propertyView">The property view related to the content of this BlackboardRow.</param>
        public UdonVariableRow(UdonParameterField item, UdonParameterProperty propertyView)
        {
            VisualTreeAsset visualTreeAsset = EditorGUIUtility.Load("UXML/GraphView/BlackboardRow.uxml") as VisualTreeAsset;
            var styleSheet = EditorGUIUtility.Load("StyleSheets/GraphView/Blackboard.uss") as StyleSheet;
            this.styleSheets.Add(styleSheet);
            VisualElement visualElement = visualTreeAsset.CloneTree();
            visualElement.AddToClassList("mainContainer");
            m_Root = visualElement.Q("root");
            m_ItemContainer = visualElement.Q("itemContainer");
            m_PropertyViewContainer = visualElement.Q("propertyViewContainer");
            m_ExpandButton = visualElement.Q<Button>("expandButton");
            m_ExpandButton.clickable.clicked += () => expanded = !expanded;
            Add(visualElement);
            ClearClassList();
            AddToClassList("blackboardRow");
            m_ItemContainer.Add(item);
            m_PropertyViewContainer.Add(propertyView);
            expanded = false;
            m_item = item;
            m_propertyView = propertyView;
        }
    }
}