//-----------------------------------------------------------------------
// <copyright file="MethodInfoFormatter.cs" company="Sirenix IVS">
// Copyright (c) 2018 Sirenix IVS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

using VRC.Udon.Serialization.OdinSerializer;

[assembly: RegisterFormatter(typeof(MethodInfoFormatter<>))]

namespace VRC.Udon.Serialization.OdinSerializer
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Utilities;

    /// <summary>
    /// Custom formatter for MethodInfo, since Unity Mono's MethodInfo ISerializable implementation will often crash if the method no longer exists upon deserialization.
    /// </summary>
    /// <seealso cref="BaseFormatter{T}" />
    public sealed class MethodInfoFormatter<T> : BaseFormatter<T>
        where T : MethodInfo
    {
        /// <summary>
        /// Provides the actual implementation for deserializing a value of type <see cref="!:T" />.
        /// </summary>
        /// <param name="value">The uninitialized value to serialize into. This value will have been created earlier using <see cref="M:OdinSerializer.BaseFormatter`1.GetUninitializedObject" />.</param>
        /// <param name="reader">The reader to deserialize with.</param>
        protected override void DeserializeImplementation(ref T value, IDataReader reader)
        {
            reader.Context.Config.DebugContext.LogWarning("MethodInfo deserialization has been removed for security.");
        }

        /// <summary>
        /// Provides the actual implementation for serializing a value of type <see cref="!:T" />.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <param name="writer">The writer to serialize with.</param>
        protected override void SerializeImplementation(ref T value, IDataWriter writer)
        {
            writer.Context.Config.DebugContext.LogWarning("MethodInfo serialization has been removed for security.");
        }

        /// <summary>
        /// Get an uninitialized object of type <see cref="!:T" />. WARNING: If you override this and return null, the object's ID will not be automatically registered and its OnDeserializing callbacks will not be automatically called, before deserialization begins.
        /// You will have to call <see cref="M:OdinSerializer.BaseFormatter`1.RegisterReferenceID(`0,OdinSerializer.IDataReader)" /> and <see cref="M:OdinSerializer.BaseFormatter`1.InvokeOnDeserializingCallbacks(`0,OdinSerializer.DeserializationContext)" /> immediately after creating the object yourself during deserialization.
        /// </summary>
        /// <returns>
        /// An uninitialized object of type <see cref="!:T" />.
        /// </returns>
        protected override T GetUninitializedObject()
        {
            return null;
        }
    }
}