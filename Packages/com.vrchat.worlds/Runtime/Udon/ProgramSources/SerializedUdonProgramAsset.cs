using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Unity.Profiling;
using UnityEngine;
using VRC.Compression;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;

namespace VRC.Udon.ProgramSources
{
    public sealed class SerializedUdonProgramAsset : AbstractSerializedUdonProgramAsset
    {
        private static readonly Lazy<int> _debugLevel = new Lazy<int>(InitializeLogging);
        private static int DebugLevel => _debugLevel.Value;

        private const DataFormat DEFAULT_SERIALIZATION_DATA_FORMAT = DataFormat.Binary;
        private const int MAXIMUM_CACHED_PROGRAM_SIZE = 1024 * 1024 * 2; // 2 MB

        [SerializeField, HideInInspector]
        private byte[] serializedProgramCompressedBytes;

        [SerializeField, HideInInspector]
        private string serializedProgramBytesString;

        [SerializeField, HideInInspector]
        private List<UnityEngine.Object> programUnityEngineObjects;

        // Store the serialization DataFormat that was actually used to serialize the program.
        // This allows us to change the DataFormat later (ex. switch to binary) without causing already serialized programs to use the wrong DataFormat.
        // Programs will be deserialized using the previous format and will switch to the new format if StoreProgram is called again later.
        [SerializeField, HideInInspector]
        private DataFormat serializationDataFormat = DEFAULT_SERIALIZATION_DATA_FORMAT;

        // Cache the deserialized program and a serialized copy of its IUdonHeap to more efficiently create clones of the IUdonProgram.
        private (IUdonProgram program, byte[] serializedHeap, List<UnityEngine.Object> serializedHeapUnityEngineObjects)? _serializationCache = null;

        // Create a DeserializationContext for each thread to avoid race conditions.
        private ThreadLocal<DeserializationContext> _heapCopyDeserializationContextThreadLocal;

        private void Awake()
        {
            int mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _heapCopyDeserializationContextThreadLocal = new ThreadLocal<DeserializationContext>(
                () =>
                {
                    DeserializationContext context = new DeserializationContext
                    {
                        Config =
                        {
                            SerializationPolicy = SerializationPolicies.Everything,
                            DebugContext =
                            {
                                // Make other threads more sensitive to errors and warnings to catch Odin complaining about Unity not allowing certain properties to be set from other threads.
                                ErrorHandlingPolicy = Thread.CurrentThread.ManagedThreadId == mainThreadId ? ErrorHandlingPolicy.Resilient : ErrorHandlingPolicy.ThrowOnWarningsAndErrors
                            }
                        }
                    };

                    return context;
                });

            #if VRC_CLIENT
            try
            {
                ulong totalProgramSize = GetSerializedProgramSize();
                if(totalProgramSize >= MAXIMUM_CACHED_PROGRAM_SIZE)
                {
                    Core.Logger.LogWarning(
                        $"Skipping caching of UdonProgram '{name}' as the total program size ({totalProgramSize}) is higher than '{MAXIMUM_CACHED_PROGRAM_SIZE}'",
                        DebugLevel);

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
                Core.Logger.LogWarning($"Failed to deserialize Udon Program due to :\n{e}.", DebugLevel);
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
                        udonHeapCopy = SerializationUtility.DeserializeValue<IUdonHeap>(
                            serializedHeap,
                            serializationDataFormat,
                            serializedHeapUnityEngineObjects,
                            _heapCopyDeserializationContextThreadLocal.Value);
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
                        Core.Logger.LogWarning($"Failed to deserialize Udon Program due to an exception:\n{e}.", DebugLevel);
                    }

                    return null;
                }
                catch(Exception e)
                {
                    Core.Logger.LogWarning($"Failed to deserialize Udon Program due to an exception:\n{e}.", DebugLevel);
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

        private static int InitializeLogging()
        {
            int hashCode = typeof(SerializedUdonProgramAsset).GetHashCode();
            if(Core.Logger.DebugLevelIsDescribed(hashCode))
            {
                return hashCode;
            }

            Core.Logger.DescribeDebugLevel(hashCode, "SerializedUdonProgramAsset", Core.Logger.Color.blue);
            Core.Logger.AddDebugLevel(hashCode);
            return hashCode;
        }

        private void OnDestroy()
        {
            serializedProgramCompressedBytes = null;
            serializedProgramBytesString = null;
            _serializationCache = null;
            programUnityEngineObjects = null;
            _heapCopyDeserializationContextThreadLocal?.Value?.Reset();
            _heapCopyDeserializationContextThreadLocal?.Dispose();
        }
    }
}
