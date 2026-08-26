## 2024-05-26 - Source Generator Execution Bottlenecks
**Observation:** In the previous implementation, the `.NET` source generator `StructCopyGenerator.cs` iterated over the entire syntax tree twice per class generation: first to discover the file-scoped or block-scoped namespace, and again to find all `UsingDirectiveSyntax` references, representing O(N * M) behavior per targeted struct (where N is total document nodes, M is targeted elements). Additionally, member iteration triggered 5 distinct LINQ loops, boxing enumerators and creating unnecessary garbage. BenchmarkDotNet metrics revealed an execution mean of ~596 us and 126 KB heap allocations.
**Strategic Action:**
- Substituted `DescendantNodes().OfType<T>()` with `Node.Parent` traversal to determine namespaces efficiently, drastically reducing tree walking depth (O(P) where P is tree depth).
- Minimized iterative LINQ operations across `structDeclaration.Members` to a single pass structure evaluating each node's `SyntaxKind`, utilizing standard generic `List<T>` elements avoiding excessive underlying array permutations (O(M)).
- Resulted in ~9% performance augmentation (latency decreased from 596us -> 547us, standard deviation and GC allocations slightly reduced).

## 2024-08-26 - Roslyn Source Generator SyntaxFactory Elimination
**Observation:** Relying on `Microsoft.CodeAnalysis.CSharp.SyntaxFactory` to construct and mutate syntax trees and then calling `NormalizeWhitespace().ToFullString()` in a Roslyn Source Generator introduces massive overhead in both execution time and heap allocations, serving as a primary computational bottleneck.
**Strategic Action:** Entirely substitute `SyntaxFactory` usage with programmatic string building via `System.Text.StringBuilder` for emitting generated C# code. This procedural shift achieved a 71% reduction in mean execution latency (679us -> 195us) and a 59% reduction in memory allocations (126KB -> 51KB) during source generation.
