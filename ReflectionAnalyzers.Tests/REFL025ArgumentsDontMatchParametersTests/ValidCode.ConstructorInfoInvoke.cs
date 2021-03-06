namespace ReflectionAnalyzers.Tests.REFL025ArgumentsDontMatchParametersTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public partial class ValidCode
    {
        public class ConstructorInfoInvoke
        {
            private static readonly DiagnosticAnalyzer Analyzer = new InvokeAnalyzer();
            private static readonly DiagnosticDescriptor Descriptor = REFL025ArgumentsDontMatchParameters.Descriptor;

            [TestCase("GetConstructor(new[] { typeof(int) }).Invoke(new object[] { 1 })")]
            public void SingleIntParameter(string call)
            {
                var code = @"
namespace RoslynSandbox
{
    public class C
    {
        public C(int value)
        {
            var foo = (int)typeof(C).GetConstructor(new[] { typeof(int) }).Invoke(new object[] { 1 });
        }

        public static int Bar(int value) => value;
    }
}".AssertReplace("GetConstructor(new[] { typeof(int) }).Invoke(new object[] { 1 })", call);

                AnalyzerAssert.Valid(Analyzer, Descriptor, code);
            }
        }
    }
}
