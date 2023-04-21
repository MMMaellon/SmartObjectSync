using System;
using System.Collections.Generic;
using VRC.Udon.Graph.Interfaces;
using VRC.Udon.Graph.Attributes;
using VRC.Udon.Graph.NodeRegistries;

[assembly: UdonGraphNodeRegistry(typeof(VRCInstantiateNodeRegistry), "VRCInstantiateNodeRegistry")]
namespace VRC.Udon.Graph.NodeRegistries
{
    public class VRCInstantiateNodeRegistry : BaseNodeRegistry
    {
        protected override Dictionary<string, INodeRegistry> NextRegistries => _nextRegistries;
        private static readonly Dictionary<string, INodeRegistry> _nextRegistries = new Dictionary<string, INodeRegistry>();

        protected override Dictionary<string, UdonNodeDefinition> NodeDefinitions => _nodeDefinitions;

        private static readonly Dictionary<string, UdonNodeDefinition> _nodeDefinitions = new Dictionary<string, UdonNodeDefinition>
        {
            {
                "VRCInstantiate.__Instantiate__UnityEngineGameObject__UnityEngineGameObject",
                new UdonNodeDefinition(
                    "VRChat Instantiate",
                    "VRCInstantiate.__Instantiate__UnityEngineGameObject__UnityEngineGameObject",
                    typeof(UnityEngine.Object),
                    new []
                    {
                        new UdonNodeParameter
                        {
                            name = "original",
                            type = typeof(UnityEngine.GameObject),
                            parameterType = UdonNodeParameter.ParameterType.IN
                        }, 
                        new UdonNodeParameter
                        {
                            name = "clone",
                            type = typeof(UnityEngine.GameObject),
                            parameterType = UdonNodeParameter.ParameterType.OUT
                        } 
                    },
                    new string[] { },
                    new string[] { },
                    new object[] { },
                    true
                )
            }
        };
    }
}
