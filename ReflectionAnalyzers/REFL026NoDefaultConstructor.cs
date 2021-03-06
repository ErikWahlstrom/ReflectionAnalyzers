namespace ReflectionAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class REFL026NoDefaultConstructor
    {
        public const string DiagnosticId = "REFL026";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "No parameterless constructor defined for this object.",
            messageFormat: "No parameterless constructor defined for {0}.",
            category: AnalyzerCategory.SystemReflection,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "No parameterless constructor defined for this object.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
