public class LeftClass<TDevice> where TDevice : IDevice
{
    public TDevice Device { get; set; } // это вот так точно, больше никаких модификаторов

    public Type DeviceType = typeof(TDevice);
}
public interface IDevice{
    string Description { get; }
}


class FooDevice : IDevice
{
    public FooDevice(string description)
    {
        Description = description;
    }

    public string Description { get; }
}

class BarDevice : IDevice
{
    public BarDevice(string description)
    {
        Description = description;
    }

    public string Description { get; }
}