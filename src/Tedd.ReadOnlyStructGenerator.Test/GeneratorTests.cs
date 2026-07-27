using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Tedd;

namespace Tedd.ReadOnlyStructGenerator.Test;

public class GeneratorTests
{
    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.InteropServices.StructLayoutAttribute).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Numerics.Vector3).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location)
        };

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        return CSharpCompilation.Create("TestCompilation",
            new[] { syntaxTree },
            references,
            options);
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, string Output) RunGenerator(string source)
    {
        var compilation = CreateCompilation(source);
        var generator = new StructCopyGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var outputSource = outputCompilation.SyntaxTrees
            .Where(t => t.FilePath.Contains(".ReadOnlyStructs.cs"))
            .Select(t => t.ToString())
            .FirstOrDefault();

        return (diagnostics, outputSource ?? string.Empty);
    }

    [Theory]
    [InlineData("namespace TestNs { [GenerateReadOnlyStruct(true, true)] public struct TestStruct { public int A; } }", true, true)]
    [InlineData("namespace TestNs { [GenerateReadOnlyStruct(true, false)] public struct TestStruct { public int A; } }", true, false)]
    [InlineData("namespace TestNs { [GenerateReadOnlyStruct(false, true)] public struct TestStruct { public int A; } }", false, true)]
    [InlineData("namespace TestNs { [GenerateReadOnlyStruct(false, false)] public struct TestStruct { public int A; } }", false, false)]
    [InlineData("namespace TestNs { [GenerateReadOnlyStruct] public struct TestStruct { public int A; } }", false, false)] // Default arguments don't generate ctors in this implementation
    public void TestGenerator_ConstructorGeneration(string source, bool hasCtor, bool hasCopyCtor)
    {
        var (diagnostics, output) = RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains("ReadOnlyTestStruct", output);

        if (hasCtor)
        {
            Assert.Contains("public ReadOnlyTestStruct(int a)", output);
        }
        else
        {
            Assert.DoesNotContain("public ReadOnlyTestStruct(int a)", output);
        }

        if (hasCopyCtor)
        {
            Assert.Contains("public ReadOnlyTestStruct(TestStruct value)", output);
        }
        else
        {
            Assert.DoesNotContain("public ReadOnlyTestStruct(TestStruct value)", output);
        }
    }

    [Theory]
    [InlineData("namespace BlockNs { [GenerateReadOnlyStruct(true, true)] public struct MyStruct { public int X; } }", "BlockNs")]
    [InlineData("namespace FileNs; [GenerateReadOnlyStruct(true, true)] public struct MyStruct { public int X; }", "FileNs")]
    [InlineData("[GenerateReadOnlyStruct(true, true)] public struct MyStruct { public int X; }", "Error.Namespace.Not.Found")]
    public void TestGenerator_NamespaceHandling(string source, string expectedNamespace)
    {
        var (diagnostics, output) = RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains($"namespace {expectedNamespace}", output);
    }

    [Theory]
    [InlineData("namespace TestNs { [GenerateReadOnlyStruct(true, true)] public struct FieldStruct { public readonly int A; public const int B = 1; public int C; } }")]
    public void TestGenerator_FieldModifiers(string source)
    {
        var (diagnostics, output) = RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Ensure regular fields get readonly
        Assert.Contains("public readonly int C;", output);
        // Ensure const fields stay const
        Assert.Contains("public const int B = 1;", output);
        // Ensure readonly fields stay readonly
        Assert.Contains("public readonly int A;", output);

        // Constructor generated has all parameters currently, but doesn't set const fields
        Assert.Contains("public ReadOnlyFieldStruct(int a, int b, int c)", output);
        Assert.Contains("this.A = a", output);
        Assert.Contains("this.C = c", output);
        Assert.DoesNotContain("this.B = b", output);
    }

    [Theory]
    [InlineData("namespace TestNs { [GenerateReadOnlyStruct(true, true)] public struct MemberStruct { public int A; public MemberStruct(int a) { A = a; } public void Method() {} } }")]
    public void TestGenerator_OtherMembers(string source)
    {
        var (diagnostics, output) = RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Ensure original constructor is renamed
        Assert.Contains("public ReadOnlyMemberStruct(int a)", output);

        // Ensure methods are copied
        Assert.Contains("public void Method()", output);
    }

    [Fact]
    public void TestGenerator_NoAttribute_NoOutput()
    {
        var source = "namespace TestNs { public struct NoAttrStruct { public int A; } }";
        var (diagnostics, output) = RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(output);
    }

    [Theory]
    [InlineData("using System; using System.Linq; namespace TestNs { [GenerateReadOnlyStruct(true, true)] public struct UsingStruct { public int A; } }", new string[] { "System", "System.Linq" })]
    public void TestGenerator_UsingsCopied(string source, string[] expectedUsings)
    {
        var (diagnostics, output) = RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        foreach (var u in expectedUsings)
        {
            Assert.Contains($"using {u};", output);
        }
    }
}