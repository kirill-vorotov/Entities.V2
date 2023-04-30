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
using System.Runtime.InteropServices;

namespace kv.Entities.V2
{
    internal class UnmanagedComponentList<T> : IComponentList where T : unmanaged, IEntityComponent
    {
        public const int InitialCapacity = 16;

        public MemoryPool<byte> MemoryPool;
        public IMemoryOwner<byte>? MemoryOwner;
        public Memory<byte> Buffer;
        
        public TypeInfo TypeInfo { get; }
        public int Capacity { get; private set; }
        public int Count { get; private set; }

        public UnmanagedComponentList(TypeInfo typeInfo, MemoryPool<byte> memoryPool)
        {
            TypeInfo = typeInfo;
            MemoryPool = memoryPool;

            if (typeInfo.IsZeroSized)
            {
                MemoryOwner = default;
                Buffer = Memory<byte>.Empty;
            }
            else
            {
                MemoryOwner = memoryPool.Rent(InitialCapacity * typeInfo.Size);
                Buffer = MemoryOwner.Memory;
            }

            Capacity = InitialCapacity;
            Count = 0;
        }

        public void Add(T value)
        {
            Debug.Assert(TypeInfo is { IsUnmanaged: true });

            if (TypeInfo.IsZeroSized)
            {
                return;
            }

            if (Count == Capacity)
            {
                Capacity <<= 1;
                if (Capacity == 0)
                {
                    Capacity = InitialCapacity;
                }
                var newMemoryOwner = MemoryPool.Rent(Capacity);
                Buffer.CopyTo(newMemoryOwner.Memory);

                MemoryOwner?.Dispose();
                MemoryOwner = newMemoryOwner;
                Buffer = newMemoryOwner.Memory;
            }

            var dstSpan = Buffer.Span.Slice(Count * TypeInfo.Size, TypeInfo.Size);
            MemoryMarshal.Write(dstSpan, ref value);
            Count++;
        }

        public void CopyComponentTo(int srcIndex, Chunk chunk, int dstIndexInChunk)
        {
            Debug.Assert(TypeInfo is { IsUnmanaged: true });
            
            if (TypeInfo.IsZeroSized)
            {
                return;
            }

            var dstOffset = chunk.ArrayIndexLookup[TypeInfo.Id];
            if (dstOffset == -1)
            {
                return;
            }
            
            var srcSpan = Buffer.Span.Slice(srcIndex * TypeInfo.Size, TypeInfo.Size);
            var dstSpan = chunk.Buffer.Span.Slice(dstOffset + dstIndexInChunk * TypeInfo.Size, TypeInfo.Size);
            srcSpan.CopyTo(dstSpan);
        }

        public void Dispose()
        {
            MemoryOwner?.Dispose();
            MemoryOwner = null;
            Buffer = Memory<byte>.Empty;
            Capacity = 0;
            Count = 0;
        }
    }
}