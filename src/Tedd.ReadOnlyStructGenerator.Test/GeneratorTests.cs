using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tedd.ReadOnlyStructGenerator;
using Xunit;
using System.Collections.Generic;
using System.Reflection;

namespace Tedd.ReadOnlyStructGenerator.Test;

public class GeneratorTests
{
    private GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create("SourceGeneratorTests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new StructCopyGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return driver.GetRunResult();
    }

    [Theory]
    [InlineData("public struct EmptyStruct {}", 1)] // Only attribute generated
    [InlineData("[GenerateReadOnlyStruct] public struct GenerateEmpty {}", 2)]
    [InlineData("[GenerateReadOnlyStruct(true, false)] public struct GenerateCtorOnly { public int A; }", 2)]
    [InlineData("[GenerateReadOnlyStruct(false, true)] public struct GenerateCopyCtorOnly { public int A; }", 2)]
    [InlineData("[GenerateReadOnlyStruct(false, false)] public struct GenerateNone { public int A; }", 2)]
    [InlineData("[GenerateReadOnlyStruct(true, true)] public struct GenerateBoth { public int A; }", 2)]
    public void TestGeneratorExecution(string structCode, int expectedGeneratedFiles)
    {
        var source = $$"""
        using System;
        using Tedd.ReadOnlyStructGenerator;

        namespace Example
        {
            {{structCode}}
        }
        """;

        var runResult = RunGenerator(source);
        Assert.Equal(expectedGeneratedFiles, runResult.GeneratedTrees.Length);
    }

    [Fact]
    public void TestFileScopedNamespace()
    {
        var source = """
        using System;
        using Tedd.ReadOnlyStructGenerator;

        namespace Example.FileScoped;

        [GenerateReadOnlyStruct]
        public struct MyStruct { public int X; }
        """;

        var runResult = RunGenerator(source);
        Assert.Equal(2, runResult.GeneratedTrees.Length);
        Assert.Contains("namespace Example.FileScoped", runResult.GeneratedTrees[1].ToString());
    }

    [Fact]
    public void TestNoNamespace()
    {
        var source = """
        using System;
        using Tedd.ReadOnlyStructGenerator;

        [GenerateReadOnlyStruct]
        public struct MyStruct { public int X; }
        """;

        var runResult = RunGenerator(source);
        Assert.Equal(2, runResult.GeneratedTrees.Length);
        Assert.Contains("namespace Error.Namespace.Not.Found", runResult.GeneratedTrees[1].ToString());
    }

    [Fact]
    public void TestConstructorsAndFieldsGeneration()
    {
        var source = """
        using System;
        using System.Runtime.InteropServices;
        using Tedd.ReadOnlyStructGenerator;

        namespace Example
        {
            [GenerateReadOnlyStruct(true, true)]
            [StructLayout(LayoutKind.Explicit)]
            public struct TestStruct
            {
                public const int abc = 1;
                [FieldOffset(0)]
                public float X;
                [FieldOffset(4)]
                public float Y;

                public TestStruct(float x, float y)
                {
                    X = x;
                    Y = y;
                }

                public void MyMethod() {}
            }
        }
        """;

        var runResult = RunGenerator(source);
        Assert.Equal(2, runResult.GeneratedTrees.Length);

        var structCode = runResult.GeneratedTrees[1].ToString();

        // Assert field offsets are preserved
        Assert.Contains("[FieldOffset(0)]", structCode);
        Assert.Contains("[FieldOffset(4)]", structCode);

        // Assert const is preserved
        Assert.Contains("public const int abc = 1;", structCode);

        // Assert copy constructor is generated
        Assert.Contains("public ReadOnlyTestStruct(TestStruct value)", structCode);

        // Assert constructor is generated
        Assert.Contains("public ReadOnlyTestStruct(int abc, float x, float y)", structCode); // Currently the generator includes const fields in constructor

        // Assert existing constructor is preserved and renamed
        Assert.Contains("public ReadOnlyTestStruct(float x, float y)", structCode);

        // Assert other members are preserved
        Assert.Contains("public void MyMethod()", structCode);
    }

    [Fact]
    public void TestToLowerFirstCharNullOrEmpty()
    {
        var method = typeof(StructCopyGenerator).GetMethod("ToLowerFirstChar", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var resultNull = method.Invoke(null, new object[] { null });
        Assert.Null(resultNull);

        var resultEmpty = method.Invoke(null, new object[] { "" });
        Assert.Equal("", resultEmpty);
    }
}
