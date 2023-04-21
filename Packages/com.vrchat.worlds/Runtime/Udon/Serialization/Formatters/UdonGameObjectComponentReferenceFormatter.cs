using System;
using VRC.Udon.Common;
using VRC.Udon.Serialization.OdinSerializer;
using VRC.Udon.Serialization.Formatters;

[assembly: RegisterFormatter(typeof(UdonGameObjectComponentReferenceFormatter))]

namespace VRC.Udon.Serialization.Formatters
{
    public sealed class UdonGameObjectComponentReferenceFormatter : BaseFormatter<UdonGameObjectComponentHeapReference>
    {
        private static readonly Serializer<Type> _typeSerializer = Serializer.Get<Type>();

        protected override UdonGameObjectComponentHeapReference GetUninitializedObject()
        {
            return null;
        }

        // ReSharper disable once RedundantAssignment
        protected override void DeserializeImplementation(ref UdonGameObjectComponentHeapReference value, IDataReader reader)
        {
            Type type = _typeSerializer.ReadValue(reader);

            value = new UdonGameObjectComponentHeapReference(type);

            RegisterReferenceID(value, reader);
            InvokeOnDeserializingCallbacks(ref value, reader.Context);
        }

        protected override void SerializeImplementation(ref UdonGameObjectComponentHeapReference value, IDataWriter writer)
        {
            _typeSerializer.WriteValue(value.type, writer);
        }
    }
}
