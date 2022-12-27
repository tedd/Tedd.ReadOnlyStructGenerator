﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Tedd;

[Generator(LanguageNames.CSharp)]
public class StructCopyGenerator : IIncrementalGenerator // ISourceGenerator
{
    private const string attributeName = "GenerateReadOnlyStruct";
    private readonly SourceText GenerateReadOnlyAttributeSourceText = SourceText.From(@$"// Auto generated by {nameof(StructCopyGenerator)}
namespace Tedd;

[AttributeUsage(AttributeTargets.Struct)]
public class {attributeName}Attribute: Attribute 
{{
    private readonly bool _generateConstructor = true;
    private readonly bool _generateCopyConstructor = true;

    public {attributeName}Attribute(bool generateConstructor = true, bool generateCopyConstructor = true)
    {{
        _generateConstructor = generateConstructor;
        _generateCopyConstructor = generateCopyConstructor;
    }}

    public bool GenerateConstructor => _generateConstructor;
    public bool GenerateCopyConstructor => _generateCopyConstructor;
}}", Encoding.ASCII);

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(PostInitialization);
        // No initialization is required for this generator.
#if DEBUG
        if (!Debugger.IsAttached)
        {
            //Debugger.Launch();
            for (var i = 0; i < 10; i++)
                if (!System.Diagnostics.Debugger.IsAttached)
                    System.Threading.Thread.Sleep(1000);
            //System.Threading.Thread.Sleep(10000);
            //while (!System.Diagnostics.Debugger.IsAttached)
            //System.Threading.Thread.Sleep(5000);
        }
#endif 
        Debug.WriteLine("Initialize code generator");
    }


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            //Debugger.Launch();
            for (var i = 0; i < 10; i++)
                if (!System.Diagnostics.Debugger.IsAttached)
                    System.Threading.Thread.Sleep(1000);
            //System.Threading.Thread.Sleep(10000);
            //while (!System.Diagnostics.Debugger.IsAttached)
            //System.Threading.Thread.Sleep(5000);
        }
#endif 
        Debug.WriteLine("Initialize code generator");

        context.RegisterPostInitializationOutput(PostInitialization);
        // Register filters
        context.RegisterSourceOutput(context.CompilationProvider, ExecuteIncremental);
        context.SyntaxProvider.CreateSyntaxProvider(
            static (n, _) => n is StructDeclarationSyntax,
            static (n, _) => ((IMethodSymbol)n.SemanticModel.GetDeclaredSymbol(n.Node)).ReturnType.Kind);
    }


    private void PostInitialization(IncrementalGeneratorPostInitializationContext context)
    {
        // Add attribute
        context.AddSource("ReadOnlyGeneratorAttribute.cs", GenerateReadOnlyAttributeSourceText);
    }

    private void PostInitialization(GeneratorPostInitializationContext context)
    {
        // Add attribute
        context.AddSource("ReadOnlyGeneratorAttribute.cs", GenerateReadOnlyAttributeSourceText);
    }

    private static void ExecuteIncremental(SourceProductionContext context, Compilation compilation)
    {
        compilation.SyntaxTrees
            .SelectMany(x => x.GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>())
            .Where(x => x.AttributeLists.Any(y => y.Attributes.Any(z => z.Name.ToString() == attributeName))) // Todo: use TextSpan
            .ToList()
            .ForEach(x => GenerateClass(context, x));
    }

    private static void GenerateClass(SourceProductionContext context, StructDeclarationSyntax structDeclaration)
    {
        Debug.WriteLine("Execute code generator");
        var log = new StringBuilder();
        //try
        //{
        var nsDic = new Dictionary<string, NamespaceDeclarationSyntax>();

        var roToken = SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);

        // Get all struct declarations that have the ReadOnlyAttribute applied.
        //var root = syntaxTree.GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>();
        //foreach (var structDeclaration in root)



        {
            foreach (var attributeList in structDeclaration.AttributeLists)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                // Check if the attribute is the one we're looking for.
                AttributeSyntax generateReadOnlyStructAttribute = null;
                foreach (var attribute in attributeList.Attributes)
                {
                    if (attribute.Name.ToString() == attributeName)
                    {
                        generateReadOnlyStructAttribute = attribute;
                        break;
                    }
                }

                if (generateReadOnlyStructAttribute == null)
                    continue;

                var generateConstructor = generateReadOnlyStructAttribute.ArgumentList?.Arguments.Count > 0 && generateReadOnlyStructAttribute.ArgumentList?.Arguments[0].Expression.ToString() == "true";
                var generateCopyConstructor = generateReadOnlyStructAttribute.ArgumentList?.Arguments.Count > 1 && generateReadOnlyStructAttribute.ArgumentList?.Arguments[1].Expression.ToString() == "true";


                // Get name of struct
                var structName = structDeclaration.Identifier.Text;

                // Get all fields
                //var fieldMembers = structDeclaration.Members.OfType<FieldDeclarationSyntax>().ToArray();
                var fields = structDeclaration.Members.OfType<FieldDeclarationSyntax>().SelectMany(field => field.Declaration.Variables).ToArray();
                var fieldMembers = structDeclaration.Members.Where(w => w.IsKind(SyntaxKind.FieldDeclaration)).ToArray();//.ToArray();
                var constructorMembers = structDeclaration.Members.Where(w => w.IsKind(SyntaxKind.ConstructorDeclaration)).ToArray();
                var otherMembers = structDeclaration.Members.Where(w => !w.IsKind(SyntaxKind.FieldDeclaration) && !w.IsKind(SyntaxKind.ConstructorDeclaration)).ToArray();

                // Set fields to readonly
                for (var i = 0; i < fieldMembers.Length; i++)
                {
                    ref var field = ref fieldMembers[i];
                    if (!field.Modifiers.Any(a => a.IsKind(SyntaxKind.ReadOnlyKeyword)))
                        field = field.AddModifiers(roToken);
                }
                // Rename constructors
                for (var i = 0; i < constructorMembers.Length; i++)
                {
                    ref var constructor = ref constructorMembers[i];
                    constructor = ((ConstructorDeclarationSyntax)constructor).WithIdentifier(SyntaxFactory.Identifier("ReadOnly" + structName));
                }



                // Clone each struct declaration.
                var structCopyDeclaration = structDeclaration
                        .WithIdentifier(SyntaxFactory.Identifier("ReadOnly" + structName))
                        .WithModifiers(structDeclaration.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
                        .WithAttributeLists(structDeclaration.AttributeLists)
                        // Add ReadOnly attribute to 
                        .WithMembers(SyntaxFactory.List(fieldMembers))
                        .AddMembers(constructorMembers)
                        .AddMembers(otherMembers)
                    ;
                //SyntaxFactory.List(
                //        structDeclaration.Members
                //            .Select(member =>  member.AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
                //    ))
                ;


                if (generateCopyConstructor)
                {
                    // Add a constructor that takes the original struct as a parameter and assigns the fields.
                    structCopyDeclaration = structCopyDeclaration
                        .AddMembers(SyntaxFactory.ConstructorDeclaration("ReadOnly" + structName)
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                            .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("value")).WithType(SyntaxFactory.ParseTypeName(structName)))
                            // Add a body to the constructor that assigns the fields.
                            .WithBody(SyntaxFactory.Block(
                                fields.Select(field => SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            SyntaxFactory.IdentifierName(field.Identifier),
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName("value"),
                                                SyntaxFactory.IdentifierName(field.Identifier)))))
                                    .ToArray())));
                }

                if (generateConstructor)
                {
                    // Add constructor that takes all fields as parameters.
                    structCopyDeclaration = structCopyDeclaration
                        .AddMembers(SyntaxFactory.ConstructorDeclaration("ReadOnly" + structName)
                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                            .AddParameterListParameters(fields
                                .Select(field => SyntaxFactory
                                    .Parameter(SyntaxFactory.Identifier(ToLowerFirstChar(field.Identifier.Text)))
                                    .WithType(SyntaxFactory.ParseTypeName(((VariableDeclarationSyntax)field.Parent).Type.ToString())))
                                .ToArray())
                            // Add a body to the constructor that assigns the fields.
                            .WithBody(SyntaxFactory.Block(
                                fields.Select(field => SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName("this"),
                                                SyntaxFactory.IdentifierName(field.Identifier)
                                            ),
                                            SyntaxFactory.IdentifierName(ToLowerFirstChar(field.Identifier.Text))
                                        )))
                                    .ToArray())));
                }

                // Get root of document
                var root = structDeclaration.Parent;
                while (root.Parent != null)
                    root = root.Parent;

                // Get namespace
                string? nsStr = null;
                foreach (var node in root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>())
                {
                    nsStr = node.Name.ToString();
                    break;
                }
                if (nsStr == null)
                {
                    foreach (var node in root.DescendantNodes().OfType<NamespaceDeclarationSyntax>())
                    {
                        nsStr = node.Name.ToString();
                        break;
                    }
                }

                nsStr ??= "Error.Namespace.Not.Found";
                //var ns = root.DescendantNodes().First(w => w.IsKind(SyntaxKind.NamespaceDeclaration) || w.IsKind(SyntaxKind.FileScopedNamespaceDeclaration));
                //SyntaxList<UsingDirectiveSyntax> usings = new();
                //var nsStr = "Error.Namespace.Not.Found";
                //if (ns is NamespaceDeclarationSyntax ns1)
                //{
                //    nsStr = ns1.Name.ToString();
                //    //usings = ns1.Usings;
                //}
                //else if (ns is FileScopedNamespaceDeclarationSyntax ns2)
                //{
                //    nsStr = ns2.Name.ToString();
                //    //usings = ns2.Usings;
                //}

                //var nsStr = ns.Name.ToString();

                //var @namespace = context.Compilation.GetSemanticModel(structDeclaration.SyntaxTree).GetDeclaredSymbol(structDeclaration).ContainingNamespace;
                //var nsStr = @namespace.ToDisplayString();
                // Get usings
                var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
                //var usingsStr = string.Join("\r\n",usings.Select(s => s.ToString()));
                // Get or create namespace struct
                var nsObj = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(nsStr))
                    .WithLeadingTrivia(SyntaxFactory.Comment("// This file was generated by the StructCopyGenerator"))
                    .AddUsings(usings)
                    .AddMembers(structCopyDeclaration);

                // Write to file
                //var src = usingsStr+"\r\n\r\n"+nsObj.NormalizeWhitespace().ToFullString();
                var src = nsObj.NormalizeWhitespace().ToFullString();
                var file = $"{nsStr}.{structName}.ReadOnlyStructs.cs";
                Debug.WriteLine("File generated: " + file);
                Debug.WriteLine(src);
                context.AddSource(file, SourceText.From(src, Encoding.UTF8));
                //context.AddSource(file, src);
            }
        }
    }



    private static string ToLowerFirstChar(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToLower(input[0]) + input.Substring(1);
    }


    //public void Execute(GeneratorExecutionContext context)
    //{




    //    Debug.WriteLine("Execute code generator");
    //    var log = new StringBuilder();
    //    //try
    //    //{
    //    var nsDic = new Dictionary<string, NamespaceDeclarationSyntax>();

    //    var roToken = SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);

    //    // Get all struct declarations that have the ReadOnlyAttribute applied.
    //    foreach (var syntaxTree in context.Compilation.SyntaxTrees)
    //    {
    //        var root = syntaxTree.GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>();
    //        foreach (var structDeclaration in root)
    //        {
    //            foreach (var attributeList in structDeclaration.AttributeLists)
    //            {
    //                // Check if the attribute is the one we're looking for.
    //                AttributeSyntax generateReadOnlyStructAttribute = null;
    //                foreach (var attribute in attributeList.Attributes)
    //                {
    //                    if (attribute.Name.ToString() == attributeName)
    //                    {
    //                        generateReadOnlyStructAttribute = attribute;
    //                        break;
    //                    }
    //                }

    //                if (generateReadOnlyStructAttribute == null)
    //                    continue;

    //                // Get name of struct
    //                var structName = structDeclaration.Identifier.Text;

    //                // Get all fields
    //                //var fieldMembers = structDeclaration.Members.OfType<FieldDeclarationSyntax>().ToArray();
    //                var fields = structDeclaration.Members.OfType<FieldDeclarationSyntax>().SelectMany(field => field.Declaration.Variables).ToArray();
    //                var fieldMembers = structDeclaration.Members.Where(w => w.IsKind(SyntaxKind.FieldDeclaration)).ToArray();//.ToArray();
    //                var constructorMembers = structDeclaration.Members.Where(w => w.IsKind(SyntaxKind.ConstructorDeclaration)).ToArray();
    //                var otherMembers = structDeclaration.Members.Where(w => !w.IsKind(SyntaxKind.FieldDeclaration) && !w.IsKind(SyntaxKind.ConstructorDeclaration)).ToArray();

    //                // Set fields to readonly
    //                for (var i = 0; i < fieldMembers.Length; i++)
    //                {
    //                    ref var field = ref fieldMembers[i];
    //                    if (!field.Modifiers.Any(a => a.IsKind(SyntaxKind.ReadOnlyKeyword)))
    //                        field = field.AddModifiers(roToken);
    //                }
    //                // Rename constructors
    //                for (var i = 0; i < constructorMembers.Length; i++)
    //                {
    //                    ref var constructor = ref constructorMembers[i];
    //                    constructor = ((ConstructorDeclarationSyntax)constructor).WithIdentifier(SyntaxFactory.Identifier("ReadOnly" + structName));
    //                }


    //                // Clone each struct declaration.
    //                var structCopyDeclaration = structDeclaration
    //                        .WithIdentifier(SyntaxFactory.Identifier("ReadOnly" + structName))
    //                        .WithModifiers(structDeclaration.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
    //                        .WithAttributeLists(structDeclaration.AttributeLists)
    //                        // Add ReadOnly attribute to 
    //                        .WithMembers(SyntaxFactory.List(fieldMembers))
    //                        .AddMembers(constructorMembers)
    //                        .AddMembers(otherMembers)
    //                    ;
    //                //SyntaxFactory.List(
    //                //        structDeclaration.Members
    //                //            .Select(member =>  member.AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
    //                //    ))
    //                ;


    //                // Add a constructor that takes the original struct as a parameter and assigns the fields.
    //                structCopyDeclaration = structCopyDeclaration.AddMembers(
    //                        SyntaxFactory.ConstructorDeclaration("ReadOnly" + structName)
    //                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
    //                            .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("value")).WithType(SyntaxFactory.ParseTypeName(structName)))
    //                            // Add a body to the constructor that assigns the fields.
    //                            .WithBody(SyntaxFactory.Block(
    //                                fields.Select(field => SyntaxFactory.ExpressionStatement(
    //                                        SyntaxFactory.AssignmentExpression(
    //                                            SyntaxKind.SimpleAssignmentExpression,
    //                                            SyntaxFactory.IdentifierName(field.Identifier),
    //                                            SyntaxFactory.MemberAccessExpression(
    //                                                SyntaxKind.SimpleMemberAccessExpression,
    //                                                SyntaxFactory.IdentifierName("value"),
    //                                                SyntaxFactory.IdentifierName(field.Identifier)))))
    //                                    .ToArray())));

    //                // Add constructor that takes all fields as parameters.
    //                structCopyDeclaration = structCopyDeclaration.AddMembers(
    //                        SyntaxFactory.ConstructorDeclaration("ReadOnly" + structName)
    //                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
    //                            .AddParameterListParameters(fields.Select(field => SyntaxFactory.Parameter(SyntaxFactory.Identifier(ToLowerFirstChar(field.Identifier.Text))).WithType(SyntaxFactory.ParseTypeName(((VariableDeclarationSyntax)field.Parent).Type.ToString()))).ToArray())
    //                            // Add a body to the constructor that assigns the fields.
    //                            .WithBody(SyntaxFactory.Block(
    //                                fields.Select(field => SyntaxFactory.ExpressionStatement(
    //                                        SyntaxFactory.AssignmentExpression(
    //                                            SyntaxKind.SimpleAssignmentExpression,
    //                                            SyntaxFactory.MemberAccessExpression(
    //                                                SyntaxKind.SimpleMemberAccessExpression,
    //                                                SyntaxFactory.IdentifierName("this"),
    //                                                SyntaxFactory.IdentifierName(field.Identifier)

    //                                                ),
    //                                            SyntaxFactory.IdentifierName(ToLowerFirstChar(field.Identifier.Text))
    //                                            )))
    //                                    .ToArray())));

    //                // Get usings rekevant for current document
    //                var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();


    //                // Get namespace
    //                var @namespace = context.Compilation.GetSemanticModel(structDeclaration.SyntaxTree).GetDeclaredSymbol(structDeclaration).ContainingNamespace;
    //                var nsStr = @namespace.ToDisplayString();
    //                // Get or create namespace struct
    //                var nsObj = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(nsStr)).WithLeadingTrivia(SyntaxFactory.Comment("// This file was generated by the StructCopyGenerator."));
    //                // Write to file
    //                var src = nsObj.NormalizeWhitespace().ToFullString();
    //                var file = $"{nsStr}.{structName}.ReadOnlyStructs.cs";
    //                Debug.WriteLine("File generated: " + file);
    //                Debug.WriteLine(src);
    //                context.AddSource(file, SourceText.From(src, Encoding.UTF8));
    //            }
    //        }
    //    }

    //    //var structDeclarations = context.Compilation.SyntaxTrees
    //    //        .SelectMany(syntaxTree => syntaxTree.GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>())
    //    //        .Where(structDeclaration => structDeclaration.AttributeLists
    //    //            .SelectMany(attributeList => attributeList.Attributes)
    //    //            .Any(attribute => attribute.Name.ToString() == attributeName)).ToArray();

    //    ////var nsDic = new Dictionary<string, NamespaceDeclarationSyntax>();

    //    //// Create a read-only copy for each struct declaration.
    //    //foreach (var structDeclaration in structDeclarations)
    //    //{
    //    //    var structName = structDeclaration.Identifier.Text;
    //    //    log.AppendLine("Struct: " + structName);

    //    //    // Clone each struct declaration.
    //    //    var structCopyDeclaration = structDeclaration
    //    //        .WithIdentifier(SyntaxFactory.Identifier("ReadOnly" + structName))
    //    //        .WithModifiers(structDeclaration.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
    //    //        .WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>())
    //    //        // Add ReadOnly attribute
    //    //        .WithMembers(SyntaxFactory.List(
    //    //            structDeclaration.Members.Select(member =>
    //    //            member.AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
    //    //        ));


    //    //    var fields = structDeclaration.Members.OfType<FieldDeclarationSyntax>().SelectMany(field => field.Declaration.Variables).ToArray();

    //    //    // TODO:
    //    //    // - Do not add readonly on constructors
    //    //    // - Do not add readonly on properties
    //    //    // - Do not add readonly on methods (same as constructors`?)
    //    //    // - Do not add readonly on events
    //    //    // - Do not add readonly on indexers
    //    //    // - Do not add readonly on operators
    //    //    // - Do not add readonly on conversion operators
    //    //    // - Do not add readonly on destructors
    //    //    // - Do not add readonly on delegates
    //    //    // - Do not add readonly on enums
    //    //    // - Check if constructor already exists for all parameters

    //    //    // Add a constructor that takes the original struct as a parameter and assigns the fields.
    //    //    structCopyDeclaration = structCopyDeclaration.AddMembers(
    //    //            SyntaxFactory.ConstructorDeclaration("ReadOnly" + structName)
    //    //                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
    //    //                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("value")).WithType(SyntaxFactory.ParseTypeName(structName)))
    //    //                // Add a body to the constructor that assigns the fields.
    //    //                .WithBody(SyntaxFactory.Block(
    //    //                    fields.Select(field => SyntaxFactory.ExpressionStatement(
    //    //                            SyntaxFactory.AssignmentExpression(
    //    //                                SyntaxKind.SimpleAssignmentExpression,
    //    //                                SyntaxFactory.IdentifierName(field.Identifier),
    //    //                                SyntaxFactory.MemberAccessExpression(
    //    //                                    SyntaxKind.SimpleMemberAccessExpression,
    //    //                                    SyntaxFactory.IdentifierName("value"),
    //    //                                    SyntaxFactory.IdentifierName(field.Identifier)))))
    //    //                        .ToArray())));

    //    //    // Add constructor that takes all fields as parameters.
    //    //    structCopyDeclaration = structCopyDeclaration.AddMembers(
    //    //            SyntaxFactory.ConstructorDeclaration("ReadOnly" + structName)
    //    //                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
    //    //                .AddParameterListParameters(fields.Select(field => SyntaxFactory.Parameter(SyntaxFactory.Identifier(ToLowerFirstChar(field.Identifier.Text))).WithType(SyntaxFactory.ParseTypeName(((VariableDeclarationSyntax)field.Parent).Type.ToString()))).ToArray())
    //    //                // Add a body to the constructor that assigns the fields.
    //    //                .WithBody(SyntaxFactory.Block(
    //    //                    fields.Select(field => SyntaxFactory.ExpressionStatement(
    //    //                            SyntaxFactory.AssignmentExpression(
    //    //                                SyntaxKind.SimpleAssignmentExpression,
    //    //                                SyntaxFactory.MemberAccessExpression(
    //    //                                    SyntaxKind.SimpleMemberAccessExpression,
    //    //                                    SyntaxFactory.IdentifierName("this"),
    //    //                                    SyntaxFactory.IdentifierName(field.Identifier)

    //    //                                    ),
    //    //                                SyntaxFactory.IdentifierName(ToLowerFirstChar(field.Identifier.Text))
    //    //                                )))
    //    //                        .ToArray())));




    //    //    // Add the read-only struct to the compilation.
    //    //    //Debug.WriteLine(structCopyDeclaration.NormalizeWhitespace().ToFullString());
    //    //    //context.AddSource(structName + "ReadOnlyStruct.cs", structCopyDeclaration.NormalizeWhitespace().ToFullString());
    //    //    // Add to namespace
    //    //    var @namespace = context.Compilation.GetSemanticModel(structDeclaration.SyntaxTree).GetDeclaredSymbol(structDeclaration).ContainingNamespace;
    //    //    var ns = @namespace.ToDisplayString();
    //    //    if (!nsDic.TryGetValue(ns, out var nsObj))
    //    //        nsDic.Add(ns, nsObj =
    //    //            SyntaxFactory
    //    //                .NamespaceDeclaration(SyntaxFactory.ParseName(@namespace.ToString()))
    //    //                .WithLeadingTrivia(SyntaxFactory.Comment("// This file was generated by the StructCopyGenerator."))
    //    //            );
    //    //    nsDic[ns] = nsDic[ns].AddMembers(structCopyDeclaration);


    //    //}

    //    // Write all the namespaces (with structs) to corresponding files
    //    //foreach (var nskvp in nsDic)
    //    //{
    //    //    // Add the read - only struct to the compilation.
    //    //    var src = nskvp.Value.NormalizeWhitespace().ToFullString();
    //    //    var file = nskvp.Key + ".ReadOnlyStructs.cs";
    //    //    Debug.WriteLine("File generated: " + file);
    //    //    Debug.WriteLine(src);
    //    //    context.AddSource(file, SourceText.From(src, Encoding.UTF8));
    //    //}
    //    //}
    //    //catch (Exception ex)
    //    //{
    //    //    context.AddSource("Error.cs", SourceText.From("/*** ERROR: " + ex.Message + " ***/", Encoding.ASCII));
    //    //    throw;
    //    //}
    //    //context.AddSource("Log.cs", SourceText.From("/*** \r\n" + log.ToString() + "\r\n***/", Encoding.ASCII));
    //}


}