using JetBrains.Annotations;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace VRC.Udon
{
    public abstract class AbstractSerializedUdonProgramAsset : ScriptableObject
    {
        [PublicAPI]
        public abstract void StoreProgram(IUdonProgram udonProgram);

        [PublicAPI]
        public abstract IUdonProgram RetrieveProgram();

        [PublicAPI]
        public abstract ulong GetSerializedProgramSize();
    }
}
