using UnityEngine.UIElements;
using UIElements = UnityEditor.UIElements;
using UnityEngine;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI
{
    public class LayerMaskField : BaseField<LayerMask>
    {
        public LayerMaskField() : base(null,null)
        {
            // Set up styling
            AddToClassList("UdonValueField");

            // Create LayerMask Editor and listen for changes
            UIElements.LayerMaskField field = new UIElements.LayerMaskField();
            field.RegisterValueChangedCallback(e =>
            {
                this.value = e.newValue;
            });

            Add(field);
        }
    }
}