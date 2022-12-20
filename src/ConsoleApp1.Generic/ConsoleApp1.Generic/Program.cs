var obj = new LeftClass<FooDevice>();
obj.Device = new FooDevice("foo");

Console.WriteLine(obj.DeviceType);

var obj2 = new LeftClass<BarDevice>();
obj2.Device = new BarDevice("bar");
Console.WriteLine(obj2.DeviceType);