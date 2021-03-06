namespace ReflectionAnalyzers.Tests.REFL011DuplicateBindingFlagsTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    internal class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new BindingFlagsAnalyzer();
        private static readonly DiagnosticDescriptor Descriptor = REFL011DuplicateBindingFlags.Descriptor;

        [TestCase("GetMethod(nameof(this.Bar), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(this.Bar), BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)")]
        [TestCase("GetMethod(nameof(this.Bar), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(this.Bar), BindingFlags.Public | BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.Bar), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.Bar), BindingFlags.Public | System.Reflection.BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.Bar), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
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
            var methodInfo = typeof(C).GetMethod(nameof(this.Bar), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        }

        public int Bar() => 0;
    }
}".AssertReplace("GetMethod(nameof(this.Bar), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)", call);
            AnalyzerAssert.Valid(Analyzer, Descriptor, code);
        }

        [TestCase("GetMethod(nameof(this.Bar), Public | Static | DeclaredOnly)")]
        [TestCase("GetMethod(nameof(this.Bar), Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)")]
        [TestCase("GetMethod(nameof(this.Bar), Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(nameof(this.Bar), Public | BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.Bar), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.Bar), BindingFlags.Public | System.Reflection.BindingFlags.Instance)")]
        [TestCase("GetMethod(nameof(this.Bar), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)")]
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
    }
}
