# Tedd.ReadOnlyStructGenerator
.NET Source Generator for duplicating structs to read-only copies.

Adding [GenerateReadOnlyStruct] to a struct like this

```csharp
[GenerateReadOnlyStruct]
public struct Test
{
    public float X;
    public float Y;
    public float Z;
}
```

will inject this code into your project

```csharp
public readonly struct ReadOnlyTest
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;
    
    public ReadOnlyTest(float x, float y, float z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

    public ReadOnlyTest(Test value)
    {
        X = value.X;
        Y = value.Y;
        Z = value.Z;
    }
}
```
`[GenerateReadOnlyStruct]` takes two boolean parameters:

* GenerateConstructor
* GenerateCopyConstructor

This allows you to prevent constructors from being generated, in case you already have that implemented.

```csharp
[GenerateReadOnlyStruct(GenerateConstructor: false, GenerateCopyConstructor: false)]
// Or just [GenerateReadOnlyStruct(false, false)]
public struct Test
{
    public float X;
    public float Y;
    public float Z;

    public Test(float x, float y, float z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }
}
```

will inject this code into your project

```csharp
public readonly struct ReadOnlyTest
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;

    public ReadOnlyTest(float x, float y, float z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }
}
```

