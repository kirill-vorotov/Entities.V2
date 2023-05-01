using System.Runtime.CompilerServices;
using kv.Entities.V2;
using SampleApp.Components;

namespace SampleApp;

public class Sample1
{
    public static void Do()
    {
        TypeMap<TypeInfo> componentTypes = new();
        componentTypes.Set<FooComponent>(new TypeInfo(0, typeof(FooComponent), Unsafe.SizeOf<FooComponent>(), Unsafe.SizeOf<FooComponent>(), true, true, false));
        componentTypes.Set<BarComponent>(new TypeInfo(1, typeof(BarComponent), Unsafe.SizeOf<BarComponent>(), Unsafe.SizeOf<BarComponent>(), false, true, false));
        componentTypes.Set<QuxComponent>(new TypeInfo(2, typeof(QuxComponent), Unsafe.SizeOf<QuxComponent>(), Unsafe.SizeOf<QuxComponent>(), true, false, false));

        IComponentArrayPool componentArrayPool = new ComponentArrayPool(componentTypes);

        var world = new World(componentTypes, componentArrayPool);

        var commandBuffer = world.GetCommandBuffer();
        var entity = commandBuffer.CreateEntity();
        commandBuffer.AddUnmanagedComponent(entity, new FooComponent());
        commandBuffer.AddComponent<BarComponent>(entity);
        commandBuffer.AddManagedComponent(entity, new QuxComponent(new int[10, 10]));
        
        world.ExecuteCommandBuffers();
    }
}