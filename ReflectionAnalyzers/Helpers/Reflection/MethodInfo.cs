namespace ReflectionAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    internal struct MethodInfo
    {
        internal readonly INamedTypeSymbol ReflectedType;
        internal readonly IMethodSymbol Method;

        public MethodInfo(INamedTypeSymbol reflectedType, IMethodSymbol method)
        {
            this.ReflectedType = reflectedType;
            this.Method = method;
        }

        internal static bool TryGet(ExpressionSyntax expression, SyntaxNodeAnalysisContext context, out MethodInfo methodInfo)
        {
            switch (expression)
            {
                case InvocationExpressionSyntax invocation when GetX.TryMatchGetMethod(invocation, context, out var member, out _, out _, out _) && member.Symbol is IMethodSymbol method:
                    methodInfo = new MethodInfo(member.ReflectedType, method);
                    return true;
                case InvocationExpressionSyntax invocation when invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                                                                context.SemanticModel.TryGetSymbol(invocation, KnownSymbol.PropertyInfo.GetGetMethod, context.CancellationToken, out _) &&
                                                                PropertyInfo.TryGet(memberAccess.Expression, context, out var propertyInfo) &&
                                                                propertyInfo.Property.GetMethod is IMethodSymbol getMethod:
                    methodInfo = new MethodInfo(propertyInfo.ReflectedType, getMethod);
                    return true;
                case InvocationExpressionSyntax invocation when invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                                                                context.SemanticModel.TryGetSymbol(invocation, KnownSymbol.PropertyInfo.GetSetMethod, context.CancellationToken, out _) &&
                                                                PropertyInfo.TryGet(memberAccess.Expression, context, out var propertyInfo) &&
                                                                propertyInfo.Property.SetMethod is IMethodSymbol setMethod:
                    methodInfo = new MethodInfo(propertyInfo.ReflectedType, setMethod);
                    return true;
                case MemberAccessExpressionSyntax memberAccess when context.SemanticModel.TryGetSymbol(memberAccess, context.CancellationToken, out ISymbol symbol):
                    if (symbol == KnownSymbol.PropertyInfo.GetMethod &&
                        PropertyInfo.TryGet(memberAccess.Expression, context, out var property))
                    {
                            methodInfo = new MethodInfo(property.ReflectedType, property.Property.GetMethod);
                            return true;
                    }

                    if (symbol == KnownSymbol.PropertyInfo.SetMethod &&
                        PropertyInfo.TryGet(memberAccess.Expression, context, out property))
                    {
                        methodInfo = new MethodInfo(property.ReflectedType, property.Property.SetMethod);
                        return true;
                    }

                    break;
            }

            if (expression.IsEither(SyntaxKind.IdentifierName, SyntaxKind.SimpleMemberAccessExpression) &&
                context.SemanticModel.TryGetSymbol(expression, context.CancellationToken, out ISymbol local))
            {
                methodInfo = default(MethodInfo);
                return AssignedValue.TryGetSingle(local, context.SemanticModel, context.CancellationToken, out var assignedValue) &&
                       TryGet(assignedValue, context, out methodInfo);
            }

            methodInfo = default(MethodInfo);
            return false;
        }
    }
}
