namespace ReflectionAnalyzers.Tests.REFL026MissingDefaultConstructorTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ActivatorAnalyzer();
        private static readonly DiagnosticDescriptor Descriptor = REFL026NoDefaultConstructor.Descriptor;

        [TestCase("Activator.CreateInstance(typeof(C))")]
        [TestCase("Activator.CreateInstance(typeof(C), true)")]
        [TestCase("Activator.CreateInstance(typeof(C), false)")]
        [TestCase("Activator.CreateInstance(this.GetType())")]
        [TestCase("Activator.CreateInstance<C>()")]
        public void ExplicitDefaultConstructor(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public C()
        {
            var foo = Activator.CreateInstance(typeof(C));
        }
    }
}".AssertReplace("Activator.CreateInstance(typeof(C))", call);

            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [TestCase("(C)Activator.CreateInstance(typeof(C))")]
        [TestCase("Activator.CreateInstance<C>()")]
        public void ImplicitDefaultConstructor(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public static C Create() => Activator.CreateInstance<C>();
    }
}".AssertReplace("Activator.CreateInstance<C>()", call);

            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [TestCase("Activator.CreateInstance(typeof(C), 1)")]
        [TestCase("Activator.CreateInstance(typeof(C), new object[] { 1 })")]
        public void OneConstructorSingleIntParameter(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public C(int i)
        {
            var foo = Activator.CreateInstance(typeof(C));
        }
    }
}".AssertReplace("Activator.CreateInstance(typeof(C))", call);

            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [TestCase("Activator.CreateInstance(typeof(C), \"abc\")")]
        [TestCase("Activator.CreateInstance(typeof(C), new object[] { null })")]
        [TestCase("Activator.CreateInstance(typeof(C), (string)null)")]
        public void OneConstructorSingleStringParameter(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public C(string text)
        {
            var foo = Activator.CreateInstance(typeof(C), ""abc"");
        }
    }
}".AssertReplace("Activator.CreateInstance(typeof(C), \"abc\")", call);

            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [Test]
        public void PrivateConstructor()
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        private C()
        {
            var foo = Activator.CreateInstance(typeof(C), true);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [Test]
        public void WhenUnknown()
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public static object Bar(Type type) => Activator.CreateInstance(type, ""foo"");
    }
}";

            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [Test]
        public void WhenUnconstrainedGeneric()
        {
            var code = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public static object Bar<T>() => Activator.CreateInstance(typeof(T), ""foo"");
    }
}";

            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }
    }
}
