## 2024-05-26 - Source Generator Execution Bottlenecks
**Observation:** In the previous implementation, the `.NET` source generator `StructCopyGenerator.cs` iterated over the entire syntax tree twice per class generation: first to discover the file-scoped or block-scoped namespace, and again to find all `UsingDirectiveSyntax` references, representing O(N * M) behavior per targeted struct (where N is total document nodes, M is targeted elements). Additionally, member iteration triggered 5 distinct LINQ loops, boxing enumerators and creating unnecessary garbage. BenchmarkDotNet metrics revealed an execution mean of ~596 us and 126 KB heap allocations.
**Strategic Action:**
- Substituted `DescendantNodes().OfType<T>()` with `Node.Parent` traversal to determine namespaces efficiently, drastically reducing tree walking depth (O(P) where P is tree depth).
- Minimized iterative LINQ operations across `structDeclaration.Members` to a single pass structure evaluating each node's `SyntaxKind`, utilizing standard generic `List<T>` elements avoiding excessive underlying array permutations (O(M)).
- Resulted in ~9% performance augmentation (latency decreased from 596us -> 547us, standard deviation and GC allocations slightly reduced).

## 2024-05-18 - Tedd.ReadOnlyStructGenerator Optimization
**Observation:** Utilizing Roslyn's `SyntaxFactory` with `.NormalizeWhitespace()` inside an incremental source generator causes substantial memory allocation (125.99 KB vs 54.36 KB per generation cycle) and high execution latency (693.9 us vs 212.6 us) due to continuous deep syntax tree traversals and construction of immutable tree graph objects.
**Strategic Action:** Substituted `SyntaxFactory` code generation logic with direct memory string concatenation via `StringBuilder`. This prevents huge heap allocations and limits overhead to `O(M)` (where `M` is struct members). This technique acts as a prime blueprint for .NET compiler source generator optimizations.
