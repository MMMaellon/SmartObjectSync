using UnityEditor.UIElements;
using UnityEngine.UIElements;
using VRC.SDK3.Components;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI
{
    public class MirrorReflectionClearFlagsField : BaseField<MirrorClearFlags>
    {
        public MirrorReflectionClearFlagsField() : base(null, null)
        {
            // Set up styling
            AddToClassList("UdonValueField");

            // Create LayerMask Editor and listen for changes
            EnumField field = new EnumField();
            field.Init(MirrorClearFlags.FromReferenceCamera);
            field.RegisterValueChangedCallback(e =>
            {
                this.value = (MirrorClearFlags)e.newValue;
            });

            Add(field);
        }
    }
}