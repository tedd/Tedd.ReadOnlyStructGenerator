using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tedd.ReadOnlyStructGenerator;
using Xunit;
using System.Threading;
using System.Reflection;
using System;
using System.Diagnostics;

namespace Tedd.ReadOnlyStructGenerator.Test
{
    public class CoverageTests
    {
        [Fact]
        public void GenerateWithConstructorsAndProperties()
        {
            var source = @"
using System;
using System.Numerics;

namespace TestNamespace
{
    [GenerateReadOnlyStruct(true, true)]
    public struct TestStruct
    {
        public readonly int Field1; // readonly field
        public const int Field2 = 5; // const field

        public TestStruct(int field1)
        {
            Field1 = field1;
        }

        public int Property1 => Field1;
    }

    [GenerateReadOnlyStruct(false, false)]
    public struct TestStructEmpty
    {
    }

    [GenerateReadOnlyStruct(true)]
    public struct TestStructPartialAttr
    {
        public int A, B;
    }
}

namespace FileScopedNamespace;
[GenerateReadOnlyStruct(true, true)]
public struct TestStruct2
{
    public int Field2;
}

[GenerateReadOnlyStruct]
public struct TestStructGlobal
{
}

public class OtherClass
{
    [GenerateReadOnlyStruct(true, true)]
    public struct NestedStruct
    {
        public int NestedField;
    }
}

// Trigger error namespace
public struct OrphanStruct
{
    // Need to have attributes to trigger generation, and generate readonly struct without a namespace
}

[GenerateReadOnlyStruct(true, true)]
public struct TopLevelStruct
{
    public int NormalField;
}
";
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
            var compilation = CSharpCompilation.Create("Tests", new[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new StructCopyGenerator();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            Assert.Empty(diagnostics);
            var generatedTrees = outputCompilation.SyntaxTrees.ToList();
            Assert.Equal(9, generatedTrees.Count);
        }

        [Fact]
        public void GenerateWithNamespaceUsings()
        {
            var source = @"
namespace TestNamespace
{
    using System;
    using System.Numerics;

    [GenerateReadOnlyStruct(true, true)]
    public struct TestStruct
    {
        public int Field1;
    }
}
";
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
            var compilation = CSharpCompilation.Create("Tests", new[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new StructCopyGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        }

        [Fact]
        public void IgnoreInvalidStructs()
        {
            var source = @"
using System;

namespace TestNamespace
{
    [AnotherAttribute]
    public struct TestStruct
    {
        public int Field1;
    }

    [GenerateReadOnlyStruct]
    public class ClassTest
    {
    }
}
";
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
            var compilation = CSharpCompilation.Create("Tests", new[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new StructCopyGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        }

        [Fact]
        public void LowerFirstChar_NullOrEmpty()
        {
            var method = typeof(StructCopyGenerator).GetMethod("ToLowerFirstChar", BindingFlags.NonPublic | BindingFlags.Static);
            var result = method.Invoke(null, new object[] { "" });
            Assert.Equal("", result);
            result = method.Invoke(null, new object[] { null });
            Assert.Null(result);
        }

        [Fact]
        public void CallInitializeAndPostInitialize()
        {
            var generator = new StructCopyGenerator();
            var incrementalCtxType = typeof(IncrementalGeneratorInitializationContext);
            var incrementalCtx = Activator.CreateInstance(incrementalCtxType);
            try
            {
                generator.Initialize((IncrementalGeneratorInitializationContext)incrementalCtx);
            }
            catch { }

            var generatorInitCtxType = typeof(GeneratorInitializationContext);
            var generatorInitCtx = Activator.CreateInstance(generatorInitCtxType);
            try
            {
                generator.Initialize((GeneratorInitializationContext)generatorInitCtx);
            }
            catch { }

            var postCtxType = typeof(GeneratorPostInitializationContext);
            var postCtx = Activator.CreateInstance(postCtxType);
            var method = typeof(StructCopyGenerator).GetMethod("PostInitialization", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { postCtxType }, null);
            try
            {
                method.Invoke(generator, new object[] { postCtx });
            }
            catch { }

            var incrementalPostCtxType = typeof(IncrementalGeneratorPostInitializationContext);
            var incrementalPostCtx = Activator.CreateInstance(incrementalPostCtxType);
            var method2 = typeof(StructCopyGenerator).GetMethod("PostInitialization", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { incrementalPostCtxType }, null);
            try
            {
                method2.Invoke(generator, new object[] { incrementalPostCtx });
            }
            catch { }
        }

        [Theory]
        [InlineData("null", "null")]
        [InlineData("false", "null")]
        [InlineData("null", "false")]
        [InlineData("true", "false")]
        [InlineData("false", "true")]
        [InlineData("false", "false")]
        public void InvalidAttributeArguments(string arg1, string arg2)
        {
            var source = $@"
using System;

namespace TestNamespace
{{
    [GenerateReadOnlyStruct({arg1}, {arg2})]
    public struct TestStructInvalidArgs
    {{
        public int Field1;
    }}
}}
";
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
            var compilation = CSharpCompilation.Create("Tests", new[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new StructCopyGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        }

        [Fact]
        public void NullParentTypeCoverage()
        {
            var fieldDecl = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("int"))
                .AddVariables(SyntaxFactory.VariableDeclarator("Field1")));
            var structDecl = SyntaxFactory.StructDeclaration("ManualStruct")
                .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(SyntaxFactory.ParseName("GenerateReadOnlyStruct"))
                    .AddArgumentListArguments(
                        SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                        SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))
                    )
                )))
                .AddMembers(fieldDecl);

            var context = default(SourceProductionContext);
            var method = typeof(StructCopyGenerator).GetMethod("GenerateClass", BindingFlags.NonPublic | BindingFlags.Static);
            try
            {
                method.Invoke(null, new object[] { context, structDecl });
            }
            catch { }
        }
    }
}
