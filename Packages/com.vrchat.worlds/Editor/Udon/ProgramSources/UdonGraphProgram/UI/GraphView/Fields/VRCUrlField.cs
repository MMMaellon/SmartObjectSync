using UnityEngine.UIElements;
using System;
using VRC.SDKBase;

namespace VRC.Udon.Editor.ProgramSources.UdonGraphProgram.UI
{
    public class VRCUrlField : BaseField<VRCUrl>
    {
        public VRCUrlField():base(null,null)
        {
            // Set up styling
            AddToClassList("UdonValueField");
            
            // Create Text Editor and listen for changes
            TextField field = new TextField(50, false, false, Char.MinValue);
            field.RegisterValueChangedCallback(
                e => 
                    value = new VRCUrl(e.newValue)
            );
            Add(field);
        }
    }
}