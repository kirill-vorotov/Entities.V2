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

namespace kv.Entities.V2
{
    public class Group
    {
        public const int DefaultBufferSize = 1024 * 16;
        
        public readonly TypeMap<TypeInfo> AllTypes;
        public readonly BitMask BitMask;

        public readonly MemoryPool<byte> MemoryPool;
        public readonly IComponentArrayPool ComponentArrayPool;
        
        public readonly TypeInfo[] GroupComponentTypes;
        public readonly int[] ArrayIndexLookup;

        public readonly int BufferSize;
        public readonly int Capacity;

        public Group(TypeMap<TypeInfo> allTypes, BitMask bitMask, MemoryPool<byte> memoryPool, IComponentArrayPool componentArrayPool, int bufferSize = DefaultBufferSize)
        {
            AllTypes = allTypes;
            BitMask = bitMask;
            MemoryPool = memoryPool;
            ComponentArrayPool = componentArrayPool;

            var popCount = bitMask.PopCount();
            GroupComponentTypes = new TypeInfo[popCount];

            ArrayIndexLookup = new int[allTypes.Count];

            BufferSize = bufferSize;

            // TODO: calculate unmanaged components offsets.
            // TODO: set managed arrays indices.
            // TODO: calculate capacity.
        }

        public void CreateEntity(out int chunkId, out int indexInChunk)
        {
            throw new System.NotImplementedException();
        }

        public void DestroyEntity(int chunkId, int indexInChunk)
        {
            throw new System.NotImplementedException();
        }

        public static void CopyEntity(Group src, int srcChunkId, int srcIndexInChunk, Group dst, out int dstChunkId, out int dstIndexInChunk)
        {
            throw new System.NotImplementedException();
        }

        private Chunk CreateChunk(out int chunkId)
        {
            chunkId = -1;
            return new Chunk(AllTypes, GroupComponentTypes, MemoryPool.Rent(BufferSize), ComponentArrayPool, ArrayIndexLookup, Capacity);
            throw new System.NotImplementedException();
        }

        private void DestroyChunk(int chunkId)
        {
            var chunk = new Chunk(AllTypes, GroupComponentTypes, MemoryPool.Rent(BufferSize), ComponentArrayPool, ArrayIndexLookup, Capacity);
            chunk.Dispose();
            throw new System.NotImplementedException();
        }
    }
}