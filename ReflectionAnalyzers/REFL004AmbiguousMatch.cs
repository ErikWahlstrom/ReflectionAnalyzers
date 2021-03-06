namespace ReflectionAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class REFL004AmbiguousMatch
    {
        public const string DiagnosticId = "REFL004";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "More than one member is matching the criteria.",
            messageFormat: "More than one member is matching the criteria.",
            category: AnalyzerCategory.SystemReflection,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "More than one member is matching the criteria.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
