using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;
using VRC.Udon.Serialization.Formatters;

[assembly: RegisterFormatter(typeof(UdonProgramFormatter))]

namespace VRC.Udon.Serialization.Formatters
{
    public sealed class UdonProgramFormatter : BaseFormatter<UdonProgram>
    {
        private static readonly Serializer<byte[]> _byteArrayReaderWriter = Serializer.Get<byte[]>();
        private static readonly Serializer<IUdonHeap> _udonHeapReaderWriter = Serializer.Get<IUdonHeap>();
        private static readonly Serializer<IUdonSymbolTable> _udonSymbolTableReaderWriter = Serializer.Get<IUdonSymbolTable>();
        private static readonly Serializer<IUdonSyncMetadataTable> _udonSyncMetadataTableReaderWriter = Serializer.Get<IUdonSyncMetadataTable>();

        protected override UdonProgram GetUninitializedObject()
        {
            return null;
        }

        // ReSharper disable once RedundantAssignment
        protected override void DeserializeImplementation(ref UdonProgram value, IDataReader reader)
        {
            reader.ReadString(out string instructionSetIdentifier);
            reader.ReadInt32(out int instructionSetVersion);
            byte[] byteCode = _byteArrayReaderWriter.ReadValue(reader);
            IUdonHeap heap = _udonHeapReaderWriter.ReadValue(reader);
            IUdonSymbolTable entryPoints = _udonSymbolTableReaderWriter.ReadValue(reader);
            IUdonSymbolTable symbolTable = _udonSymbolTableReaderWriter.ReadValue(reader);
            IUdonSyncMetadataTable syncMetadataTable = _udonSyncMetadataTableReaderWriter.ReadValue(reader);

            if(!reader.ReadInt32(out int updateOrder))
            {
                updateOrder = 0;
            }

            value = new UdonProgram(instructionSetIdentifier, instructionSetVersion, byteCode, heap, entryPoints, symbolTable, syncMetadataTable, updateOrder);

            RegisterReferenceID(value, reader);
            InvokeOnDeserializingCallbacks(ref value, reader.Context);
        }

        protected override void SerializeImplementation(ref UdonProgram value, IDataWriter writer)
        {
            writer.WriteString("InstructionSetIdentifier", value.InstructionSetIdentifier);
            writer.WriteInt32("InstructionSetVersion", value.InstructionSetVersion);
            _byteArrayReaderWriter.WriteValue("ByteCode", value.ByteCode, writer);
            _udonHeapReaderWriter.WriteValue("Heap", value.Heap, writer);
            _udonSymbolTableReaderWriter.WriteValue("EntryPoints", value.EntryPoints, writer);
            _udonSymbolTableReaderWriter.WriteValue("SymbolTable", value.SymbolTable, writer);
            _udonSyncMetadataTableReaderWriter.WriteValue("SyncMetadataTable", value.SyncMetadataTable, writer);
            writer.WriteInt32("UpdateOrder", value.UpdateOrder);
        }
    }
}
