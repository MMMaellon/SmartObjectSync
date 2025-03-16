using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Unity.Profiling;
using UnityEngine;
using VRC.Compression;
using VRC.SDK3.Components;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;
using VRC.Udon.Serialization.OdinSerializer.Utilities;

[assembly: UdonSignatureHolderMarker(typeof(VRC.Udon.ProgramSources.SerializedUdonProgramAsset))]

namespace VRC.Udon.ProgramSources
{
    public sealed class SerializedUdonProgramAsset : AbstractSerializedUdonProgramAsset, IUdonSignatureHolder
    {
        private static readonly Lazy<string> _debugCategory = new Lazy<string>(InitializeLogging);
        private static string DebugCategoryName => _debugCategory.Value;

        private const DataFormat DEFAULT_SERIALIZATION_DATA_FORMAT = DataFormat.Binary;
        private const int MAXIMUM_CACHED_PROGRAM_SIZE = 1024 * 1024 * 2; // 2 MB

        [SerializeField, HideInInspector]
        private byte[] serializedProgramCompressedBytes;

        [SerializeField, HideInInspector]
        private string serializedProgramBytesString;

        [SerializeField, HideInInspector]
        private byte[] serializedSignature;

        [SerializeField, HideInInspector]
        private List<UnityEngine.Object> programUnityEngineObjects;

        // Store the serialization DataFormat that was actually used to serialize the program.
        // This allows us to change the DataFormat later (ex. switch to binary) without causing already serialized programs to use the wrong DataFormat.
        // Programs will be deserialized using the previous format and will switch to the new format if StoreProgram is called again later.
        [SerializeField, HideInInspector]
        private DataFormat serializationDataFormat = DEFAULT_SERIALIZATION_DATA_FORMAT;

        // Cache the deserialized program and a serialized copy of its IUdonHeap to more efficiently create clones of the IUdonProgram.
        private (IUdonProgram program, byte[] serializedHeap, List<UnityEngine.Object> serializedHeapUnityEngineObjects)? _serializationCache = null;

        private int _mainThreadId;

        private void OnEnable()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
#if VRC_CLIENT
            try
            {
                ulong totalProgramSize = GetSerializedProgramSize();
                if(totalProgramSize >= MAXIMUM_CACHED_PROGRAM_SIZE)
                {
                    Core.Logger.LogWarning(
                        $"Skipping caching of UdonProgram '{name}' as the total program size ({totalProgramSize}) is higher than '{MAXIMUM_CACHED_PROGRAM_SIZE}'",
                        DebugCategoryName);

                    _serializationCache = null;
                    return;
                }

                // Deserialize the full program once and cache it.
                // Then reserialize the IUdonHeap and cache it so we can deserialize a clone for each UdonProgram.
                // This is more efficient than SerializationUtility.CreateCopy which serializes each time.
                IUdonProgram deserializedUdonProgram = ReadSerializedProgram();
                if(deserializedUdonProgram == null)
                {
                    return;
                }

                byte[] serializedUdonHeap = SerializationUtility.SerializeValue(
                    deserializedUdonProgram.Heap,
                    serializationDataFormat,
                    out List<UnityEngine.Object> serializedHeapUnityEngineObjects);

                _serializationCache = (deserializedUdonProgram, serializedUdonHeap, serializedHeapUnityEngineObjects);
            }
            catch(Exception e)
            {
                Core.Logger.LogWarning($"Failed to deserialize Udon Program due to :\n{e}.", DebugCategoryName);
            }
#endif
        }

        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
        public override void StoreProgram(IUdonProgram udonProgram)
        {
            if(this == null)
            {
                return;
            }

            byte[] serializedProgramBytes = SerializationUtility.SerializeValue(udonProgram, DEFAULT_SERIALIZATION_DATA_FORMAT, out programUnityEngineObjects);
            // Store a compressed byte array only - we no longer store Base64 encoded strings.
            serializedProgramCompressedBytes = GZip.Compress(serializedProgramBytes);
            serializedProgramBytesString = string.Empty;
            serializationDataFormat = DEFAULT_SERIALIZATION_DATA_FORMAT;

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        private ProfilerMarker _retrieveProgramProfilerMarker = new ProfilerMarker("SerializedUdonProgram.RetrieveProgram");
        private ProfilerMarker _retrieveProgramCopyHeapProfilerMarker = new ProfilerMarker("SerializedUdonProgram.RetrieveProgram CopyHeap");
        private ProfilerMarker _cloneProgramCopyByteCodeProfilerMarker = new ProfilerMarker("SerializedUdonProgram.RetrieveProgram CopyByteCode");

        public override IUdonProgram RetrieveProgram()
        {
            using(_retrieveProgramProfilerMarker.Auto())
            {
                try
                {
                    if(this == null)
                    {
                        return null;
                    }

                    if(_serializationCache == null)
                    {
                        return ReadSerializedProgram();
                    }

                    (IUdonProgram program, byte[] serializedHeap, List<UnityEngine.Object> serializedHeapUnityEngineObjects) = _serializationCache.Value;
                    if(program == null || serializedHeap == null || serializedHeapUnityEngineObjects == null)
                    {
                        return null;
                    }

                    // Clone the byte code array.
                    byte[] byteCodeCopy;
                    using(_cloneProgramCopyByteCodeProfilerMarker.Auto())
                    {
                        byte[] byteCode = program.ByteCode;
                        byteCodeCopy = new byte[byteCode.Length];
                        Array.Copy(byteCode, byteCodeCopy, byteCode.Length);
                    }

                    // Deserialize a fresh copy of the serialized IUdonHeap from earlier.
                    IUdonHeap udonHeapCopy;
                    using(_retrieveProgramCopyHeapProfilerMarker.Auto())
                    {
                        using (var cachedContext = Cache<DeserializationContext>.Claim())
                        {
                            var context = cachedContext.Value;
                            context.Config.SerializationPolicy = SerializationPolicies.Everything;
                            context.Config.DebugContext.ErrorHandlingPolicy =
                                Thread.CurrentThread.ManagedThreadId == _mainThreadId
                                    ? ErrorHandlingPolicy.Resilient
                                    : ErrorHandlingPolicy.ThrowOnWarningsAndErrors;
                            udonHeapCopy = SerializationUtility.DeserializeValue<IUdonHeap>(
                                serializedHeap,
                                serializationDataFormat,
                                serializedHeapUnityEngineObjects,
                                context);
                        }
                    }

                    // Everything except the byte code array and IUdonHeap are immutable so they don't need to be cloned.
                    return new UdonProgram(
                        program.InstructionSetIdentifier,
                        program.InstructionSetVersion,
                        byteCodeCopy,
                        udonHeapCopy,
                        program.EntryPoints,
                        program.SymbolTable,
                        program.SyncMetadataTable,
                        program.UpdateOrder
                    );
                }
                catch(SerializationAbortException e)
                {
                    // Odin can't deserialize Unity Gradients properly off the main-thread 
                    if(!e.Message.StartsWith("Failed to read Gradient.mode, due to Unity's API"))
                    {
                        Core.Logger.LogWarning($"Failed to deserialize Udon Program due to an exception:\n{e}.", DebugCategoryName);
                    }

                    return null;
                }
                catch(Exception e)
                {
                    Core.Logger.LogWarning($"Failed to deserialize Udon Program due to an exception:\n{e}.", DebugCategoryName);
                    return null;
                }
            }
        }

        private IUdonProgram ReadSerializedProgram()
        {
            byte[] serializedProgramBytes = null;

            // If the newer compressed bytes format is available, use that.
            if (serializedProgramCompressedBytes != null && serializedProgramCompressedBytes.Length > 0)
            {
                try
                {
                    serializedProgramBytes = GZip.Decompress(serializedProgramCompressedBytes);
                }
                catch (InvalidDataException invalidDataException)
                {
                    Core.Logger.LogWarning($"Failed to deserialize UdonProgram because the program was invalid. Exception:\n{invalidDataException}");
                }
            }

            // If there is no compressed byte array or reading the array failed, try to fall back to the base 64 encoded string.
            if (serializedProgramBytes == null)
            {
                try
                {
                    serializedProgramBytes = Convert.FromBase64String(serializedProgramBytesString ?? "");
                }
                catch (FormatException formatException)
                {
                    Core.Logger.LogWarning($"Failed to deserialize UdonProgram because the program was invalid. Exception:\n{formatException}");
                }
            }

            if (serializedProgramBytes == null)
            {
                return null;
            }
            else
            {
                return SerializationUtility.DeserializeValue<IUdonProgram>(serializedProgramBytes, serializationDataFormat, programUnityEngineObjects);
            }
        }

        /// <summary>
        /// Finds the total size of this serialized Udon program.
        /// </summary>
        /// <returns>The size of the program in bytes.</returns>
        public override ulong GetSerializedProgramSize()
        {
            if (serializedProgramCompressedBytes != null && serializedProgramCompressedBytes.Length > 0)
            {
                return (ulong)serializedProgramCompressedBytes.Length;
            }
            else if (!string.IsNullOrEmpty(serializedProgramBytesString))
            {
                return (ulong)serializedProgramBytesString.Length;
            }

            return 0L;
        }

        private static string InitializeLogging()
        {
            const string categoryName = "SerializedUdonProgramAsset";
            if(Core.Logger.CategoryIsDescribed(categoryName))
            {
                return categoryName;
            }

            Core.Logger.DescribeCategory(categoryName, Core.Logger.Color.blue);
            Core.Logger.EnableCategory(categoryName);
            return categoryName;
        }

        private void OnDisable()
        {
#if VRC_CLIENT
            serializedProgramCompressedBytes = null;
            serializedProgramBytesString = null;
            programUnityEngineObjects = null;
#endif
            _serializationCache = null;
        }

#region IUdonSignatureHolder
        void IUdonSignatureHolder.EnsureGZipFormat()
        {
            if ((serializedProgramCompressedBytes == null || serializedProgramCompressedBytes.Length == 0) && !string.IsNullOrEmpty(serializedProgramBytesString))
            {
                Core.Logger.Log($"Converting SerializedUdonProgramAsset '{name}' to compressed format");
                serializedProgramCompressedBytes = GZip.Compress(Convert.FromBase64String(serializedProgramBytesString));
            }
            serializedProgramBytesString = null; // always clear, compressedBytes format has priority in case somehow both get set
        }

        byte[] IUdonSignatureHolder.Signature
        {
            get => serializedSignature;
            set => serializedSignature = value;
        }

        byte[] IUdonSignatureHolder.SignedData => serializedProgramCompressedBytes;

        // in client only, allow skipping signature validation for internal behaviours (like stations)
        public bool IsInternallyValidated { get; private set; } = false;
    #if VRC_CLIENT
        public void SetInternallyValidated() => IsInternallyValidated = true;
    #endif
#endregion
    }
}
