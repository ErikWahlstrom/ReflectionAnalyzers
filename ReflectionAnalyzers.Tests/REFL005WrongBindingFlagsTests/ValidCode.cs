namespace ReflectionAnalyzers.Tests.REFL005WrongBindingFlagsTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GetXAnalyzer();
        private static readonly DiagnosticDescriptor Descriptor = REFL005WrongBindingFlags.Descriptor;

        [TestCase("GetMethod(nameof(Static))")]
        [TestCase("GetMethod(nameof(Static), BindingFlags.Public | BindingFlags.Static)")]
        [TestCase("GetMethod(nameof(Static), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(StaticPublicPrivate))")]
        [TestCase("GetMethod(nameof(StaticPublicPrivate), BindingFlags.Public | BindingFlags.Static)")]
        [TestCase("GetMethod(nameof(StaticPublicPrivate), BindingFlags.NonPublic | BindingFlags.Static)")]
        [TestCase("GetMethod(nameof(StaticPublicPrivate), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(StaticPublicPrivate), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(ReferenceEquals), BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)")]
        [TestCase("GetMethod(nameof(ReferenceEquals), BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy)")]
        [TestCase("GetMethod(nameof(this.Public))")]
        [TestCase("GetMethod(nameof(this.Public), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase)")]
        [TestCase("GetMethod(nameof(this.Public), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(this.Public), BindingFlags.Public | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(this.PublicPrivate))")]
        [TestCase("GetMethod(nameof(this.PublicPrivate), BindingFlags.Public | BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.PublicPrivate), BindingFlags.NonPublic | BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.PublicPrivate), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(this.PublicPrivate), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(this.ToString))")]
        [TestCase("GetMethod(nameof(this.ToString), BindingFlags.Public | BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.ToString), BindingFlags.Instance | BindingFlags.Public)")]
        [TestCase("GetMethod(nameof(this.ToString), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(this.ToString), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase)")]
        [TestCase("GetMethod(nameof(this.GetHashCode))")]
        [TestCase("GetMethod(nameof(this.GetHashCode), BindingFlags.Public | BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.Private), BindingFlags.NonPublic | BindingFlags.NonPublic | BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.Private), BindingFlags.NonPublic | BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.Private), BindingFlags.Instance | BindingFlags.NonPublic)")]
        [TestCase("GetMethod(nameof(this.Private), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(this.Private), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase)")]
        public void GetMethod(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System.Reflection;

    class C
    {
        public C()
        {
            var methodInfo = typeof(C).GetMethod(nameof(this.Public), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        public static int Static() => 0;

        public static int StaticPublicPrivate() => 0;

        public int Public() => 0;

        public int PublicPrivate() => 0;

        public override string ToString() => string.Empty;

        private static int StaticPublicPrivate(int i) => i;

        private int Private() => 0;

        private int PublicPrivate(int i) => i;
    }
}".AssertReplace("GetMethod(nameof(this.Public), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)", call);
            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [TestCase("GetMethod(nameof(this.ToString))")]
        [TestCase("GetMethod(nameof(this.ToString), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        public void GetMethodWhenShadowed(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System.Reflection;

    class C
    {
        public C()
        {
            var methodInfo = typeof(C).GetMethod(nameof(this.ToString), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        public new string ToString() => string.Empty;
    }
}".AssertReplace("GetMethod(nameof(this.ToString), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)", call);
            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [TestCase("GetMethod(nameof(Static), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new[] { typeof(int) }, null)")]
        [TestCase("GetMethod(nameof(Static), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new[] { i.GetType() }, null)")]
        [TestCase("GetMethod(nameof(Static), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new Type[] { typeof(int) }, null)")]
        [TestCase("GetMethod(nameof(Static), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new Type[1] { typeof(int) }, null)")]
        [TestCase("GetMethod(nameof(this.Instance), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new[] { typeof(int) }, null)")]
        [TestCase("GetMethod(nameof(this.Instance), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new[] { i.GetType() }, null)")]
        [TestCase("GetMethod(nameof(this.Instance), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new Type[] { typeof(int) }, null)")]
        [TestCase("GetMethod(nameof(this.Instance), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new Type[1] { typeof(int) }, null)")]
        public void OverloadsFilteredByType(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System;
    using System.Reflection;

    class C
    {
        public C(int i)
        {
            var methodInfo = typeof(C).GetMethod(nameof(Static), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new[] { typeof(int) }, null);
        }

        public static double Static(int value) => value;

        public static double Static(double value) => value;

        public int Instance(int value) => value;

        public double Instance(double value) => value;
    }
}".AssertReplace("GetMethod(nameof(Static), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new[] { typeof(int) }, null)", call);

            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [TestCase("GetMethod(\"Bar\")")]
        [TestCase("GetMethod(\"Bar\", BindingFlags.Public | BindingFlags.Instance)")]
        [TestCase("GetMethod(\"Bar\", BindingFlags.NonPublic | BindingFlags.Instance)")]
        [TestCase("GetMethod(\"Bar\", BindingFlags.Public | BindingFlags.Static)")]
        [TestCase("GetMethod(\"Bar\", BindingFlags.NonPublic | BindingFlags.Static)")]
        [TestCase("GetMethod(\"Bar\", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)")]
        [TestCase("GetMethod(\"Bar\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)")]
        [TestCase("GetMethod(\"Bar\", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(\"Bar\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        public void GetMethodUnknownType(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System;
    using System.Reflection;

    class C
    {
        public C(Type type)
        {
            var methodInfo = type.GetMethod(""Bar"");
        }
    }
}".AssertReplace("GetMethod(\"Bar\")", call);
            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [TestCase("GetMethod(nameof(this.Bar), Public | Instance)")]
        [TestCase("GetMethod(nameof(this.Bar), Public | Instance | DeclaredOnly)")]
        [TestCase("GetMethod(nameof(this.Bar), Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)")]
        [TestCase("GetMethod(nameof(this.Bar), Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(this.Bar), Public | BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.Bar), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.Bar), BindingFlags.Public | System.Reflection.BindingFlags.Instance)")]
        public void GetMethodUsingStatic(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System.Reflection;
    using static System.Reflection.BindingFlags;

    class C
    {
        public C()
        {
            var methodInfo = typeof(C).GetMethod(nameof(this.Bar), Public | Static | DeclaredOnly);
        }

        public int Bar() => 0;
    }
}".AssertReplace("GetMethod(nameof(this.Bar), Public | Static | DeclaredOnly)", call);
            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [TestCase("GetProperty(nameof(Static), BindingFlags.Public | BindingFlags.Static)")]
        [TestCase("GetProperty(nameof(Static), BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic)")]
        [TestCase("GetProperty(nameof(Static), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetProperty(nameof(Static), BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)")]
        [TestCase("GetProperty(nameof(this.Public), BindingFlags.Public | BindingFlags.Instance)")]
        [TestCase("GetProperty(nameof(this.Public), BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)")]
        [TestCase("GetProperty(nameof(this.Public), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        [TestCase("GetProperty(nameof(this.Public), BindingFlags.Public | BindingFlags.Instance | BindingFlags.Instance)")]
        [TestCase("GetProperty(nameof(PrivateStatic), BindingFlags.NonPublic | BindingFlags.Static)")]
        [TestCase("GetProperty(nameof(PrivateStatic), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.NonPublic)")]
        [TestCase("GetProperty(nameof(PrivateStatic), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetProperty(nameof(PrivateStatic), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)")]
        [TestCase("GetProperty(nameof(this.Private), BindingFlags.NonPublic | BindingFlags.Instance)")]
        [TestCase("GetProperty(nameof(this.Private), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.NonPublic)")]
        [TestCase("GetProperty(nameof(this.Private), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        [TestCase("GetProperty(nameof(this.Private), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase)")]
        public void GetProperty(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System.Reflection;

    class C
    {
        public C()
        {
            var methodInfo = typeof(C).GetProperty(nameof(this.Public), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        public static int Static => 0;

        public int Public => 0;

        private static int PrivateStatic => 0;

        private int Private => 0;
    }
}".AssertReplace("GetProperty(nameof(this.Public), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)", call);
            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [TestCase("GetNestedType(nameof(PublicStatic))")]
        [TestCase("GetNestedType(nameof(PublicStatic), BindingFlags.Public)")]
        [TestCase("GetNestedType(nameof(PublicStatic), BindingFlags.Public | BindingFlags.DeclaredOnly)")]
        [TestCase("GetNestedType(nameof(PublicStatic), BindingFlags.Public | BindingFlags.Instance)")]
        [TestCase("GetNestedType(nameof(PublicStatic), BindingFlags.Public | BindingFlags.Static)")]
        [TestCase("GetNestedType(nameof(Public))")]
        [TestCase("GetNestedType(nameof(Public), BindingFlags.Public)")]
        [TestCase("GetNestedType(nameof(Public), BindingFlags.Public | BindingFlags.DeclaredOnly)")]
        [TestCase("GetNestedType(nameof(Public), BindingFlags.Public | BindingFlags.Instance)")]
        [TestCase("GetNestedType(nameof(Public), BindingFlags.Public | BindingFlags.Static)")]
        [TestCase("GetNestedType(nameof(PrivateStatic), BindingFlags.NonPublic)")]
        [TestCase("GetNestedType(nameof(PrivateStatic), BindingFlags.NonPublic | BindingFlags.Public)")]
        [TestCase("GetNestedType(nameof(PrivateStatic), BindingFlags.NonPublic | BindingFlags.DeclaredOnly)")]
        [TestCase("GetNestedType(nameof(PrivateStatic), BindingFlags.NonPublic | BindingFlags.Instance)")]
        [TestCase("GetNestedType(nameof(PrivateStatic), BindingFlags.NonPublic | BindingFlags.Static)")]
        [TestCase("GetNestedType(nameof(Private), BindingFlags.NonPublic)")]
        [TestCase("GetNestedType(nameof(Private), BindingFlags.NonPublic | BindingFlags.Public)")]
        [TestCase("GetNestedType(nameof(Private), BindingFlags.NonPublic | BindingFlags.DeclaredOnly)")]
        [TestCase("GetNestedType(nameof(Private), BindingFlags.NonPublic | BindingFlags.Instance)")]
        [TestCase("GetNestedType(nameof(Private), BindingFlags.NonPublic | BindingFlags.Static)")]
        public void GetNestedType(string call)
        {
            var code = @"
namespace RoslynSandbox
{
    using System.Reflection;

    class C
    {
        public C()
        {
            var methodInfo = typeof(C).GetNestedType(nameof(Public), BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        public static class PublicStatic
        {
        }

        public class Public
        {
        }

        private static class PrivateStatic
        {
        }

        private class Private
        {
        }
    }
}".AssertReplace("GetNestedType(nameof(Public), BindingFlags.Public | BindingFlags.DeclaredOnly)", call);
            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [Test]
        public void NonPublicNotVisible()
        {
            var exception = @"
namespace RoslynSandbox
{
    using System;

    public class CustomAggregateException : AggregateException
    {
        public int InnerExceptionCount { get; }
    }
}";
            var code = @"
namespace RoslynSandbox
{
    using System;
    using System.Reflection;

    class C
    {
        public C()
        {
            var member = typeof(CustomAggregateException).GetProperty(""InnerExceptionCount"", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}";

            AnalyzerAssert.Valid(Analyzer, Descriptor, exception, code);
        }
    }
}
