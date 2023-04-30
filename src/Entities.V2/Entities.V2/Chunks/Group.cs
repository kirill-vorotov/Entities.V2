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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace kv.Entities.V2
{
    public class Group
    {
        public struct Entry
        {
            public Chunk? Chunk;
            public int ChunkId;
            public bool IsAvailable;
            public int NextAvailableChunkId;
            public int PrevAvailableChunkId;

            public Entry(Chunk? chunk, int chunkId, bool isAvailable, int nextAvailableChunkId, int prevAvailableChunkId)
            {
                Chunk = chunk;
                ChunkId = chunkId;
                IsAvailable = isAvailable;
                NextAvailableChunkId = nextAvailableChunkId;
                PrevAvailableChunkId = prevAvailableChunkId;
            }
        }
        
        public const int DefaultBufferSize = 1024 * 16;
        
        public readonly TypeMap<TypeInfo> AllTypes;
        public readonly BitMask BitMask;

        public readonly MemoryPool<byte> MemoryPool;
        public readonly IComponentArrayPool ComponentArrayPool;
        
        public readonly TypeInfo[] GroupComponentTypes;
        public readonly int[] ArrayIndexLookup;

        public readonly int BufferSize;
        public readonly int Capacity;

        private int _lastDestroyedChunkId = -1;
        private readonly List<int> _chunkIndices = new();
        private readonly List<Entry> _entries = new();
        
        private int _tailAvailableChunkId = -1;

        public Group(TypeMap<TypeInfo> allTypes, BitMask bitMask, MemoryPool<byte> memoryPool, IComponentArrayPool componentArrayPool, int bufferSize = DefaultBufferSize)
        {
            AllTypes = allTypes;
            BitMask = bitMask;
            MemoryPool = memoryPool;
            ComponentArrayPool = componentArrayPool;

            var popCount = bitMask.PopCount();
            GroupComponentTypes = new TypeInfo[popCount];

            ArrayIndexLookup = new int[allTypes.Entries.Length];

            BufferSize = bufferSize;

            var typeIndex = 0;
            var totalUnmanagedComponentsSize = 0;
            var maxUnmanagedComponentAlignment = 0;
            var totalUnmanagedComponentAlignment = 0;

            for (var i = 0; i < allTypes.Entries.Length; i++)
            {
                if (!allTypes.Entries[i].HasValue)
                {
                    continue;
                }
                
                if (!BitMask[i])
                {
                    ArrayIndexLookup[i] = -1;
                    continue;
                }

                var typeInfo = allTypes.Entries[i].Value;
                GroupComponentTypes[typeIndex] = typeInfo;

                if (typeInfo is { IsUnmanaged: true, IsZeroSized: false })
                {
                    ArrayIndexLookup[i] = -1;
                    totalUnmanagedComponentsSize += typeInfo.Size;
                    totalUnmanagedComponentAlignment += typeInfo.Alignment;
                    
                    if (typeInfo.Alignment > maxUnmanagedComponentAlignment)
                    {
                        maxUnmanagedComponentAlignment = typeInfo.Alignment;
                    }
                }
                else if (!typeInfo.IsUnmanaged)
                {
                    ArrayIndexLookup[i] = typeIndex;
                }
                
                typeIndex++;
            }

            if (totalUnmanagedComponentsSize > 0)
            {
                Capacity = (bufferSize - totalUnmanagedComponentAlignment) / totalUnmanagedComponentsSize;
                
                var offset = 0;
                for (var i = 0; i < GroupComponentTypes.Length; i++)
                {
                    var typeInfo = GroupComponentTypes[i];
                    if (!typeInfo.IsUnmanaged || typeInfo.IsZeroSized)
                    {
                        continue;
                    }

                    var componentsSize = typeInfo.Size * Capacity;
                    var alignment = typeInfo.Alignment;

                    if (offset % alignment > 0)
                    {
                        var tmp = offset / alignment + 1;
                        offset = tmp * alignment;
                    }

                    ArrayIndexLookup[i] = offset;
                    offset += componentsSize;
                }
            }
            else
            {
                Capacity = bufferSize / Unsafe.SizeOf<IntPtr>();
            }
        }

        [PublicAPI]
        public Chunk GetChunk(int chunkId, out bool isAvailable)
        {
            Debug.Assert(chunkId >= 0 && chunkId < _chunkIndices.Count);
            
            var chunkIndex = _chunkIndices[chunkId];
            Debug.Assert(chunkIndex >= 0 && chunkIndex < _entries.Count);
            
            var entry = _entries[chunkIndex];
            isAvailable = entry.IsAvailable;
            return entry.Chunk!;
        }

        [PublicAPI]
        public bool TryGetAvailableChunk(out int chunkId, out Chunk? chunk)
        {
            chunkId = -1;
            chunk = default;
            
            if (_tailAvailableChunkId == -1)
            {
                return false;
            }

            chunkId = _tailAvailableChunkId;
            var entryIndex = _chunkIndices[chunkId];
            var entry = _entries[entryIndex];

            chunk = entry.Chunk;
            return entry.Chunk is not null;
        }

        [PublicAPI]
        public void CreateEntity(Entity entity, out int chunkId, out Chunk chunk, out int indexInChunk)
        {
            indexInChunk = -1;

            if (!TryGetAvailableChunk(out chunkId, out chunk!))
            {
                chunk = CreateChunk(out chunkId);
            }

            Debug.Assert(!chunk.IsFull);

            indexInChunk = chunk.Count;
            chunk.Entities[indexInChunk] = entity;
            chunk.Count++;

            if (chunk.IsFull)
            {
                MakeChunkUnavailable(chunkId);
            }
        }

        [PublicAPI]
        public void DestroyEntity(int chunkId, int indexInChunk)
        {
            var chunk = GetChunk(chunkId, out _);
            
            Debug.Assert(!chunk.IsEmpty);
            chunk.RemoveAt(indexInChunk);

            if (chunk.Count * 3 == chunk.Capacity * 2)
            {
                MakeChunkAvailable(chunkId);
            }
        }

        [PublicAPI]
        public static void CopyEntity(Group src, int srcChunkId, int srcIndexInChunk, Group dst, out int dstChunkId, out Chunk dstChunk, out int dstIndexInChunk)
        {
            var srcChunk = src.GetChunk(srcChunkId, out _);
            
            Debug.Assert(!srcChunk.IsEmpty);

            if (!dst.TryGetAvailableChunk(out dstChunkId, out dstChunk!))
            {
                dstChunk = dst.CreateChunk(out dstChunkId);
            }
            
            Debug.Assert(!dstChunk.IsFull);

            dstIndexInChunk = dstChunk.Count;
            srcChunk.CopyTo(srcIndexInChunk, dstChunk, dstIndexInChunk);
            dstChunk.Count++;

            if (dstChunk.IsFull)
            {
                dst.MakeChunkUnavailable(dstChunkId);
            }
            
            srcChunk.RemoveAt(srcIndexInChunk);
            
            if (srcChunk.Count * 3 == srcChunk.Capacity * 2)
            {
                src.MakeChunkAvailable(srcChunkId);
            }
        }

        [PublicAPI]
        public Chunk CreateChunk(out int chunkId)
        {
            Entry entry;
            chunkId = -1;
            var memoryOwner = MemoryPool.Rent(BufferSize);
            
            Chunk chunk = new(AllTypes, GroupComponentTypes, memoryOwner, ComponentArrayPool, ArrayIndexLookup, Capacity);
            
            if (_lastDestroyedChunkId == -1)
            {
                chunkId = _chunkIndices.Count;
                entry = new Entry(chunk, chunkId, false, -1, -1);
                _entries.Add(entry);
                var chunkIndex = _entries.Count - 1;
                
                _chunkIndices.Add(chunkIndex);
            }
            else
            {
                chunkId = _lastDestroyedChunkId;
                entry = new Entry(chunk, chunkId, false, -1, -1);
                _entries.Add(entry);
                var chunkIndex = _entries.Count - 1;

                _lastDestroyedChunkId = _chunkIndices[_lastDestroyedChunkId];
                _chunkIndices[chunkId] = chunkIndex;
            }

            MakeChunkAvailable(chunkId);
            return entry.Chunk!;
        }

        [PublicAPI]
        public void DestroyChunk(int chunkId)
        {
            Debug.Assert(chunkId >= 0 || chunkId < _chunkIndices.Count);
            
            MakeChunkUnavailable(chunkId);
            
            var destroyedChunkIndex = _chunkIndices[chunkId];
            var entry = _entries[destroyedChunkIndex];
            
            Debug.Assert(entry.Chunk is { IsEmpty: true });

            entry.Chunk.Dispose();
            
            if (destroyedChunkIndex < _entries.Count - 1)
            {
                var lastEntry = _entries[^1];
                
                _entries[destroyedChunkIndex] = lastEntry;
                _entries[^1] = new Entry(default, -1, false, -1, -1);
                _chunkIndices[lastEntry.ChunkId] = destroyedChunkIndex;
            }

            _entries.RemoveAt(_entries.Count - 1);
            _chunkIndices[chunkId] = _lastDestroyedChunkId;
            _lastDestroyedChunkId = chunkId;
        }

        [PublicAPI]
        public IEnumerable<Chunk> GetAvailableChunks()
        {
            if (_tailAvailableChunkId == -1)
            {
                yield break;
            }

            var currentId = _tailAvailableChunkId;

            while (currentId != -1)
            {
                var entryIndex = _chunkIndices[currentId];
                var entry = _entries[entryIndex];
                if (entry.Chunk is not null)
                {
                    yield return entry.Chunk;
                }

                currentId = entry.NextAvailableChunkId;
            }
        }

        internal IEnumerable<Entry> Entries => _entries;
        
        internal void Clear()
        {
            _chunkIndices.Clear();
            _lastDestroyedChunkId = -1;
            foreach (var entry in _entries)
            {
                entry.Chunk?.Dispose();
            }
            
            _entries.Clear();
        }

        private void MakeChunkAvailable(int chunkId)
        {
            Debug.Assert(chunkId >= 0 || chunkId < _chunkIndices.Count);
            
            var entryIndex = _chunkIndices[chunkId];
            Debug.Assert(entryIndex >= 0 && entryIndex < _entries.Count);
            
            var entry = _entries[entryIndex];

            if (entry.IsAvailable)
            {
                return;
            }

            if (_tailAvailableChunkId >= 0)
            {
                var tailEntryIndex = _chunkIndices[_tailAvailableChunkId];
                var tailEntry = _entries[tailEntryIndex];
                
                tailEntry.NextAvailableChunkId = chunkId;
                _entries[tailEntryIndex] = tailEntry;
            }
            
            entry.IsAvailable = true;
            entry.PrevAvailableChunkId = _tailAvailableChunkId;
            entry.NextAvailableChunkId = -1;
            _entries[entryIndex] = entry;

            _tailAvailableChunkId = chunkId;
        }

        private void MakeChunkUnavailable(int chunkId)
        {
            var entryIndex = _chunkIndices[chunkId];
            var entry = _entries[entryIndex];

            if (!entry.IsAvailable)
            {
                return;
            }

            var prevEntryIndex = entry.PrevAvailableChunkId >= 0 ? _chunkIndices[entry.PrevAvailableChunkId] : -1;
            var nextEntryIndex = entry.NextAvailableChunkId >= 0 ? _chunkIndices[entry.NextAvailableChunkId] : -1;

            var isTailEntry = nextEntryIndex == -1;
            var isHeadEntry = prevEntryIndex == -1;
            
            if (!isTailEntry)
            {
                var nextEntry = _entries[nextEntryIndex];
                nextEntry.PrevAvailableChunkId = entry.PrevAvailableChunkId;
                _entries[nextEntryIndex] = nextEntry;
            }
            else
            {
                _tailAvailableChunkId = entry.PrevAvailableChunkId;
            }
            
            if (!isHeadEntry)
            {
                var prevEntry = _entries[prevEntryIndex];
                prevEntry.NextAvailableChunkId = entry.NextAvailableChunkId;
                _entries[prevEntryIndex] = prevEntry;
            }

            entry.PrevAvailableChunkId = -1;
            entry.NextAvailableChunkId = -1;
            entry.IsAvailable = false;

            _entries[entryIndex] = entry;
        }
    }
}