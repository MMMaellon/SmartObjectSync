using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace VRC.Udon
{
    public abstract class AbstractUdonProgramSource : ScriptableObject
    {
        [PublicAPI]
        public abstract AbstractSerializedUdonProgramAsset SerializedProgramAsset { get; }

        [PublicAPI]
        public abstract void RunEditorUpdate(UdonBehaviour udonBehaviour, ref bool dirty);

        [PublicAPI]
        public abstract void RefreshProgram();
    }
}
