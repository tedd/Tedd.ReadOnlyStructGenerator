## 2025-02-12 - Dependency and Framework Modernization Assessment

**Observation:** The core source generator (`Tedd.ReadOnlyStructGenerator`) and archive projects use `netstandard2.0` which is correct for a Roslyn Source Generator that targets a wide range of consumers. The referenced packages `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.Analyzers` are at version `4.14.0`. Attempting to upgrade them to `5.3.0` breaks the tests because the test project (`Tedd.ReadOnlyStructGenerator.Test`) running against .NET 8 SDK uses Roslyn compiler version 4.x which throws `CS9057: Analyzer assembly cannot be used because it references version '5.3.0.0' of the compiler, which is newer than the currently running version '5.0.0.0'`. Therefore, for a source generator, we should not aggressively bump the Roslyn dependencies since it forces consumers to use newer SDKs, breaking compatibility with older SDKs (like .NET 6 or .NET 7).
Version `4.14.0` is appropriate and safe for maximum compatibility.

The test project `Tedd.ReadOnlyStructGenerator.Test` uses `net8.0` and can have its test SDK updated from `18.6.0` to `18.7.0`.
The `Tedd.ReadOnlyStructGenerator.Benchmarks` project has no updates.

**Strategic Action:**
1. Keep `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.Analyzers` at `4.14.0` for maximum backward compatibility with older .NET SDKs running the source generator.
2. Upgrade `Microsoft.NET.Test.Sdk` in the test project to `18.7.0`.
3. Retain target frameworks `netstandard2.0` (for generator) and `net8.0` (for tests/benchmarks).
