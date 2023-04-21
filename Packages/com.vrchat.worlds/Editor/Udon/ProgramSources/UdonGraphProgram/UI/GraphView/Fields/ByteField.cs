using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI
{
    public class ByteField : BaseField<byte>
    {
        public ByteField():base(null,null)
        {
            // Set up styling
            AddToClassList("UdonValueField");
            
            // Create Char Editor and listen for changes
            IntegerField field = new IntegerField();
            
            field.RegisterValueChangedCallback(
                e =>
                    value = Convert.ToByte(e.newValue));

            Add(field);
        }
    }
}