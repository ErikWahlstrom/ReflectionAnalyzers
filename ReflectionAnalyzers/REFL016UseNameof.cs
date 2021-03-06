namespace ReflectionAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class REFL016UseNameof
    {
        public const string DiagnosticId = "REFL016";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Use nameof.",
            messageFormat: "Use nameof.",
            category: AnalyzerCategory.SystemReflection,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Use nameof.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
