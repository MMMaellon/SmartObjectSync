#if !VRC_CLIENT
using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.Udon;
using VRC.Udon.Common.Attributes;
using VRC.Udon.Common.Delegates;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Security.Interfaces;
using VRC.Udon.Wrapper.Modules;
using Object = UnityEngine.Object;

[assembly: UdonWrapperModule(typeof(ExternVRCInstantiate))]

namespace VRC.Udon.Wrapper.Modules
{
    public class ExternVRCInstantiate : IUdonWrapperModule
    {
        public string Name => "VRCInstantiate";

        private readonly Dictionary<string, int> _parameterCounts;
        private readonly Dictionary<string, UdonExternDelegate> _functionDelegates;
        private readonly IUdonSecurityBlacklist _blacklist;

        //Passing unused parameter for consistent construction
        // ReSharper disable once UnusedParameter.Local
        public ExternVRCInstantiate(IUdonComponentGetter componentGetter, IUdonSecurityBlacklist blacklist)
        {
            _blacklist = blacklist;
            _parameterCounts = new Dictionary<string, int>
            {
                {"__Instantiate__UnityEngineGameObject__UnityEngineGameObject", 2},
            };

            _functionDelegates = new Dictionary<string, UdonExternDelegate>
            {
                {"__Instantiate__UnityEngineGameObject__UnityEngineGameObject", __Instantiate__UnityEngineGameObject__UnityEngineGameObject}
            };
        }

        public int GetExternFunctionParameterCount(string externFunctionSignature)
        {
            if(_parameterCounts.TryGetValue(externFunctionSignature, out int numParameters))
            {
                return numParameters;
            }

            throw new System.NotSupportedException($"Function '{externFunctionSignature}' is not implemented yet");
        }

        public UdonExternDelegate GetExternFunctionDelegate(string externFunctionSignature)
        {
            if(_functionDelegates.TryGetValue(externFunctionSignature, out UdonExternDelegate externDelegate))
            {
                return externDelegate;
            }

            throw new System.NotSupportedException($"Function '{externFunctionSignature}' is not implemented yet");
        }

        private void __Instantiate__UnityEngineGameObject__UnityEngineGameObject(IUdonHeap heap, Span<uint> parameterAddresses)
        {
            GameObject original = heap.GetHeapVariable<GameObject>(parameterAddresses[0]);
            #if !UDON_DISABLE_SECURITY
            _blacklist.FilterBlacklisted(ref original);
            #endif

            GameObject clone = Object.Instantiate(original);
            foreach(UdonBehaviour udonBehaviour in clone.GetComponentsInChildren<UdonBehaviour>(true))
            {
                UdonManager.Instance.RegisterUdonBehaviour(udonBehaviour);
            }

            heap.SetHeapVariable(parameterAddresses[1], clone);
        }
    }
}
#endif
