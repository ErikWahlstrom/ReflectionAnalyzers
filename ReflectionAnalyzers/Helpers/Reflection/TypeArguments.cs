namespace ReflectionAnalyzers
{
    using System.Collections.Immutable;
    using System.Linq;
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    internal struct TypeArguments
    {
        internal readonly ISymbol Symbol;
        internal readonly ImmutableArray<ITypeParameterSymbol> Parameters;
        internal readonly ImmutableArray<ExpressionSyntax> Arguments;

        public TypeArguments(ISymbol symbol, ImmutableArray<ITypeParameterSymbol> parameters, ImmutableArray<ExpressionSyntax> arguments)
        {
            this.Symbol = symbol;
            this.Parameters = parameters;
            this.Arguments = arguments;
        }

        internal static bool TryCreate(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context, out TypeArguments typeArguments)
        {
            if (invocation?.ArgumentList is ArgumentListSyntax argumentList &&
                (TryGetTypeParameters(invocation, context, out var symbol, out var parameters) ||
                 TryGetMethodParameters(invocation, context, out symbol, out parameters)))
            {
                if (argumentList.Arguments.TrySingle(out var argument) &&
                    Array.TryGetValues(argument.Expression, context, out var arrayExpressions))
                {
                    typeArguments = new TypeArguments(symbol, parameters, arrayExpressions);
                    return true;
                }

                if (!IsUnknownArray())
                {
                    typeArguments = new TypeArguments(symbol, parameters, ArgumentsExpressions());
                    return true;
                }
            }

            typeArguments = default(TypeArguments);
            return false;

            ImmutableArray<ExpressionSyntax> ArgumentsExpressions()
            {
                var builder = ImmutableArray.CreateBuilder<ExpressionSyntax>(argumentList.Arguments.Count);
                foreach (var arg in argumentList.Arguments)
                {
                    builder.Add(arg.Expression);
                }

                return builder.ToImmutable();
            }

            bool IsUnknownArray()
            {
                return argumentList.Arguments.TrySingle(out var single) &&
                       single.Expression is IdentifierNameSyntax identifierName &&
                       context.SemanticModel.TryGetType(identifierName, context.CancellationToken, out var type) &&
                       type is IArrayTypeSymbol;
            }
        }

        internal bool TryFindConstraintViolation(SyntaxNodeAnalysisContext context, out ExpressionSyntax argument, out ITypeParameterSymbol parameter)
        {
            for (var i = 0; i < this.Parameters.Length; i++)
            {
                if (Type.TryGet(this.Arguments[i], context, out var type, out _) &&
                    !this.SatisfiesConstraints(type, this.Parameters[i], context))
                {
                    argument = this.Arguments[i];
                    parameter = this.Parameters[i];
                    return true;
                }
            }

            argument = null;
            parameter = null;
            return false;
        }

        internal bool TryGetArgumentsTypes(SyntaxNodeAnalysisContext context, out ITypeSymbol[] types)
        {
            if (this.Arguments.Length == 0)
            {
                types = System.Array.Empty<ITypeSymbol>();
                return false;
            }

            types = new ITypeSymbol[this.Arguments.Length];
            for (var i = 0; i < this.Arguments.Length; i++)
            {
                var argument = this.Arguments[i];
                if (Type.TryGet(argument, context, out var type, out _))
                {
                    types[i] = type;
                }
                else
                {
                    types = System.Array.Empty<ITypeSymbol>();
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetTypeParameters(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context, out ISymbol symbol, out ImmutableArray<ITypeParameterSymbol> parameters)
        {
            if (IsMakeGeneric(invocation, KnownSymbol.Type.MakeGenericType, context) &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (Type.TryGet(memberAccess.Expression, context, out var type, out _) &&
                    type is INamedTypeSymbol namedType)
                {
                    symbol = type;
                    parameters = namedType.TypeParameters;

                    while (type.ContainingType is INamedTypeSymbol containingType)
                    {
                        parameters = parameters.InsertRange(0, containingType.TypeParameters);
                        type = containingType;
                    }

                    return true;
                }
            }

            symbol = null;
            return false;
        }

        private static bool TryGetMethodParameters(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context, out ISymbol symbol, out ImmutableArray<ITypeParameterSymbol> parameters)
        {
            if (IsMakeGeneric(invocation, KnownSymbol.MethodInfo.MakeGenericMethod, context) &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (GetX.TryGetMethodInfo(memberAccess, context, out var method))
                {
                    symbol = method;
                    parameters = method.TypeParameters;
                    return true;
                }
            }

            symbol = null;
            return false;
        }

        private static bool IsMakeGeneric(InvocationExpressionSyntax invocation, QualifiedMethod expected, SyntaxNodeAnalysisContext context)
        {
            return invocation.TryGetTarget(expected, context.SemanticModel, context.CancellationToken, out var makeGeneric) &&
                   makeGeneric.Parameters.TrySingle(out var parameter) &&
                   parameter.IsParams &&
                   parameter.Type is IArrayTypeSymbol arrayType &&
                   arrayType.ElementType == KnownSymbol.Type;
        }

        private bool SatisfiesConstraints(ITypeSymbol type, ITypeParameterSymbol typeParameter, SyntaxNodeAnalysisContext context)
        {
            if (typeParameter.HasConstructorConstraint)
            {
                switch (type)
                {
                    case INamedTypeSymbol namedType when !namedType.Constructors.TryFirst(x => x.DeclaredAccessibility == Accessibility.Public && x.Parameters.Length == 0, out _):
                    case ITypeParameterSymbol parameter when !parameter.HasConstructorConstraint:
                        return false;
                }
            }

            if (typeParameter.HasReferenceTypeConstraint)
            {
                switch (type)
                {
                    case INamedTypeSymbol namedType when !namedType.IsReferenceType:
                    case ITypeParameterSymbol parameter when parameter.HasValueTypeConstraint ||
                                                             IsValueTypeContext(context.Node, parameter):
                        return false;
                }
            }

            if (typeParameter.HasValueTypeConstraint)
            {
                switch (type)
                {
                    case INamedTypeSymbol namedType when !namedType.IsValueType || namedType == KnownSymbol.NullableOfT:
                    case ITypeParameterSymbol parameter when parameter.HasReferenceTypeConstraint ||
                                                             !IsValueTypeContext(context.Node, parameter):
                        return false;
                }
            }

            foreach (var constraintType in typeParameter.ConstraintTypes)
            {
                switch (constraintType)
                {
                    case ITypeParameterSymbol parameter when this.TryFindArgumentType(parameter, context, out var argumentType):
                        if (!IsAssignableTo(type, argumentType))
                        {
                            return false;
                        }

                        break;
                    case INamedTypeSymbol namedType:
                        if (!IsAssignableTo(type, namedType))
                        {
                            if (namedType.IsGenericType)
                            {
                                if (namedType.TypeArguments.All(x => x.TypeKind != TypeKind.TypeParameter))
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                }
            }

            return true;

            bool IsValueTypeContext(SyntaxNode node, ITypeParameterSymbol candidate)
            {
                if (node.TryFirstAncestor(out ConditionalExpressionSyntax ternary))
                {
                    if (ternary.WhenTrue.Contains(node) &&
                        TryGetEffectivelyValueType(ternary.Condition, out var result))
                    {
                        return result;
                    }
                    else if (ternary.WhenFalse.Contains(node) &&
                             TryGetEffectivelyValueType(ternary.Condition, out result))
                    {
                        return !result;
                    }

                    return IsValueTypeContext(ternary, candidate);
                }

                if (node.TryFirstAncestor(out IfStatementSyntax ifStatement))
                {
                    if (ifStatement.Statement.Contains(node) &&
                        TryGetEffectivelyValueType(ifStatement.Condition, out var result))
                    {
                        return result;
                    }
                    else if (ifStatement.Else?.Contains(node) == true &&
                             TryGetEffectivelyValueType(ifStatement.Condition, out result))
                    {
                        return !result;
                    }

                    return IsValueTypeContext(ifStatement, candidate);
                }

                return false;

                bool TryGetEffectivelyValueType(ExpressionSyntax condition, out bool result)
                {
                    switch (condition)
                    {
                        case MemberAccessExpressionSyntax memberAccess when
                            memberAccess.Name.Identifier.Text == "IsValueType" &&
                            memberAccess.Expression is TypeOfExpressionSyntax typeOf &&
                            typeOf.Type is IdentifierNameSyntax identifierName &&
                            identifierName.Identifier.Text == candidate.Name:
                            result = true;
                            return true;
                        case PrefixUnaryExpressionSyntax prefixUnary when
                             prefixUnary.IsKind(SyntaxKind.LogicalNotExpression):
                            return !TryGetEffectivelyValueType(prefixUnary.Operand, out result);
                        case BinaryExpressionSyntax binary when
                             binary.IsKind(SyntaxKind.LogicalAndExpression):
                            return TryGetEffectivelyValueType(binary.Left, out result) ||
                                   TryGetEffectivelyValueType(binary.Right, out result);
                    }

                    result = false;
                    return false;
                }
            }

            bool IsAssignableTo(ITypeSymbol source, ITypeSymbol destination)
            {
                var conversion = context.Compilation.ClassifyConversion(source, destination);
                return conversion.IsIdentity ||
                       conversion.IsImplicit;
            }
        }

        private bool TryFindArgumentType(ITypeParameterSymbol parameter, SyntaxNodeAnalysisContext context, out ITypeSymbol type)
        {
            var i = this.Parameters.IndexOf(parameter);
            if (i >= 0)
            {
                return Type.TryGet(this.Arguments[i], context, out type, out _);
            }

            type = null;
            return false;
        }
    }
}
