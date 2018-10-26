namespace ReflectionAnalyzers.Tests.REFL026MissingDefaultConstructorTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ActivatorAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(REFL026NoDefaultConstructor.Descriptor);

        [TestCase("Activator.CreateInstance<↓Foo>()")]
        [TestCase("Activator.CreateInstance(typeof(↓Foo))")]
        public void OneConstructorSingleIntParameter(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(int i)
        {
            var foo = Activator.CreateInstance(typeof(↓Foo));
        }
    }
}".AssertReplace("Activator.CreateInstance(typeof(↓Foo))", call);

            var message = "No parameterless constructor defined for RoslynSandbox.Foo.";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage(message), code);
        }

        [TestCase("Activator.CreateInstance<↓Foo>()")]
        [TestCase("Activator.CreateInstance(typeof(↓Foo))")]
        [TestCase("Activator.CreateInstance(typeof(↓Foo), false)")]
        public void PrivateConstructor(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        private Foo()
        {
        }

        public Foo Create() => Activator.CreateInstance<↓Foo>();
    }
}".AssertReplace("Activator.CreateInstance<↓Foo>()", call);

            var message = "No parameterless constructor defined for RoslynSandbox.Foo.";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage(message), code);
        }

        [TestCase("Activator.CreateInstance<↓Foo>()")]
        [TestCase("Activator.CreateInstance(typeof(↓Foo))")]
        public void OneConstructorSingleParams(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class Foo
    {
        public Foo(params int[] values)
        {
        }

        public Foo Create() => Activator.CreateInstance<↓Foo>();
    }
}".AssertReplace("Activator.CreateInstance<↓Foo>()", call);

            var message = "No parameterless constructor defined for RoslynSandbox.Foo.";
            AnalyzerAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage(message), code);
        }
    }
}