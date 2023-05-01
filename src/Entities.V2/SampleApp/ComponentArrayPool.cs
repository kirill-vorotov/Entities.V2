using System.Buffers;
using System.Runtime.CompilerServices;
using kv.Entities.V2;
using SampleApp.Components;

namespace SampleApp;

public class ComponentArrayPool : IComponentArrayPool
{
    private readonly Func<int, Array>[] _rentFunctions;
    private readonly Action<Array>[] _returnFunctions;

    public ComponentArrayPool(TypeMap<TypeInfo> components)
    {
        TypeInfo typeInfo;
        
        _rentFunctions = new Func<int, Array>[components.Capacity];
        
        components.TryGetValue<FooComponent>(out typeInfo);
        _rentFunctions[typeInfo.Id] = (capacity) => ArrayPool<FooComponent>.Shared.Rent(capacity);
        
        components.TryGetValue<BarComponent>(out typeInfo);
        _rentFunctions[typeInfo.Id] = (capacity) => ArrayPool<BarComponent>.Shared.Rent(capacity);
        
        components.TryGetValue<QuxComponent>(out typeInfo);
        _rentFunctions[typeInfo.Id] = (capacity) => ArrayPool<QuxComponent>.Shared.Rent(capacity);

        _returnFunctions = new Action<Array>[components.Capacity];
        
        components.TryGetValue<FooComponent>(out typeInfo);
        _returnFunctions[typeInfo.Id] = (array) => ArrayPool<FooComponent>.Shared.Return(Unsafe.As<FooComponent[]>(array));
        
        components.TryGetValue<BarComponent>(out typeInfo);
        _returnFunctions[typeInfo.Id] = (array) => ArrayPool<BarComponent>.Shared.Return(Unsafe.As<BarComponent[]>(array));
        
        components.TryGetValue<QuxComponent>(out typeInfo);
        _returnFunctions[typeInfo.Id] = (array) => ArrayPool<QuxComponent>.Shared.Return(Unsafe.As<QuxComponent[]>(array));
    }

    public Array Rent(TypeInfo typeInfo, int minimumLength) => _rentFunctions[typeInfo.Id].Invoke(minimumLength);

    public void Return(TypeInfo typeInfo, Array array) => _returnFunctions[typeInfo.Id].Invoke(array);
}