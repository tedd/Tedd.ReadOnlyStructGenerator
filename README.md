# Tedd.ReadOnlyStructGenerator
.NET Source Generator for duplicating structs to read-only copies.

`[GenerateReadOnlyStruct]
public struct Test
{
    public float X;
    public float Y;
    public float Z;
}`

will inject this code into your project

`public readonly struct ReadOnlyTest
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;
    public ReadOnlyTest(Test value)
    {
        X = value.X;
        Y = value.Y;
        Z = value.Z;
    }

    public ReadOnlyTest(float x, float y, float z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }
}`
