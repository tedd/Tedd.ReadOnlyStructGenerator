namespace Tedd.ReadOnlyStructGenerator.Test;

[GenerateReadOnlyStruct]
public struct Test
{
    public float X;
    public float Y;
    public float Z;
}

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var ro = new ReadOnlyTest();
    }
}   
