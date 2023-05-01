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
using System.Collections.Concurrent;
using System.Threading;
using JetBrains.Annotations;

namespace kv.Entities.V2
{
    public sealed partial class World
    {
        internal ConcurrentDictionary<int, CommandBuffer> CommandBuffers = new();

        [PublicAPI]
        public CommandBuffer GetCommandBuffer()
        {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;

            if (!CommandBuffers.TryGetValue(currentThreadId, out var commandBuffer))
            {
                commandBuffer = new CommandBuffer(currentThreadId, ComponentTypes);
                CommandBuffers[currentThreadId] = commandBuffer;
            }

            return commandBuffer;
        }

        [PublicAPI]
        public void ExecuteCommandBuffers()
        {
            foreach (var commandBuffer in CommandBuffers.Values)
            {
                DestroyEntities(commandBuffer);
                CreateEntities(commandBuffer);
                UpdateEntities(commandBuffer);
                commandBuffer.Clear();
            }
            
            UpdateCachedQueries();
        }

        private void DestroyEntities(CommandBuffer commandBuffer)
        {
            foreach (var entity in commandBuffer.ToDestroy)
            {
                EntityRepo.DestroyEntity(entity);
            }
        }

        private void CreateEntities(CommandBuffer commandBuffer)
        {
            var bufferSize = BitMask.GetBufferSize(ComponentTypes.Entries.Length);
            var bitMaskMemoryOwner = MemoryPool<ulong>.Shared.Rent(bufferSize);
            var bitMask = new BitMask(bitMaskMemoryOwner.Memory[..bufferSize]);

            Group? group = default;
            var groupId = -1;

            foreach (var entityToCreate in commandBuffer.ToCreate)
            {
                if (!entityToCreate.HasValue)
                {
                    continue;
                }
                
                bitMask.SetAll(false);

                foreach (var component in entityToCreate.Components)
                {
                    bitMask[component.typeId] = true;
                }

                if (group is null || !group.BitMask.Equals(bitMask))
                {
                    if (!EntityRepo.TryGetGroup(bitMask, out group, out groupId))
                    {
                        group = EntityRepo.CreateGroup(bitMask, out groupId);
                    }
                }

                Debug.Assert(group is not null);
                EntityRepo.CreateEntity(group, groupId, out _, out var chunk, out var indexInChunk);
                
                foreach (var component in entityToCreate.Components)
                {
                    var srcList = commandBuffer.Components[component.typeId];
                    if (srcList is null || component.index == -1)
                    {
                        continue;
                    }
                    
                    srcList.CopyComponentTo(component.index, chunk, indexInChunk);
                }
            }

            bitMaskMemoryOwner.Dispose();
        }

        private void UpdateEntities(CommandBuffer commandBuffer)
        {
            var bufferSize = BitMask.GetBufferSize(ComponentTypes.Entries.Length);
            var bitMaskMemoryOwner = MemoryPool<ulong>.Shared.Rent(bufferSize);
            var bitMask = new BitMask(bitMaskMemoryOwner.Memory[..bufferSize]);
            
            Group? group = default;
            var groupId = -1;

            foreach (var entityToUpdate in commandBuffer.ToUpdate)
            {
                if (!entityToUpdate.HasValue)
                {
                    continue;
                }
                
                bitMask.SetAll(false);

                foreach (var component in entityToUpdate.Components)
                {
                    bitMask[component.typeId] = component.command switch
                    {
                        EntityToUpdate.Command.Add => true,
                        EntityToUpdate.Command.Remove => false,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }
                
                if (group is null || !group.BitMask.Equals(bitMask))
                {
                    if (!EntityRepo.TryGetGroup(bitMask, out group, out groupId))
                    {
                        group = EntityRepo.CreateGroup(bitMask, out groupId);
                    }
                }

                Debug.Assert(group is not null);
                
                EntityRepo.MoveEntity(entityToUpdate.Entity, groupId, group, out _, out var dstChunk, out var dstIndexInChunk);
                
                foreach (var component in entityToUpdate.Components)
                {
                    var srcList = commandBuffer.Components[component.typeId];
                    if (component.command == EntityToUpdate.Command.Remove || srcList is null || component.index == -1)
                    {
                        continue;
                    }
                    
                    srcList.CopyComponentTo(component.index, dstChunk, dstIndexInChunk);
                }
            }
            
            bitMaskMemoryOwner.Dispose();
        }
    }
}