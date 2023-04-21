using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using VRC.Udon.EditorBindings;
using VRC.Udon.UAssembly.Assembler;
using VRC.Udon.UAssembly.Attributes;

[assembly: UdonTypeResolver(typeof(UdonBehaviourTypeResolver), "UdonBehaviourTypeResolver")]
namespace VRC.Udon.EditorBindings
{
    public class UdonBehaviourTypeResolver : BaseTypeResolver
    {
        private static readonly Dictionary<string, Type> _types = new Dictionary<string, Type>
        {
            {"VRCUdonUdonBehaviour", typeof(UdonBehaviour)},
        };

        protected override Dictionary<string, Type> Types => _types;
        
        [PublicAPI]
        public UdonBehaviourTypeResolver()
        {
        }
    }
}
