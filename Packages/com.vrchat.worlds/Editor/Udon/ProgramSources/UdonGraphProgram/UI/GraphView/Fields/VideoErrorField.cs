using UnityEditor.UIElements;
using UnityEngine.UIElements;
using VRC.SDK3.Components.Video;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI
{
    public class VideoErrorField : BaseField<VideoError>
    {
        public VideoErrorField() : base(null, null)
        {
            // Set up styling
            AddToClassList("UdonValueField");

            // Create LayerMask Editor and listen for changes
            EnumField field = new EnumField();
            field.Init(VideoError.Unknown);
            field.RegisterValueChangedCallback(e =>
            {
                this.value =  (VideoError)e.newValue;
            });

            Add(field);
        }
    }
}