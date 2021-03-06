namespace ReflectionAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class REFL028CastReturnValueToCorrectType
    {
        public const string DiagnosticId = "REFL028";

        internal static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Cast return value to correct type.",
            messageFormat: "Cast return value to {0}.",
            category: AnalyzerCategory.SystemReflection,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Cast return value to correct type.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));
    }
}
