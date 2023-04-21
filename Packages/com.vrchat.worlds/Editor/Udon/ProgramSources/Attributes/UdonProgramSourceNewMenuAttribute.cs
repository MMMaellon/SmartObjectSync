using System;

namespace VRC.Udon.Editor.ProgramSources.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class UdonProgramSourceNewMenuAttribute : Attribute
    {
        public Type Type { get; }
        public string DisplayName { get; }

        public UdonProgramSourceNewMenuAttribute(Type type, string displayName)
        {
            Type = type;
            DisplayName = displayName;
        }
    }
}

