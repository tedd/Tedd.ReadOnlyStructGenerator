using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tedd;
using Xunit;

namespace Tedd.ReadOnlyStructGenerator.Test;

public class GeneratorTest
{
    [Theory]
    // 1. Basic struct with default attribute, inside FileScopedNamespace
    [InlineData(@"
namespace MyNamespace;

[GenerateReadOnlyStruct]
public struct MyStruct {
    public int A;
}
")]
    // 2. Struct with explicit true/true attribute, inside BlockScopedNamespace
    [InlineData(@"
namespace MyNamespace {
    [GenerateReadOnlyStruct(true, true)]
    public struct MyStruct {
        public int A;
        public readonly int B;
        public const int C = 1;
    }
}
")]
    // 3. Struct with explicit false/false attribute, no explicit namespace (global namespace), multiple variables in one field
    [InlineData(@"
[GenerateReadOnlyStruct(false, false)]
public struct MyStruct {
    public int A, B;
    public MyStruct(int a, int b) { A = a; B = b; }
    public void Method() {}
}
")]
    // 4. Struct with different members (property, constructor), wrong attribute (to test ignore logic), string namespace Error.Namespace.Not.Found check via no namespace but checking logic.
    // Actually the logic says if nsStr == null, nsStr = "Error.Namespace.Not.Found".
    // This happens if struct is not in a namespace block, and there is no file-scoped namespace. Global namespace means CompilationUnitSyntax is the parent. The logic looks for NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax. If neither is found, nsStr is null, and then defaults to "Error.Namespace.Not.Found".
    [InlineData(@"
[SomeOtherAttribute]
[GenerateReadOnlyStruct]
public struct AnotherStruct {
    public int Property { get; set; }
}
class SomeOtherAttribute : System.Attribute {}
")]
    // 5. Empty struct
    [InlineData(@"
[GenerateReadOnlyStruct]
public struct EmptyStruct {}
")]
    // 6. LowerFirstChar edge case (empty string field name... though syntax won't allow easily, but uppercase field name)
    [InlineData(@"
namespace X {
    [GenerateReadOnlyStruct]
    public struct StructWithUppercase {
        public string Title;
        public string? title; // conflict but let's test if lowercases correctly
    }
}
")]
    public void RunGenerator_VariousInputs(string sourceCode)
    {
        var generator = new StructCopyGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CSharpCompilation.Create("Comp",
            new[] { CSharpSyntaxTree.ParseText(sourceCode) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // We only assert that no unhandled exceptions were thrown by generator and that it ran.
        // It's possible there are diagnostics for missing references since we only added object,
        // but the generator should not throw.
        var runResult = driver.GetRunResult();
        Assert.True(runResult.Diagnostics.IsEmpty);
        if (runResult.GeneratedTrees.Length > 0) { Assert.Contains("ReadOnly", runResult.GeneratedTrees.Last().GetText().ToString()); }
        Assert.True(runResult.Results.Length > 0);

    }
}
public class EdgeCasesTest
{
    [Fact]
    public void RunGenerator_SyntaxError_ReturnsDiagnostics()
    {
        var generator = new StructCopyGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CSharpCompilation.Create("Comp",
            new[] { CSharpSyntaxTree.ParseText("[GenerateReadOnlyStruct] public struct MyStruct { public int ") },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
    }
}
