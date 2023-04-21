using UnityEngine.UIElements;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI
{
    public class CharField : BaseField<char>
    {
        public CharField():base(null,null)
        {
            // Set up styling
            AddToClassList("UdonValueField");
            
            // Create Char Editor and listen for changes
            TextField field = new TextField
            {
                maxLength = 1
            };
            field.RegisterValueChangedCallback(
                e =>
                    value = e.newValue.ToCharArray()[0]);

            Add(field);
        }
    }
}