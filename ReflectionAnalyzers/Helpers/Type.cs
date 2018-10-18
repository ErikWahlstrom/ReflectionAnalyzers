namespace ReflectionAnalyzers
{
    using Gu.Roslyn.AnalyzerExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    internal static class Type
    {
        internal static bool TryGet(ExpressionSyntax expression, SyntaxNodeAnalysisContext context, out ITypeSymbol result, out ExpressionSyntax source)
        {
            return TryGet(expression, context, null, out result, out source);
        }

        internal static bool HasVisibleMembers(ITypeSymbol type, BindingFlags flags)
        {
            if (!flags.HasFlagFast(BindingFlags.NonPublic))
            {
                return true;
            }

            if (flags.HasFlagFast(BindingFlags.DeclaredOnly))
            {
                return HasVisibleNonPublicMembers(type, recursive: false);
            }

            if (!flags.HasFlagFast(BindingFlags.Instance) &&
                !flags.HasFlagFast(BindingFlags.FlattenHierarchy))
            {
                return HasVisibleNonPublicMembers(type, recursive: false);
            }

            return HasVisibleNonPublicMembers(type, recursive: true);
        }

        internal static bool HasVisibleNonPublicMembers(ITypeSymbol type, bool recursive)
        {
            if (type == null ||
                type.TypeKind == TypeKind.Interface ||
                type == KnownSymbol.Object)
            {
                return true;
            }

            if (!type.Locations.TryFirst(x => x.IsInSource, out _))
            {
                return false;
            }

            return !recursive || HasVisibleNonPublicMembers(type.BaseType, recursive: true);
        }

        internal static bool IsCastToWrongType(InvocationExpressionSyntax invocation, ITypeSymbol expectedType, SyntaxNodeAnalysisContext context, out TypeSyntax typeSyntax)
        {
            if (context.SemanticModel.IsAccessible(context.Node.SpanStart, expectedType))
            {
                switch (invocation.Parent)
                {
                    case CastExpressionSyntax castExpression when context.SemanticModel.TryGetType(castExpression.Type, context.CancellationToken, out var castType) &&
                                                                  !expectedType.IsAssignableTo(castType, context.Compilation):
                        typeSyntax = castExpression.Type;
                        return true;
                }
            }

            typeSyntax = null;
            return false;
        }

        private static bool TryGet(ExpressionSyntax expression, SyntaxNodeAnalysisContext context, PooledSet<ExpressionSyntax> visited, out ITypeSymbol result, out ExpressionSyntax source)
        {
            switch (expression)
            {
                case IdentifierNameSyntax identifierName when context.SemanticModel.TryGetSymbol(identifierName, context.CancellationToken, out ILocalSymbol local):
#pragma warning disable IDISP003 // Dispose previous before re-assigning.
                    using (visited = visited.IncrementUsage())
#pragma warning restore IDISP003 // Dispose previous before re-assigning.
                    {
                        source = null;
                        result = null;
                        return AssignedValueWalker.TryGetSingle(local, context.SemanticModel, context.CancellationToken, out var assignedValue) &&
                               visited.Add(assignedValue) &&
                               TryGet(assignedValue, context, visited, out result, out source);
                    }

                case MemberAccessExpressionSyntax memberAccess when memberAccess.Name.Identifier.ValueText == "ReturnType" &&
                                                                    memberAccess.Expression is InvocationExpressionSyntax invocation &&
                                                                    GetX.TryMatchGetMethod(invocation, context, out var reflectedMember, out _, out _, out _) &&
                                                                    reflectedMember.Match == FilterMatch.Single &&
                                                                    reflectedMember.Symbol is IMethodSymbol method:
                    source = memberAccess;
                    result = method.ReturnType;
                    return true;
                case MemberAccessExpressionSyntax memberAccess when memberAccess.Name.Identifier.ValueText == "FieldType" &&
                                                                    memberAccess.Expression is InvocationExpressionSyntax invocation &&
                                                                    GetX.TryMatchGetField(invocation, context, out var reflectedMember, out _, out _) &&
                                                                    reflectedMember.Match == FilterMatch.Single &&
                                                                    reflectedMember.Symbol is IFieldSymbol field:
                    source = memberAccess;
                    result = field.Type;
                    return true;
                case MemberAccessExpressionSyntax memberAccess when memberAccess.Name.Identifier.ValueText == "PropertyType" &&
                                                                    memberAccess.Expression is InvocationExpressionSyntax invocation &&
                                                                    GetX.TryMatchGetProperty(invocation, context, out var reflectedMember, out _, out _, out _) &&
                                                                    reflectedMember.Match == FilterMatch.Single &&
                                                                    reflectedMember.Symbol is IPropertySymbol field:
                    source = memberAccess;
                    result = field.Type;
                    return true;
                case TypeOfExpressionSyntax typeOf:
                    source = typeOf;
                    return context.SemanticModel.TryGetType(typeOf.Type, context.CancellationToken, out result);
                case InvocationExpressionSyntax invocation when invocation.ArgumentList is ArgumentListSyntax args &&
                                                                args.Arguments.Count == 0 &&
                                                                invocation.TryGetMethodName(out var name) &&
                                                                name == "GetType":
                    switch (invocation.Expression)
                    {
                        case MemberAccessExpressionSyntax typeAccess:
                            source = invocation;
                            return context.SemanticModel.TryGetType(typeAccess.Expression, context.CancellationToken, out result);
                        case IdentifierNameSyntax _ when expression.TryFirstAncestor(out TypeDeclarationSyntax containingType):
                            source = invocation;
                            return context.SemanticModel.TryGetSymbol(containingType, context.CancellationToken, out result);
                    }

                    break;
                case InvocationExpressionSyntax candidate when IsTypeGetType(candidate, out var typeName, out var ignoreCase):
                    source = candidate;
                    result = context.Compilation.GetTypeByMetadataName(typeName, ignoreCase);
                    return result != null;

                    bool IsTypeGetType(InvocationExpressionSyntax invocation, out string metadataName, out bool ignoreNameCase)
                    {
                        if (invocation.TryGetTarget(KnownSymbol.Type.GetType, context.SemanticModel, context.CancellationToken, out var target) &&
                            target.TryFindParameter("typeName", out var nameParameter) &&
                            invocation.TryFindArgument(nameParameter, out var nameArg) &&
                            nameArg.TryGetStringValue(context.SemanticModel, context.CancellationToken, out metadataName))
                        {
                            switch (target.Parameters.Length)
                            {
                                case 1:
                                    ignoreNameCase = false;
                                    return true;
                                case 2 when target.TryFindParameter("throwOnError", out _):
                                    ignoreNameCase = false;
                                    return true;
                                case 3 when target.TryFindParameter("throwOnError", out _) &&
                                            target.TryFindParameter("ignoreCase", out var ignoreCaseParameter) &&
                                            invocation.TryFindArgument(ignoreCaseParameter, out var ignoreCaseArg) &&
                                            context.SemanticModel.TryGetConstantValue(ignoreCaseArg.Expression, context.CancellationToken, out ignoreNameCase):
                                    return true;
                            }
                        }

                        metadataName = null;
                        ignoreNameCase = false;
                        return false;
                    }

                case InvocationExpressionSyntax candidate when IsAssemblyGetType(candidate, out var typeName, out var ignoreCase):

                    switch (candidate.Expression)
                    {
                        case MemberAccessExpressionSyntax typeAccess when context.SemanticModel.TryGetType(typeAccess.Expression, context.CancellationToken, out var typeInAssembly):
                            source = candidate;
                            result = typeInAssembly.ContainingAssembly.GetTypeByMetadataName(typeName, ignoreCase);
                            return result != null;
                        case IdentifierNameSyntax _ when expression.TryFirstAncestor(out TypeDeclarationSyntax containingType) &&
                                                         context.SemanticModel.TryGetSymbol(containingType, context.CancellationToken, out var typeInAssembly):
                            source = candidate;
                            result = typeInAssembly.ContainingAssembly.GetTypeByMetadataName(typeName, ignoreCase);
                            return result != null;
                    }

                    break;

                    bool IsAssemblyGetType(InvocationExpressionSyntax invocation, out string metadataName, out bool ignoreNameCase)
                    {
                        if (invocation.TryGetTarget(KnownSymbol.Assembly.GetType, context.SemanticModel, context.CancellationToken, out var target) &&
                            target.TryFindParameter("name", out var nameParameter) &&
                            invocation.TryFindArgument(nameParameter, out var nameArg) &&
                            nameArg.TryGetStringValue(context.SemanticModel, context.CancellationToken, out metadataName))
                        {
                            switch (target.Parameters.Length)
                            {
                                case 1:
                                    ignoreNameCase = false;
                                    return true;
                                case 2 when target.TryFindParameter("throwOnError", out _):
                                    ignoreNameCase = false;
                                    return true;
                                case 3 when target.TryFindParameter("throwOnError", out _) &&
                                            target.TryFindParameter("ignoreCase",   out var ignoreCaseParameter) &&
                                            invocation.TryFindArgument(ignoreCaseParameter, out var ignoreCaseArg) &&
                                            context.SemanticModel.TryGetConstantValue(ignoreCaseArg.Expression, context.CancellationToken, out ignoreNameCase):
                                    return true;
                            }
                        }

                        metadataName = null;
                        ignoreNameCase = false;
                        return false;
                    }

                case InvocationExpressionSyntax invocation when invocation.TryGetTarget(KnownSymbol.Type.GetGenericTypeDefinition, context.SemanticModel, context.CancellationToken, out _) &&
                                                                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                                                                TryGet(memberAccess.Expression, context, visited, out var definingType, out _) &&
                                                                definingType is INamedTypeSymbol namedType:
                    source = invocation;
                    result = namedType.ConstructedFrom;
                    return true;

                case InvocationExpressionSyntax invocation when GetX.TryMatchGetNestedType(invocation, context, out var reflectedMember, out _, out _):
                    source = invocation;
                    result = reflectedMember.Symbol as ITypeSymbol;
                    return result != null && reflectedMember.Match == FilterMatch.Single;
                case InvocationExpressionSyntax invocation when invocation.TryGetTarget(KnownSymbol.Type.MakeGenericType, context.SemanticModel, context.CancellationToken, out _) &&
                                                                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                                                                TypeArguments.TryCreate(invocation, context, out var typeArguments) &&
                                                                typeArguments.TryGetArgumentsTypes(context, out var types):
#pragma warning disable IDISP003 // Dispose previous before re-assigning.
                    using (visited = visited.IncrementUsage())
#pragma warning restore IDISP003 // Dispose previous before re-assigning.
                    {
                        if (visited.Add(invocation) &&
                            TryGet(memberAccess.Expression, context, visited, out var definition, out _) &&
                            definition is INamedTypeSymbol namedType)
                        {
                            source = invocation;
                            result = namedType.Construct(types);
                            return result != null;
                        }
                    }

                    break;
            }

            source = null;
            result = null;
            return false;
        }
    }
}
