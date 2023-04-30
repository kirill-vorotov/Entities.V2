using System.Buffers;

namespace kv.Entities.V2
{
    public sealed partial class World
    {
        internal readonly TypeMap<TypeInfo> ComponentTypes;
        internal readonly IComponentArrayPool ComponentArrayPool;

        internal EntityRepo EntityRepo;
        
        public World(TypeMap<TypeInfo> componentTypes, IComponentArrayPool componentArrayPool)
        {
            ComponentTypes = componentTypes;
            ComponentArrayPool = componentArrayPool;

            EntityRepo = new EntityRepo(componentTypes, MemoryPool<byte>.Shared, componentArrayPool);
        }
    }
}