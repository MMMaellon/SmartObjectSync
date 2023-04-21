using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI
{
    public class DecimalField : BaseField<decimal>
    {
        public DecimalField():base(null,null)
        {
            // Set up styling
            AddToClassList("UdonValueField");
            
            // Create Char Editor and listen for changes
            DoubleField field = new DoubleField();
            field.RegisterValueChangedCallback(
                e =>
                    value = Convert.ToDecimal(e.newValue));

            Add(field);
        }
    }
}