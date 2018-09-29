namespace ReflectionAnalyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GetInterfaceAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            REFL020AmbiguousMatchInterface.Descriptor,
            REFL022UseFullyQualifiedName.Descriptor,
            REFL023TypeDoesNotImplementInterface.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.InvocationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is InvocationExpressionSyntax invocation &&
                invocation.TryGetTarget(KnownSymbol.Type.GetInterface, context.SemanticModel, context.CancellationToken, out var getInterface) &&
                getInterface.TryFindParameter(KnownSymbol.String, out var nameParameter) &&
                invocation.TryFindArgument(nameParameter, out var nameArg) &&
                TryGetName(nameArg, context, out var maybeNameSyntax, out var name) &&
                ReflectedMember.TryGetType(invocation, context, out var type, out _))
            {
                var count = CountInterfaces(type, name, out var match);

                if (count > 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(REFL020AmbiguousMatchInterface.Descriptor, nameArg.GetLocation()));
                }

                if (count == 1 && match.MetadataName == name)
                {
                    switch (nameArg.Expression)
                    {
                        case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression):
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    REFL022UseFullyQualifiedName.Descriptor,
                                    literal.GetLocation(),
                                    ImmutableDictionary<string, string>.Empty.Add(
                                        nameof(SyntaxKind.StringLiteralExpression),
                                        $"{match.ContainingNamespace}.{match.MetadataName}")));
                            break;
                        default:
                            if (maybeNameSyntax.HasValue &&
                                maybeNameSyntax.Value.Identifier.ValueText == "Name")
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        REFL022UseFullyQualifiedName.Descriptor,
                                        maybeNameSyntax.Value.Identifier.GetLocation(),
                                        ImmutableDictionary<string, string>.Empty.Add(
                                            nameof(SimpleNameSyntax),
                                            "FullName")));
                            }
                            else
                            {
                                context.ReportDiagnostic(Diagnostic.Create(REFL022UseFullyQualifiedName.Descriptor, nameArg.GetLocation()));
                            }

                            break;
                    }
                }

                if (count == 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(REFL023TypeDoesNotImplementInterface.Descriptor, nameArg.GetLocation()));
                }
            }
        }

        private static int CountInterfaces(ITypeSymbol type, string name, out ITypeSymbol match)
        {
            var count = 0;
            match = null;
            foreach (var candidate in type.AllInterfaces)
            {
                if (IsMatch(candidate))
                {
                    count++;
                    match = candidate;
                }
            }

            return count;

            bool IsMatch(ITypeSymbol candidate)
            {
                if (candidate.MetadataName == name)
                {
                    return true;
                }

                return name.IsParts(candidate.ContainingNamespace.ToString(), ".", candidate.MetadataName);
            }
        }

        private static bool TryGetName(ArgumentSyntax nameArg, SyntaxNodeAnalysisContext context, out Optional<SimpleNameSyntax> nameSyntax, out string name)
        {
            nameSyntax = default(Optional<SimpleNameSyntax>);
            switch (nameArg.Expression)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    if (memberAccess.Expression is TypeOfExpressionSyntax typeOf &&
                        context.SemanticModel.TryGetType(typeOf.Type, context.CancellationToken, out var type))
                    {
                        if (memberAccess.Name.Identifier.ValueText == "Name")
                        {
                            nameSyntax = memberAccess.Name;
                            name = type.MetadataName;
                            return true;
                        }

                        if (memberAccess.Name.Identifier.ValueText == "FullName")
                        {
                            nameSyntax = memberAccess.Name;
                            name = $"{type.ContainingNamespace}.{type.MetadataName}";
                            return true;
                        }
                    }

                    name = null;
                    return false;
                default:
                    return context.SemanticModel.TryGetConstantValue(nameArg.Expression, context.CancellationToken, out name);
            }
        }
    }
}
