using kv.Entities.V2;

namespace SampleApp.Components;

public class QuxComponent : IEntityComponent
{
    public int[,] Array;

    public QuxComponent(int[,] array)
    {
        Array = array;
    }
}