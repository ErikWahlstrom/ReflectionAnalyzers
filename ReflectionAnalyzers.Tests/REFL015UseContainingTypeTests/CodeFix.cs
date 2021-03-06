namespace ReflectionAnalyzers.Tests.REFL015UseContainingTypeTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;
    using ReflectionAnalyzers.Codefixes;

    internal class CodeFix
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GetXAnalyzer();
        private static readonly CodeFixProvider Fix = new UseContainingTypeFix();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create("REFL015");

        [TestCase("GetField(\"PrivateStaticField\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetEvent(\"PrivateStaticEvent\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetProperty(\"PrivateStaticProperty\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(\"PrivateStaticMethod\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetField(\"PrivateStaticField\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)")]
        [TestCase("GetEvent(\"PrivateStaticEvent\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)")]
        [TestCase("GetProperty(\"PrivateStaticProperty\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)")]
        [TestCase("GetMethod(\"PrivateStaticMethod\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)")]
        public void GetPrivateMemberTypeof(string call)
        {
            var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class CBase
    {
        private static int PrivateStaticField;

        private static event EventHandler PrivateStaticEvent;

        private static int PrivateStaticProperty { get; set; }

        private static int PrivateStaticMethod() => 0;
    }
}";
            var code = @"
namespace RoslynSandbox
{
    using System.Reflection;

    public class C : CBase
    {
        public C()
        {
            var member = typeof(↓C).GetField(""PrivateStaticField"", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        }
    }
}".AssertReplace("GetField(\"PrivateStaticField\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)", call);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Reflection;

    public class C : CBase
    {
        public C()
        {
            var member = typeof(CBase).GetField(""PrivateStaticField"", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        }
    }
}".AssertReplace("GetField(\"PrivateStaticField\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)", call);
            var message = "Use the containing type CBase.";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic.WithMessage(message), new[] { baseCode, code }, fixedCode);
        }

        [TestCase("GetField(\"PrivateStaticField\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetEvent(\"PrivateStaticEvent\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetProperty(\"PrivateStaticProperty\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetMethod(\"PrivateStaticMethod\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)")]
        [TestCase("GetField(\"PrivateStaticField\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)")]
        [TestCase("GetEvent(\"PrivateStaticEvent\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)")]
        [TestCase("GetProperty(\"PrivateStaticProperty\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)")]
        [TestCase("GetMethod(\"PrivateStaticMethod\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)")]
        public void GetPrivateMemberThisGetType(string call)
        {
            var baseCode = @"
namespace RoslynSandbox
{
    using System;

    public class CBase
    {
        private static int PrivateStaticField;

        private static event EventHandler PrivateStaticEvent;

        private static int PrivateStaticProperty { get; set; }

        private static int PrivateStaticMethod() => 0;
    }
}";
            var code = @"
namespace RoslynSandbox
{
    using System.Reflection;

    public class C : CBase
    {
        public C()
        {
            var member = ↓this.GetType().GetField(""PrivateStaticField"", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        }
    }
}".AssertReplace("GetField(\"PrivateStaticField\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)", call);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Reflection;

    public class C : CBase
    {
        public C()
        {
            var member = typeof(CBase).GetField(""PrivateStaticField"", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        }
    }
}".AssertReplace("GetField(\"PrivateStaticField\", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)", call);
            var message = "Use the containing type CBase.";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic.WithMessage(message), new[] { baseCode, code }, fixedCode);
        }

        [TestCase("PublicStatic")]
        [TestCase("Public")]
        public void GetPublicNestedType(string type)
        {
            var baseCode = @"
namespace RoslynSandbox
{
    using System.Reflection;

    public class CBase
    {
        public static class PublicStatic
        {
        }

        public class Public
        {
        }
    }
}";
            var code = @"
namespace RoslynSandbox
{
    using System.Reflection;

    public class C : CBase
    {
        public C()
        {
            var typeInfo = typeof(↓C).GetNestedType(nameof(PublicStatic), BindingFlags.Public);
        }
    }
}".AssertReplace("nameof(PublicStatic)", $"nameof({type})");

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Reflection;

    public class C : CBase
    {
        public C()
        {
            var typeInfo = typeof(CBase).GetNestedType(nameof(PublicStatic), BindingFlags.Public);
        }
    }
}".AssertReplace("nameof(PublicStatic)", $"nameof({type})");
            var message = "Use the containing type CBase.";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic.WithMessage(message), new[] { baseCode, code }, fixedCode);
        }

        [TestCase("PrivateStatic")]
        [TestCase("Private")]
        public void GetPrivateNestedType(string type)
        {
            var baseCode = @"
namespace RoslynSandbox
{
    using System.Reflection;

    public class CBase
    {
        private static class PrivateStatic
        {
        }

        private class Private
        {
        }
    }
}";
            var code = @"
namespace RoslynSandbox
{
    using System.Reflection;

    public class C : CBase
    {
        public C()
        {
            var typeInfo = typeof(↓C).GetNestedType(""PrivateStatic"", BindingFlags.NonPublic);
        }
    }
}".AssertReplace("PrivateStatic", type);

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Reflection;

    public class C : CBase
    {
        public C()
        {
            var typeInfo = typeof(CBase).GetNestedType(""PrivateStatic"", BindingFlags.NonPublic);
        }
    }
}".AssertReplace("PrivateStatic", type);

            var message = "Use the containing type CBase.";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic.WithMessage(message), new[] { baseCode, code }, fixedCode);
        }

        [Test]
        public void PrivateFieldInBase()
        {
            var baseCode = @"
namespace RoslynSandbox
{
    class B
    {
        private readonly int field;
    }
}";
            var code = @"
namespace RoslynSandbox
{
    using System.Reflection;

     class C : B
    {
        public object Get => typeof(↓C).GetField(""field"", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}";

            var fixedCode = @"
namespace RoslynSandbox
{
    using System.Reflection;

     class C : B
    {
        public object Get => typeof(B).GetField(""field"", BindingFlags.NonPublic | BindingFlags.Instance);
    }
}";

            var message = "Use the containing type B.";
            AnalyzerAssert.CodeFix(Analyzer, Fix, ExpectedDiagnostic.WithMessage(message), new[] { baseCode, code }, fixedCode);
        }
    }
}
