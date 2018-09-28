namespace ReflectionAnalyzers.Tests.REFL025ArgumentsDontMatchParametersTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class Diagnostics
    {
        public class ConstructorInfoInvoke
        {
            private static readonly DiagnosticAnalyzer Analyzer = new InvokeAnalyzer();
            private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(REFL025ArgumentsDontMatchParameters.Descriptor);

            [TestCase("GetConstructor(new[] { typeof(int) }).Invoke(new object[] { ↓1.2 })")]
            public void SingleIntParameter(string call)
            {
                var code = @"
namespace RoslynSandbox
{
    public class Foo
    {
        public Foo(int value)
        {
            var foo = (int)typeof(Foo).GetConstructor(new[] { typeof(int) }).Invoke(new object[] { ↓1.2 });
        }

        public static int Bar(int value) => value;
    }
}".AssertReplace("GetConstructor(new[] { typeof(int) }).Invoke(new object[] { ↓1.2 })", call);

                AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic, code);
            }
        }
    }
}
