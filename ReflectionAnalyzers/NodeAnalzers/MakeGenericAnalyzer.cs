namespace ReflectionAnalyzers
{
    using System.Collections.Immutable;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class MakeGenericAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            REFL031UseCorrectGenericArguments.Descriptor);

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
                TypeArguments.TryCreate(invocation, context, out var typeArguments))
            {
                if (typeArguments.Parameters.Length != typeArguments.Arguments.Length)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            REFL031UseCorrectGenericArguments.Descriptor,
                            invocation.ArgumentList.GetLocation(),
                            $"The member has {typeArguments.Parameters.Length} parameter{PluralS(typeArguments.Parameters.Length)} but {typeArguments.Arguments.Length} argument{PluralS(typeArguments.Arguments.Length)} are passed in."));
                }
                else if (typeArguments.TryFindMisMatch(context, out var argument, out var parameter))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            REFL031UseCorrectGenericArguments.Descriptor,
                            argument.GetLocation(),
                            $"The argument {argument} does not satisfy the constraints of the parameter {parameter}."));
                }
            }

            string PluralS(int i) => i == 1 ? string.Empty : "s";
        }
    }
}