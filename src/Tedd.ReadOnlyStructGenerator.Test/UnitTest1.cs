using System.Runtime.InteropServices;

namespace Tedd.ReadOnlyStructGenerator.Test;

[GenerateReadOnlyStruct]
[StructLayout(LayoutKind.Sequential)]
public struct Test
{
    public float X;
    public float Y;
    public float Z;
    public float ZZZZZ;

}

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var ro = new ReadOnlyTest();
    }
}   
