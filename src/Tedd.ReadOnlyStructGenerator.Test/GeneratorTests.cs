using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tedd.ReadOnlyStructGenerator;
using Xunit;

namespace Tedd.ReadOnlyStructGenerator.Test
{
    public class GeneratorTests
    {
        [Fact]
        public void TestGeneratorCodeCoverage()
        {
            var source = @"
using System;

namespace TestNamespace
{
    [GenerateReadOnlyStruct(true, true)]
    public struct TestStruct
    {
        public int Field1;
        public string Field2;
    }
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
            Assert.Equal(3, generatedTrees.Count); // Original + Attribute + Generated
        }
    }
}
