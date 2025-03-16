//-----------------------------------------------------------------------
// <copyright file="ArrayFormatterLocator.cs" company="Sirenix IVS">
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

using System.Collections.Generic;
using VRC.Udon.Serialization.OdinSerializer;
using VRC.Udon.Serialization.OdinSerializer.Utilities;

[assembly: RegisterFormatterLocator(typeof(ArrayFormatterLocator), -80)]

namespace VRC.Udon.Serialization.OdinSerializer
{
    using System;

    internal class ArrayFormatterLocator : IFormatterLocator
    {
        // VRC Unity John: PER-818 - Introduce a type-keyed cache of created formatters to avoid creating duplicates (which was happening previously).
        private static readonly Dictionary<Type, IFormatter> FormatterInstances = new(FastTypeComparer.Instance);
        // VRC Unity John: PER-818 - end
        
        public bool TryGetFormatter(Type type, FormatterLocationStep step, ISerializationPolicy policy, out IFormatter formatter)
        {
            if (!type.IsArray)
            {
                formatter = null;
                return false;
            }

            // VRC Unity John: PER-818 - Introduce a type-keyed cache of created formatters to avoid creating duplicates (which was happening previously).
            // If we've serialized this type before, we'll have a cached formatter
            if (FormatterInstances.TryGetValue(type, out formatter))
            {
                return true;
            }
            // VRC Unity John: PER-818 - end
            
            var elementType = type.GetElementType();

            if (type.GetArrayRank() == 1)
            {
                if (FormatterUtilities.IsPrimitiveArrayType(elementType))
                {
                    #if false //vrc security patch
                    
                    try
                    {
                        formatter = (IFormatter)Activator.CreateInstance(typeof(PrimitiveArrayFormatter<>).MakeGenericType(elementType));
                    }
                    catch (Exception ex)
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        if (allowWeakFallbackFormatters && (ex is ExecutionEngineException || ex.GetBaseException() is ExecutionEngineException))
#pragma warning restore CS0618 // Type or member is obsolete
                        {
                            formatter = new WeakPrimitiveArrayFormatter(type, elementType);
                        }
                        else throw;
                    }
                    #endif
                    formatter = (IFormatter)Activator.CreateInstance(typeof(PrimitiveArrayFormatter<>).MakeGenericType(elementType));
                }
                else
                {
                    #if false //vrc security patch
                    try
                    {
                        formatter = (IFormatter)Activator.CreateInstance(typeof(ArrayFormatter<>).MakeGenericType(elementType));
                    }
                    catch (Exception ex)
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        if (allowWeakFallbackFormatters && (ex is ExecutionEngineException || ex.GetBaseException() is ExecutionEngineException))
#pragma warning restore CS0618 // Type or member is obsolete
                        {
                            formatter = new WeakArrayFormatter(type, elementType);
                        }
                        else throw;
                    }
                    #endif
                    formatter = (IFormatter)Activator.CreateInstance(typeof(ArrayFormatter<>).MakeGenericType(elementType));
                }
            }
            else
            {
                #if false //vrc security patch
                
                try
                {
                    formatter = (IFormatter)Activator.CreateInstance(typeof(MultiDimensionalArrayFormatter<,>).MakeGenericType(type, type.GetElementType()));
                }
                catch (Exception ex)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    if (allowWeakFallbackFormatters && (ex is ExecutionEngineException || ex.GetBaseException() is ExecutionEngineException))
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                        formatter = new WeakMultiDimensionalArrayFormatter(type, elementType);
                    }
                    else throw;
                }
                #endif
                formatter = (IFormatter)Activator.CreateInstance(typeof(MultiDimensionalArrayFormatter<,>).MakeGenericType(type, type.GetElementType()));
            }

            // VRC Unity John: PER-818 - Introduce a type-keyed cache of created formatters to avoid creating duplicates (which was happening previously).
            FormatterInstances.Add(type, formatter);
            // VRC Unity John: PER-818 - end

            return true;
        }
    }
}
