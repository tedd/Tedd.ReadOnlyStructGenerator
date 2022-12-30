using System.Numerics;
using System.Runtime.InteropServices;

namespace Tedd.ReadOnlyStructGenerator.Test;

[GenerateReadOnlyStruct(false,true)]
[StructLayout(LayoutKind.Explicit)]
public struct Test
{
    public const int abc = 1;
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

[GenerateReadOnlyStruct(false, false)]
public struct Test2
{
    public float X;
    public float Y;
    public float Z;

    public Test2(float x, float y, float z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

    public Vector3 Vector3
    {
        get => new Vector3(X, Y, Z);
        // This section is only applicable for the mutable version of this struct
#if !RO_GEN
        set
        {
            X = value.X;
            Z = value.Y;
            Z = value.Z;
        }
#endif
    }
}

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var ro = new ReadOnlyTest(1,2,3);
        Assert.Equal(1, ro.X);
        Assert.Equal(2, ro.Y);
        Assert.Equal(3, ro.Z);

        var ro2 = new ReadOnlyTest2(3,2,1);
        Assert.Equal(3, ro2.X);
        Assert.Equal(2, ro2.Y);
        Assert.Equal(1, ro2.Z);
    }
}   
