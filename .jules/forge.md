## 2026-09-02 - Dependency Constraints Analysis

**Observation:** The Roslyn Source Generator projects (`Tedd.ReadOnlyStructGenerator` and `Tedd.ReadOnlyStructGenerator.Archive`) target `netstandard2.0` and depend on `Microsoft.CodeAnalysis.Analyzers` and `Microsoft.CodeAnalysis.CSharp` version `4.14.0`. There are available updates up to `5.9.0`. However, updating the compiler dependencies in a Roslyn Source Generator beyond the baseline version shipped in the lowest supported .NET SDK (e.g., keeping at 4.14.0 for .NET 8 SDK support) causes `CS9057` compiler version mismatch errors for consumers.

**Strategic Action:** Do not update `Microsoft.CodeAnalysis.*` in the generator and archive projects. They must remain at version `4.14.0` for maximum compatibility with downstream SDKs.

## 2026-09-02 - Packaging Adjustments

**Observation:** Running `dotnet pack` generates warnings about missing Readmes for the `Tedd.ReadOnlyStructGenerator.Archive` and `Tedd.ReadOnlyStructGenerator.Benchmarks` projects, which produce packages that do not need to be shipped.

**Strategic Action:** Set `<IsPackable>false</IsPackable>` in the `.csproj` files for `Archive` and `Benchmarks` projects to prevent NuGet package creation. Also remove empty `<PackageLicenseExpression></PackageLicenseExpression>` in `Tedd.ReadOnlyStructGenerator.csproj`.
