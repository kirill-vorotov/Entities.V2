using System.Buffers;

namespace kv.Entities.V2
{
    public sealed partial class World
    {
        internal readonly TypeMap<TypeInfo> ComponentTypes;

        internal EntityRepo EntityRepo;
        
        public World(TypeMap<TypeInfo> componentTypes, IComponentArrayPool componentArrayPool)
        {
            ComponentTypes = componentTypes;

            EntityRepo = new EntityRepo(componentTypes, MemoryPool<byte>.Shared, componentArrayPool);
        }
    }
}