﻿namespace Gu.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GU0008AvoidRelayProperties : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GU0008";
        private const string Title = "Avoid relay properties.";
        private const string MessageFormat = "Avoid relay properties.";
        private const string Description = "Avoid relay properties.";
        private static readonly string HelpLink = Analyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: AnalyzerCategory.Correctness,
            defaultSeverity: DiagnosticSeverity.Hidden,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: Description,
            helpLinkUri: HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.PropertyDeclaration);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            var propertySymbol = (IPropertySymbol)context.ContainingSymbol;
            if (propertySymbol.IsStatic ||
                propertySymbol.DeclaredAccessibility == Accessibility.Protected ||
                propertySymbol.DeclaredAccessibility == Accessibility.Private)
            {
                return;
            }

            if (IsRelayProperty((PropertyDeclarationSyntax)context.Node))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation()));
            }
        }

        private static bool IsRelayProperty(PropertyDeclarationSyntax property)
        {
            if (property.ExpressionBody != null)
            {
                return IsRelayReturn(property.ExpressionBody.Expression);
            }

            AccessorDeclarationSyntax getter;
            if (property.TryGetGetAccessorDeclaration(out getter))
            {
                if (getter.Body == null)
                {
                    return false;
                }

                StatementSyntax statement;
                if (getter.Body.Statements.TryGetSingle(out statement))
                {
                    return IsRelayReturn((statement as ReturnStatementSyntax)?.Expression);
                }
            }

            return false;
        }

        private static bool IsRelayReturn(ExpressionSyntax expression)
        {
            if (expression == null || !expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return false;
            }

            var memberAccess = (MemberAccessExpressionSyntax)expression;
            if (memberAccess.Expression is IdentifierNameSyntax &&
                memberAccess.Name is IdentifierNameSyntax)
            {
                return true;
            }

            if (memberAccess.Expression is MemberAccessExpressionSyntax &&
                memberAccess.Name is IdentifierNameSyntax)
            {
                return true;
            }

            return false;
        }
    }
}