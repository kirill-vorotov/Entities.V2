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
using System.Collections.Generic;
using JetBrains.Annotations;

namespace kv.Entities.V2
{
    public class EntityRepo
    {
        public int LastDestroyedEntityId = -1;

        public TypeMap<TypeInfo> AllTypes;
        public MemoryPool<byte> MemoryPool;
        public IComponentArrayPool ComponentArrayPool;
        
        public List<EntityInfo> Entities = new();
        public List<Group> Groups = new();

        public EntityRepo(TypeMap<TypeInfo> allTypes, MemoryPool<byte> memoryPool, IComponentArrayPool componentArrayPool)
        {
            AllTypes = allTypes;
            MemoryPool = memoryPool;
            ComponentArrayPool = componentArrayPool;
        }

        [PublicAPI]
        public bool IsValid(Entity entity) => entity is { Id: >= 0, Version: >= 0 } &&
                                              Entities[entity.Id].Id == entity.Id &&
                                              Entities[entity.Id].Version == entity.Version;

        [PublicAPI]
        public Entity CreateEntity(Group targetGroup, int targetGroupId, out int chunkId, out Chunk chunk, out int indexInChunk)
        {
            Entity entity;
            
            if (LastDestroyedEntityId == -1)
            {
                entity = new Entity(Entities.Count, 0);
                targetGroup.CreateEntity(entity, out chunkId, out chunk, out indexInChunk);
                Entities.Add(new EntityInfo(entity.Id, entity.Version, targetGroupId, chunkId, indexInChunk));
            }
            else
            {
                entity = new Entity(LastDestroyedEntityId, Entities[LastDestroyedEntityId].Version);
                targetGroup.CreateEntity(entity, out chunkId, out chunk, out indexInChunk);

                LastDestroyedEntityId = Entities[LastDestroyedEntityId].Id;
                Entities[entity.Id] = new EntityInfo(entity.Id, entity.Version, targetGroupId, chunkId, indexInChunk);
            }
            
            return entity;
        }

        [PublicAPI]
        public void DestroyEntity(Entity entity)
        {
            Debug.Assert(IsValid(entity));

            var destroyedEntityInfo = Entities[entity.Id];
            var group = Groups[destroyedEntityInfo.GroupId];
            var chunkId = destroyedEntityInfo.ChunkId;
            var chunk = group.GetChunk(chunkId, out _);
            
            Debug.Assert(chunk is not null);

            if (destroyedEntityInfo.IndexInChunk < chunk.Count - 1)
            {
                var lastEntityInChunk = chunk.Entities[chunk.Count - 1];
                group.DestroyEntity(chunkId, destroyedEntityInfo.IndexInChunk);
                Entities[lastEntityInChunk.Id] = new EntityInfo(lastEntityInChunk.Id, lastEntityInChunk.Version,
                    destroyedEntityInfo.GroupId, destroyedEntityInfo.ChunkId, destroyedEntityInfo.IndexInChunk);
            }
            else
            {
                group.DestroyEntity(chunkId, chunk.Count - 1);
            }
            
            var newVersion = Entities[entity.Id].Version + 1;
            Entities[entity.Id] = new EntityInfo(LastDestroyedEntityId, newVersion, -1, -1, -1);
            LastDestroyedEntityId = entity.Id;
        }

        [PublicAPI]
        public void MoveEntity(Entity entity, int dstGroupId, Group dstGroup, out int dstChunkId, out Chunk dstChunk, out int dstIndexInChunk)
        {
            Debug.Assert(IsValid(entity));

            var info = Entities[entity.Id];
            var srcGroup = Groups[info.GroupId];
            var srcChunk = srcGroup.GetChunk(info.ChunkId, out var isAvailable);
            
            Debug.Assert(isAvailable && !srcChunk.IsFull);

            Group.CopyEntity(srcGroup, info.ChunkId, info.IndexInChunk, dstGroup, out dstChunkId, out dstChunk, out dstIndexInChunk);
            Entities[entity.Id] = new EntityInfo(entity.Id, entity.Version, dstGroupId, dstChunkId, dstIndexInChunk);

            if (info.IndexInChunk < srcChunk.Count - 1)
            {
                var lastEntityInChunk = srcChunk.Entities[srcChunk.Count - 1];
                srcChunk.RemoveAt(info.IndexInChunk);
                Entities[lastEntityInChunk.Id] = new EntityInfo(lastEntityInChunk.Id, lastEntityInChunk.Version, info.GroupId, info.ChunkId, info.IndexInChunk);
            }
            else
            {
                srcChunk.RemoveAt(srcChunk.Count - 1);
            }
        }

        [PublicAPI]
        public void Clear()
        {
            foreach (var group in Groups)
            {
                group.Clear();
            }
            
            Entities.Clear();
            LastDestroyedEntityId = -1;
        }
        
        internal bool TryGetGroup(BitMask bitMask, out Group? group, out int groupId)
        {
            group = default;
            groupId = -1;

            for (var index = 0; index < Groups.Count; index++)
            {
                var g = Groups[index];
                if (!g.BitMask.Equals(bitMask))
                {
                    continue;
                }

                group = g;
                groupId = index;
                return true;
            }

            return false;
        }

        internal Group CreateGroup(BitMask bitMask, out int groupId)
        {
            var newBitMask = new BitMask(new ulong[bitMask.Buffer.Length]);
            newBitMask.SetAll(bitMask);
            
            var group = new Group(AllTypes, newBitMask, MemoryPool, ComponentArrayPool);
            Groups.Add(group);
            groupId = Groups.Count - 1;
            return group;
        }
    }
}