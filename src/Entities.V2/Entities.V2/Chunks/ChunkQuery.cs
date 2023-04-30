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
using System.Collections.Generic;
using JetBrains.Annotations;

namespace kv.Entities.V2
{
    public sealed class ChunkQuery
    {
        public readonly EntityRepo EntityRepo;
        public readonly TypeMap<TypeInfo> AllTypes;

        public BitMask All;
        public BitMask Any;
        public BitMask None;

        public readonly List<Group> Groups = new();
        public readonly List<Chunk> Chunks = new();

        public ChunkQuery(EntityRepo entityRepo, TypeMap<TypeInfo> allTypes)
        {
            EntityRepo = entityRepo;
            AllTypes = allTypes;

            var bufferSize = BitMask.GetBufferSize(allTypes.Entries.Length);

            All = new BitMask(new ulong[bufferSize]);
            Any = new BitMask(new ulong[bufferSize]);
            None = new BitMask(new ulong[bufferSize]);
        }

        [PublicAPI]
        public List<Chunk>.Enumerator GetEnumerator() => Chunks.GetEnumerator();

        [PublicAPI]
        public ChunkQuery WithAll<T>() where T : IEntityComponent
        {
            if (!AllTypes.TryGetValue<T>(out var typeInfo))
            {
                return this;
            }

            All[typeInfo.Id] = true;
            return this;
        }

        [PublicAPI]
        public ChunkQuery WithAny<T>() where T : IEntityComponent
        {
            if (!AllTypes.TryGetValue<T>(out var typeInfo))
            {
                return this;
            }

            Any[typeInfo.Id] = true;
            return this;
        }

        [PublicAPI]
        public ChunkQuery WithNone<T>() where T : IEntityComponent
        {
            if (!AllTypes.TryGetValue<T>(out var typeInfo))
            {
                return this;
            }

            None[typeInfo.Id] = true;
            return this;
        }

        [PublicAPI]
        public ChunkQuery Update()
        {
            Groups.Clear();
            Chunks.Clear();
            
            foreach (var group in EntityRepo.Groups)
            {
                if (!Test(group.BitMask, All, Any, None))
                {
                    continue;
                }
                
                Groups.Add(group);
                foreach (var entry in group.Entries)
                {
                    if (entry.Chunk is null || entry.Chunk.IsEmpty)
                    {
                        continue;
                    }
                    
                    Chunks.Add(entry.Chunk);
                }
            }

            return this;
        }

        private static bool Test(BitMask targetMask, BitMask allMask, BitMask anyMask, BitMask noneMask)
        {
            if (targetMask.Buffer.Length != allMask.Buffer.Length)
            {
                return false;
            }

            var any = 0UL;
            var foundAny = false;

            var targetSpan = targetMask.Buffer.Span;
            var allSpan = allMask.Buffer.Span;
            var anySpan = anyMask.Buffer.Span;
            var noneSpan = noneMask.Buffer.Span;

            for (var i = 0; i < targetSpan.Length; i++)
            {
                var targetValue = targetSpan[i];

                var noneValue = noneSpan[i];
                if ((noneValue & targetValue) > 0)
                {
                    return false;
                }

                var allValue = allSpan[i];
                if ((allValue & targetValue) != allValue)
                {
                    return false;
                }

                var anyValue = anySpan[i];
                any |= anyValue;
                if (!foundAny)
                {
                    foundAny = (anyValue & targetValue) > 0;
                }
            }

            return any == 0 || foundAny;
        }
    }
}