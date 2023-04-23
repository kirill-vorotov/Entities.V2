/*
 * MIT License
 *
 * Copyright (c) 2023 Kirill Vorotov
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

#nullable enable
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace kv.Entities.V2
{
    public class Chunk : IDisposable
    {
        public readonly TypeMap<TypeInfo> AllTypes;
        public readonly TypeInfo[] GroupComponentTypes;
        public readonly IMemoryOwner<byte> BufferMemoryOwner;
        public readonly IComponentArrayPool ComponentArrayPool;
        public readonly int[] ArrayIndexLookup;
        public readonly int Capacity;
        
        public Entity[] Entities;
        public Memory<byte> Buffer;
        public Array?[] ManagedComponents;
        
        [PublicAPI]
        public int Count { get; internal set; }

        [PublicAPI]
        public bool IsEmpty => Count == 0;

        [PublicAPI]
        public bool IsFull => Count == Capacity;
        
        public Chunk(TypeMap<TypeInfo> allTypes, TypeInfo[] groupComponentTypes, IMemoryOwner<byte> bufferMemoryOwner, IComponentArrayPool componentArrayPool, int[] arrayIndexLookup, int capacity)
        {
            AllTypes = allTypes;
            GroupComponentTypes = groupComponentTypes;
            BufferMemoryOwner = bufferMemoryOwner;
            ComponentArrayPool = componentArrayPool;
            ArrayIndexLookup = arrayIndexLookup;
            Capacity = capacity;

            Entities = new Entity[capacity];
            Buffer = bufferMemoryOwner.Memory;
            ManagedComponents = new Array[GroupComponentTypes.Length];

            Count = 0;

            for (var i = 0; i < groupComponentTypes.Length; i++)
            {
                var typeInfo = groupComponentTypes[i];

                if (typeInfo.IsZeroSized)
                {
                    continue;
                }

                if (!typeInfo.IsUnmanaged)
                {
                    ManagedComponents[i] = componentArrayPool.Rent(typeInfo, capacity);
                }
            }
        }

        public void Dispose()
        {
            BufferMemoryOwner.Dispose();
            Buffer = Memory<byte>.Empty;

            for (var i = 0; i < ManagedComponents.Length; i++)
            {
                var componentArray = ManagedComponents[i];
                
                if (componentArray is null)
                {
                    continue;
                }

                var typeInfo = GroupComponentTypes[i];
                ComponentArrayPool.Return(typeInfo, componentArray);
                ManagedComponents[i] = null;
            }

            ManagedComponents = null!;
        }

        [PublicAPI]
        public bool HasComponent<T>() where T : IEntityComponent
        {
            if (!AllTypes.TryGetValue<T>(out var typeInfo))
            {
                return false;
            }

            return ArrayIndexLookup[typeInfo.Id] >= 0;
        }

        [PublicAPI]
        public Span<TComponent> GetComponents<TComponent>() where TComponent : unmanaged, IEntityComponent
        {
            if (Count == 0)
            {
                return Span<TComponent>.Empty;
            }
            
            if (!AllTypes.TryGetValue<TComponent>(out var typeInfo))
            {
                return Span<TComponent>.Empty;
            }
            
            Debug.Assert(typeInfo is { IsUnmanaged: true });

            if (typeInfo.IsZeroSized)
            {
                return Span<TComponent>.Empty;
            }

            var offset = ArrayIndexLookup[typeInfo.Id];
            if (offset < 0)
            {
                return Span<TComponent>.Empty;
            }

            var span = Buffer.Span.Slice(offset, Count * typeInfo.Size);
            return MemoryMarshal.Cast<byte, TComponent>(span);
        }
        
        [PublicAPI]
        public Span<TComponent> GetManagedComponents<TComponent>() where TComponent : IEntityComponent
        {
            if (Count == 0)
            {
                return Span<TComponent>.Empty;
            }
            
            if (!AllTypes.TryGetValue<TComponent>(out var typeInfo))
            {
                return Span<TComponent>.Empty;
            }
            
            Debug.Assert(typeInfo is { IsUnmanaged: false });

            var arrayIndex = ArrayIndexLookup[typeInfo.Id];
            if (arrayIndex < 0)
            {
                return Span<TComponent>.Empty;
            }

            var array = ManagedComponents[arrayIndex];
            return Unsafe.As<TComponent[]>(array).AsSpan(0, Count);
        }

        [PublicAPI]
        public void RemoveAt(int indexInChunk)
        {
            Debug.Assert(indexInChunk >= 0 && indexInChunk < Count);

            if (indexInChunk == Count - 1)
            {
                // Remove last entity
                foreach (var typeInfo in GroupComponentTypes)
                {
                    if (typeInfo.IsUnmanaged)
                    {
                        continue;
                    }

                    var arrayIndex = ArrayIndexLookup[typeInfo.Id];
                    var array = ManagedComponents[arrayIndex];
                    Debug.Assert(array is not null);
                    Array.Clear(array, indexInChunk, 1);
                }
                
                Entities[indexInChunk] = Entity.CreateInvalid();
                Count--;
                return;
            }
            
            var lastIndexInChunk = Count - 1;

            foreach (var typeInfo in GroupComponentTypes)
            {
                if (typeInfo.IsZeroSized)
                {
                    continue;
                }

                var offsetOrArrayIndex = ArrayIndexLookup[typeInfo.Id];
                
                if (typeInfo.IsUnmanaged)
                {
                    var dstSpan = Buffer.Span.Slice(offsetOrArrayIndex + indexInChunk * typeInfo.Size, typeInfo.Size);
                    var srcSpan = Buffer.Span.Slice(offsetOrArrayIndex + lastIndexInChunk * typeInfo.Size, typeInfo.Size);
                    srcSpan.CopyTo(dstSpan);
                }
                else
                {
                    var array = ManagedComponents[offsetOrArrayIndex];
                    Debug.Assert(array is not null);
                    Array.Copy(array, lastIndexInChunk, array, indexInChunk, 1);
                    Array.Clear(array, lastIndexInChunk, 1);
                }
            }

            Entities[indexInChunk] = Entities[lastIndexInChunk];
            Entities[lastIndexInChunk] = Entity.CreateInvalid();
            Count--;
        }

        [PublicAPI]
        public void CopyTo(int srcIndexInChunk, Chunk dst, int dstIndexInChunk)
        {
            Debug.Assert(dst.Count < dst.Capacity);

            foreach (var typeInfo in GroupComponentTypes)
            {
                var srcOffsetOrArrayIndex = ArrayIndexLookup[typeInfo.Id];
                var dstOffsetOrArrayIndex = dst.ArrayIndexLookup[typeInfo.Id];

                if (typeInfo.IsZeroSized || dstOffsetOrArrayIndex < 0)
                {
                    // nothing to copy
                    continue;
                }

                if (typeInfo.IsUnmanaged)
                {
                    var srcSpan = Buffer.Span.Slice(srcOffsetOrArrayIndex + srcIndexInChunk * typeInfo.Size, typeInfo.Size);
                    var dstSpan = dst.Buffer.Span.Slice(dstOffsetOrArrayIndex + dstIndexInChunk * typeInfo.Size, typeInfo.Size);
                    srcSpan.CopyTo(dstSpan);
                }
                else
                {
                    var srcArray = ManagedComponents[srcOffsetOrArrayIndex];
                    var dstArray = dst.ManagedComponents[dstOffsetOrArrayIndex];
                    Debug.Assert(srcArray is not null);
                    Debug.Assert(dstArray is not null);
                    Array.Copy(srcArray, srcIndexInChunk, dstArray, dstIndexInChunk, 1);
                }

                dst.Entities[dstIndexInChunk] = Entities[srcIndexInChunk];
            }
        }

        [PublicAPI]
        public void Clear()
        {
            foreach (var componentArray in ManagedComponents)
            {
                if (componentArray is null)
                {
                    continue;
                }

                Array.Clear(componentArray, 0, Count);
            }
            
            Buffer.Span.Clear();
            Entities.AsSpan().Fill(Entity.CreateInvalid());
            Count = 0;
        }
    }
}