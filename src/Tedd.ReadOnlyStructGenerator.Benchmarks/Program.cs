using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tedd;

namespace Tedd.ReadOnlyStructGenerator.Benchmarks
{
    [MemoryDiagnoser]
    public class GeneratorBenchmarks
    {
        private Compilation _compilation = null!;
        private Tedd.ReadOnlyStructGenerator.Archive.StructCopyGenerator _archiveGenerator = null!;
        private Tedd.StructCopyGenerator _optimizedGenerator = null!;

        [GlobalSetup]
        public void Setup()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
                using System;
                namespace TestNamespace
                {
                    [GenerateReadOnlyStruct]
                    public struct Vector3
                    {
                        public float X;
                        public float Y;
                        public float Z;
                    }

                    [GenerateReadOnlyStruct]
                    public struct Matrix4x4
                    {
                        public float M11; public float M12; public float M13; public float M14;
                        public float M21; public float M22; public float M23; public float M24;
                        public float M31; public float M32; public float M33; public float M34;
                        public float M41; public float M42; public float M43; public float M44;
                    }
                }
            ");

            _compilation = CSharpCompilation.Create("TestCompilation",
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            _archiveGenerator = new Tedd.ReadOnlyStructGenerator.Archive.StructCopyGenerator();
            _optimizedGenerator = new Tedd.StructCopyGenerator();
        }

        [Benchmark(Baseline = true)]
        public void ArchiveGenerator()
        {
            var driver = CSharpGeneratorDriver.Create(_archiveGenerator);
            driver.RunGenerators(_compilation);
        }

        [Benchmark]
        public void OptimizedGenerator()
        {
            var driver = CSharpGeneratorDriver.Create(_optimizedGenerator);
            driver.RunGenerators(_compilation);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<GeneratorBenchmarks>();
        }
    }
}
