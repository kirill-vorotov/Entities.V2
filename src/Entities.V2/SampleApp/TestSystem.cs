using kv.Entities.V2;
using kv.Entities.V2.Generated;
using SampleApp.Components;

namespace SampleApp;

public static partial class TestSystem
{
    [All(typeof(FooComponent), QueryMode.ReadWrite)]
    [None(typeof(BarComponent))]
    [Any(typeof(QuxComponent))]
    [Any(typeof(BooComponent))]
    public partial struct Query
    {
    }

    public static void Execute()
    {
        var world = EntitiesApi.CreateWorld();
        var commandBuffer = world.GetCommandBuffer();

        for (var i = 0; i < 10; i++)
        {
            var entity = commandBuffer.CreateEntity();
            commandBuffer.AddUnmanagedComponent(entity, new FooComponent());
            if (i % 2 == 0)
            {
                commandBuffer.AddComponent<BarComponent>(entity);
            }
        }

        {
            var entity = commandBuffer.CreateEntity();
            commandBuffer.AddUnmanagedComponent(entity, new FooComponent());
            commandBuffer.AddManagedComponent(entity, new QuxComponent(new int[10, 10]));
        }
        
        world.ExecuteCommandBuffers();
        
        var query = Query.Create(world);

        var index = 0;
        foreach (var entry in query)
        {
            entry.FooComponent.X = index;
            entry.FooComponent.Y = -index;

            index++;
        }
        
        Console.WriteLine(index);
    }
}