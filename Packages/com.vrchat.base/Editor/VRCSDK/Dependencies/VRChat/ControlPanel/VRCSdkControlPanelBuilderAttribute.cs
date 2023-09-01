using System;
using JetBrains.Annotations;

namespace VRC.SDKBase.Editor
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    [MeansImplicitUse]
    public class VRCSdkControlPanelBuilderAttribute : Attribute
    {
        public Type Type { get; }
        public VRCSdkControlPanelBuilderAttribute(Type type)
        {
            Type = type;
        }
    }
}
