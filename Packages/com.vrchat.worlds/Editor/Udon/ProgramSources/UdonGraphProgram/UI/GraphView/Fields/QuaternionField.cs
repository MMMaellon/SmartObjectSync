using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI
{
    public class QuaternionField : BaseField<Quaternion>
    {
        public QuaternionField() :base(null, null)
        {
            // Set up styling
            AddToClassList("UdonValueField");
            
            // Create Vector4 Editor and listen for changes
            Vector4Field field = new Vector4Field();
            field.RegisterValueChangedCallback(
                e => 
                    value = new Quaternion(e.newValue.x, e.newValue.y, e.newValue.z, e.newValue.w)
                );
            Add(field);
        }

    }
}