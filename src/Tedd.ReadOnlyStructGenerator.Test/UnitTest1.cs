using System.Runtime.InteropServices;

namespace Tedd.ReadOnlyStructGenerator.Test;

[GenerateReadOnlyStruct(false,true)]
[StructLayout(LayoutKind.Explicit)]
public struct Test
{
    [FieldOffset(0)]
    public float X;
    [FieldOffset(4)]
    public float Y;
    [FieldOffset(8)]
    public float Z;
    
    public Test(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

}

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var ro = new ReadOnlyTest();
    }
}   
