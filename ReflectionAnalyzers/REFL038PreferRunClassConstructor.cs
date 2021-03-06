namespace ReflectionAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class REFL038PreferRunClassConstructor
    {
        internal const string DiagnosticId = "REFL038";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Prefer RuntimeHelpers.RunClassConstructor.",
            messageFormat: "Prefer RuntimeHelpers.RunClassConstructor.",
            category: AnalyzerCategory.SystemReflection,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The static constructor should only be run once. Prefer RuntimeHelpers.RunClassConstructor().",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
