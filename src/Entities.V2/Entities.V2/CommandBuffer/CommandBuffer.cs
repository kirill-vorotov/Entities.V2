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
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace kv.Entities.V2
{
    public sealed class CommandBuffer
    {
        internal readonly int ThreadId;
        internal readonly TypeMap<TypeInfo> ComponentTypes;

        internal TypeMap<IComponentList?> Components = new();

        internal List<Entity> ToDestroy = new();
        internal List<EntityToCreate> ToCreate = new();
        internal List<EntityToUpdate> ToUpdate = new();

        internal CommandBuffer(int threadId, TypeMap<TypeInfo> componentTypes)
        {
            ThreadId = threadId;
            ComponentTypes = componentTypes;
        }

        [PublicAPI]
        public FutureEntity CreateEntity()
        {
            var futureEntityId = ToCreate.Count;
            var futureEntity = new FutureEntity(futureEntityId);
            ToCreate.Add(new EntityToCreate(futureEntity));
            return futureEntity;
        }

        [PublicAPI]
        public void DestroyEntity(Entity entity)
        {
            ToDestroy.Add(entity);
        }

        [PublicAPI]
        public void AddComponent<T>(Entity entity) where T : IEntityComponent
        {
            throw new System.NotImplementedException();
        }

        [PublicAPI]
        public void AddComponent<T>(FutureEntity entity) where T : IEntityComponent
        {
            throw new System.NotImplementedException();
        }

        [PublicAPI]
        public void AddUnmanagedComponent<T>(Entity entity, T value) where T : unmanaged, IEntityComponent
        {
            if (entity.Id >= ToUpdate.Capacity)
            {
                ToUpdate.Capacity = entity.Id + 1;
            }

            if (!ComponentTypes.TryGetValue<T>(out var typeInfo))
            {
                return;
            }
            
            Debug.Assert(typeInfo is { IsUnmanaged: true });

            if (!ToUpdate[entity.Id].HasValue)
            {
                ToUpdate[entity.Id] = new EntityToUpdate(entity);
            }
            if (TryAddUnmanagedComponent(value, out var index))
            {
                ToUpdate[entity.Id].Components.Set<T>(index);
            }
        }

        [PublicAPI]
        public void AddUnmanagedComponent<T>(FutureEntity entity, T value) where T : unmanaged, IEntityComponent
        {
            throw new System.NotImplementedException();
        }

        [PublicAPI]
        public void AddManagedComponent<T>(Entity entity, T value) where T : IEntityComponent
        {
            throw new System.NotImplementedException();
        }

        [PublicAPI]
        public void AddManagedComponent<T>(FutureEntity entity, T value) where T : IEntityComponent
        {
            throw new System.NotImplementedException();
        }

        [PublicAPI]
        public void RemoveComponent<T>(Entity entity) where T : IEntityComponent
        {
            throw new System.NotImplementedException();
        }

        internal void Clear()
        {
            foreach (var componentList in Components)
            {
                componentList?.Dispose();
            }

            Components.Clear();
            ToDestroy.Clear();

            foreach (var entityToCreate in ToCreate)
            {
                if (!entityToCreate.HasValue)
                {
                    continue;
                }
                
                entityToCreate.Components.Clear();
            }

            foreach (var entityToUpdate in ToUpdate)
            {
                if (!entityToUpdate.HasValue)
                {
                    continue;
                }
                
                entityToUpdate.Components.Clear();
            }
        }

        private bool TryAddUnmanagedComponent<T>(T value, out int index) where T : unmanaged, IEntityComponent
        {
            if (!ComponentTypes.TryGetValue<T>(out var typeInfo))
            {
                index = -1;
                return false;
            }
            
            Debug.Assert(typeInfo is { IsUnmanaged: true });

            if (typeInfo.IsZeroSized)
            {
                index = -1;
                return false;
            }
            
            if (!Components.TryGetValue<T>(out var componentList))
            {
                componentList = new UnmanagedComponentList<T>(typeInfo, MemoryPool<byte>.Shared);
                Components.Set<T>(componentList);
            }

            Debug.Assert(componentList is UnmanagedComponentList<T>);
            var unmanagedComponentList = Unsafe.As<UnmanagedComponentList<T>>(componentList);
            unmanagedComponentList.Add(value);
            index = unmanagedComponentList.Count - 1;
            return true;
        }
        
        private bool TryAddManagedComponent<T>(T value, out int index) where T : IEntityComponent
        {
            if (!ComponentTypes.TryGetValue<T>(out var typeInfo))
            {
                index = -1;
                return false;
            }
            
            Debug.Assert(typeInfo is { IsUnmanaged: false });
            
            if (!Components.TryGetValue<T>(out var componentList))
            {
                componentList = new ManagedComponentList<T>(typeInfo, ArrayPool<T>.Shared);
                Components.Set<T>(componentList);
            }

            Debug.Assert(componentList is ManagedComponentList<T>);
            var managedComponentList = Unsafe.As<ManagedComponentList<T>>(componentList);
            managedComponentList.Add(value);
            index = managedComponentList.Count - 1;
            return true;
        }
    }
}