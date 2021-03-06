namespace ReflectionAnalyzers.Tests.REFL036CheckNullTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;
    using ReflectionAnalyzers.Codefixes;

    public class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GetTypeAnalyzer();
        private static readonly CodeFixProvider Fix = new ThrowOnErrorFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(REFL036CheckNull.Descriptor);

        [TestCase("Get() => Type.GetType(\"C\").Assembly",                                                                     "Get() => Type.GetType(\"C\", throwOnError: true).Assembly")]
        [TestCase("Get() => Type.GetType(\"C\", throwOnError: false).Assembly",                                                "Get() => Type.GetType(\"C\", throwOnError: true).Assembly")]
        [TestCase("Get(System.Reflection.Assembly source) => source.GetType(\"C\").Assembly",                                  "Get(System.Reflection.Assembly source) => source.GetType(\"C\", throwOnError: true).Assembly")]
        [TestCase("Get(System.Reflection.Assembly source) => source.GetType(\"C\", throwOnError: false).Assembly",             "Get(System.Reflection.Assembly source) => source.GetType(\"C\", throwOnError: true).Assembly")]
        [TestCase("Get(System.Reflection.Emit.AssemblyBuilder source) => source.GetType(\"C\").Assembly",                      "Get(System.Reflection.Emit.AssemblyBuilder source) => source.GetType(\"C\", throwOnError: true).Assembly")]
        [TestCase("Get(System.Reflection.Emit.AssemblyBuilder source) => source.GetType(\"C\", throwOnError: false).Assembly", "Get(System.Reflection.Emit.AssemblyBuilder source) => source.GetType(\"C\", throwOnError: true).Assembly")]
        public void WhenMemberAccess(string before, string after)
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public static object Get() => Type.GetType(""C"").Assembly;
    }
}".AssertReplace("Get() => Type.GetType(\"C\").Assembly", before);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public static object Get() => Type.GetType(""C"", throwOnError: true).Assembly;
    }
}".AssertReplace("Get() => Type.GetType(\"C\", throwOnError: true).Assembly", after);
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic, code, fixedCode);
        }
    }
}
