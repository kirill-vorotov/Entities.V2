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
using System.Buffers;

namespace kv.Entities.V2
{
    internal class ManagedComponentList<T> : IComponentList where T : IEntityComponent
    {
        public const int InitialCapacity = 16;

        public ArrayPool<T> ArrayPool;
        public T[] Array;
        
        public TypeInfo TypeInfo { get; }
        public int Capacity { get; private set; }
        public int Count { get; private set; }

        public ManagedComponentList(TypeInfo typeInfo, ArrayPool<T> arrayPool)
        {
            TypeInfo = typeInfo;
            ArrayPool = arrayPool;

            Array = ArrayPool.Rent(InitialCapacity);
            Capacity = InitialCapacity;
            Count = 0;
        }

        public void Add(T value)
        {
            Debug.Assert(TypeInfo is { IsUnmanaged: false });

            if (Count == Capacity)
            {
                Capacity <<= 1;
                if (Capacity == 0)
                {
                    Capacity = InitialCapacity;
                }

                var newArray = ArrayPool.Rent(Capacity);
                Array.CopyTo(newArray, 0);
                
                ArrayPool.Return(Array);
                Array = newArray;
            }

            Array[Count] = value;
            Count++;
        }

        public void CopyComponentTo(int srcIndex, Chunk chunk, int dstIndexInChunk)
        {
            Debug.Assert(TypeInfo is { IsUnmanaged: false });

            var arrayIndex = chunk.ArrayIndexLookup[TypeInfo.Id];
            if (arrayIndex == -1)
            {
                return;
            }

            var dstArray = chunk.ManagedComponents;
            System.Array.Copy(Array, srcIndex, dstArray, dstIndexInChunk, 1);
        }
        
        public void Dispose()
        {
            ArrayPool.Return(Array);
            Array = System.Array.Empty<T>();
            Capacity = 0;
            Count = 0;
        }
    }
}