namespace ReflectionAnalyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class GetXAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            REFL003MemberDoesNotExist.Descriptor,
            REFL004AmbiguousMatch.Descriptor,
            REFL005WrongBindingFlags.Descriptor,
            REFL006RedundantBindingFlags.Descriptor,
            REFL008MissingBindingFlags.Descriptor,
            REFL009MemberCantBeFound.Descriptor,
            REFL013MemberIsOfWrongType.Descriptor,
            REFL014PreferGetMemberThenAccessor.Descriptor,
            REFL015UseContainingType.Descriptor,
            REFL016UseNameof.Descriptor,
            REFL017DontUseNameofWrongMember.Descriptor,
            REFL018ExplicitImplementation.Descriptor,
            REFL019NoMemberMatchesTheTypes.Descriptor,
            REFL029MissingTypes.Descriptor,
            REFL033UseSameTypeAsParameter.Descriptor,
            REFL045InsufficientFlags.Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.InvocationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (!context.IsExcludedFromAnalysis() &&
                context.Node is InvocationExpressionSyntax invocation &&
                invocation.ArgumentList is ArgumentListSyntax argumentList)
            {
                if (TryGetX(context, out var member, out var name, out var flags, out var types))
                {
                    if (member.Match == FilterMatch.NoMatch)
                    {
                        if (member.ReflectedType?.IsSealed == true ||
                            member.ReflectedType?.IsStatic == true ||
                            member.ReflectedType?.TypeKind == TypeKind.Interface ||
                            member.GetX == KnownSymbol.Type.GetNestedType ||
                            member.GetX == KnownSymbol.Type.GetConstructor ||
                            member.TypeSource is TypeOfExpressionSyntax)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(REFL003MemberDoesNotExist.Descriptor, name.Argument.GetLocation(), member.ReflectedType, name.MetadataName));
                        }
                        else if (!IsNullCheckedAfter(invocation))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(REFL009MemberCantBeFound.Descriptor, name.Argument.GetLocation(), name.MetadataName, member.ReflectedType));
                        }
                    }

                    if (member.Match == FilterMatch.Ambiguous)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL004AmbiguousMatch.Descriptor,
                                argumentList.GetLocation(),
                                ImmutableDictionary<string, string>.Empty.Add(
                                    nameof(INamedTypeSymbol),
                                    member.ReflectedType?.QualifiedMetadataName())));
                    }

                    if (HasWrongFlags(member, flags, out var location, out var flagsText))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL005WrongBindingFlags.Descriptor,
                                location,
                                ImmutableDictionary<string, string>.Empty.Add(nameof(ArgumentSyntax), flagsText),
                                $" Expected: {flagsText}."));
                    }

                    if (HasRedundantFlag(member, flags, out flagsText))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL006RedundantBindingFlags.Descriptor,
                                flags.Argument.GetLocation(),
                                ImmutableDictionary<string, string>.Empty.Add(nameof(ArgumentSyntax), flagsText),
                                $" Expected: {flagsText}."));
                    }

                    if (HasMissingFlags(member, flags, out location, out flagsText))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL008MissingBindingFlags.Descriptor,
                                location,
                                ImmutableDictionary<string, string>.Empty.Add(nameof(ArgumentSyntax), flagsText),
                                $" Expected: {flagsText}."));
                    }

                    if (member.Match == FilterMatch.WrongMemberType)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL013MemberIsOfWrongType.Descriptor,
                                invocation.GetNameLocation(),
                                member.ReflectedType,
                                member.Symbol.Kind.ToString().ToLower(),
                                name.MetadataName));
                    }

                    if (IsPreferGetMemberThenAccessor(member, name, flags, types, context, out var callText))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL014PreferGetMemberThenAccessor.Descriptor,
                                invocation.GetNameLocation(),
                                ImmutableDictionary<string, string>.Empty.Add(
                                    nameof(ExpressionSyntax),
                                    callText),
                                callText));
                    }

                    if (member.Match == FilterMatch.UseContainingType)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL015UseContainingType.Descriptor,
                                TargetTypeLocation(),
                                ImmutableDictionary<string, string>.Empty.Add(
                                    nameof(ISymbol.ContainingType),
                                    member.Symbol.ContainingType.ToString(context)),
                                member.Symbol.ContainingType.Name));
                    }

                    if (ShouldUseNameof(member, name, context, out location, out var nameText))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL016UseNameof.Descriptor,
                                location,
                                ImmutableDictionary<string, string>.Empty.Add(nameof(NameSyntax), nameText)));
                    }

                    if (UsesNameOfWrongMember(member, name, context, out location, out nameText))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL017DontUseNameofWrongMember.Descriptor,
                                location,
                                ImmutableDictionary<string, string>.Empty.Add(nameof(ExpressionSyntax), nameText),
                                nameText));
                    }

                    if (member.Match == FilterMatch.ExplicitImplementation)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL018ExplicitImplementation.Descriptor,
                                TargetTypeLocation(),
                                ImmutableDictionary<string, string>.Empty.Add(
                                    nameof(ISymbol.ContainingType),
                                    member.Symbol.ContainingType.ToString(context)),
                                member.Symbol.Name));
                    }

                    if (member.Match == FilterMatch.WrongTypes)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL019NoMemberMatchesTheTypes.Descriptor,
                                types.Argument?.GetLocation() ?? invocation.GetNameLocation()));
                    }

                    if (HasMissingTypes(member, types, context, out var typeArrayText))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL029MissingTypes.Descriptor,
                                argumentList.GetLocation(),
                                ImmutableDictionary<string, string>.Empty.Add(nameof(TypeSyntax), typeArrayText)));
                    }

                    if (ShouldUseSameTypeAsParameter(member, types, context, out location, out var typeText))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL033UseSameTypeAsParameter.Descriptor,
                                location,
                                ImmutableDictionary<string, string>.Empty.Add(nameof(TypeSyntax), typeText),
                                typeText));
                    }

                    if (member.Match == FilterMatch.InSufficientFlags)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                REFL045InsufficientFlags.Descriptor,
                                flags.Argument?.GetLocation() ?? invocation.GetNameLocation()));
                    }
                }
            }

            Location TargetTypeLocation()
            {
                return invocation.Expression is MemberAccessExpressionSyntax explicitMemberAccess &&
                        explicitMemberAccess.Expression is TypeOfExpressionSyntax typeOf
                    ? typeOf.Type.GetLocation()
                    : invocation.Expression.GetLocation();
            }
        }

        private static bool IsNullCheckedAfter(InvocationExpressionSyntax invocation)
        {
            switch (invocation.Parent)
            {
                case EqualsValueClauseSyntax equalsValueClause when equalsValueClause.Parent is VariableDeclaratorSyntax declarator &&
                                                                    declarator.Parent is VariableDeclarationSyntax declaration &&
                                                                    declaration.Parent is LocalDeclarationStatementSyntax statement &&
                                                                    statement.Parent is BlockSyntax block &&
                                                                    block.Statements.TryElementAt(block.Statements.IndexOf(statement) + 1, out var next) &&
                                                                    next is IfStatementSyntax ifStatement &&
                                                                    IsNullCheck(ifStatement.Condition, declarator.Identifier.ValueText):

                    return true;
                case IsPatternExpressionSyntax _:
                case ConditionalAccessExpressionSyntax _:
                    return true;
            }

            return false;

            bool IsNullCheck(ExpressionSyntax expression, string name)
            {
                switch (expression)
                {
                    case BinaryExpressionSyntax binary when binary.IsEither(SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression) &&
                                                            binary.Right.IsKind(SyntaxKind.NullLiteralExpression) &&
                                                            binary.Left is IdentifierNameSyntax left &&
                                                            left.Identifier.ValueText == name:
                        return true;
                    case IsPatternExpressionSyntax isPattern when isPattern.Expression is IdentifierNameSyntax identifier &&
                                                                  identifier.Identifier.ValueText == name &&
                                                                  isPattern.Pattern is ConstantPatternSyntax constant &&
                                                                  constant.Expression.IsKind(SyntaxKind.NullLiteralExpression):
                        return true;
                }

                return false;
            }
        }

        private static bool TryGetX(SyntaxNodeAnalysisContext context, out ReflectedMember member, out Name name, out Flags flags, out Types types)
        {
            name = default(Name);
            if (context.Node is InvocationExpressionSyntax candidate)
            {
                return GetX.TryMatchGetConstructor(candidate, context, out member, out flags, out types) ||
                       GetX.TryMatchGetEvent(candidate, context, out member, out name, out flags) ||
                       GetX.TryMatchGetField(candidate, context, out member, out name, out flags) ||
                       GetX.TryMatchGetMethod(candidate, context, out member, out name, out flags, out types) ||
                       GetX.TryMatchGetNestedType(candidate, context, out member, out name, out flags) ||
                       GetX.TryMatchGetProperty(candidate, context, out member, out name, out flags, out types);
            }

            member = default(ReflectedMember);
            flags = default(Flags);
            types = default(Types);
            return false;
        }

        private static bool HasMissingFlags(ReflectedMember member, Flags flags, out Location location, out string flagsText)
        {
            if (Flags.TryGetExpectedBindingFlags(member.ReflectedType, member.Symbol, out var correctFlags) &&
                member.Invocation?.ArgumentList is ArgumentListSyntax argumentList &&
                (member.Match == FilterMatch.Single || member.Match == FilterMatch.WrongFlags))
            {
                if (flags.Argument == null)
                {
                    location = MissingFlagsLocation();
                    flagsText = correctFlags.ToDisplayString(member.Invocation);
                    return true;
                }

                if (flags.Argument is ArgumentSyntax argument &&
                    HasMissingFlag())
                {
                    location = argument.GetLocation();
                    flagsText = correctFlags.ToDisplayString(member.Invocation);
                    return true;
                }
            }

            location = null;
            flagsText = null;
            return false;

            bool HasMissingFlag()
            {
                if (member.Symbol is ITypeSymbol ||
                    (member.Symbol is IMethodSymbol method &&
                     method.MethodKind == MethodKind.Constructor))
                {
                    return false;
                }

                return Equals(member.Symbol.ContainingType, member.ReflectedType) &&
                       !flags.Explicit.HasFlagFast(BindingFlags.DeclaredOnly);
            }

            Location MissingFlagsLocation()
            {
                return member.GetX == KnownSymbol.Type.GetConstructor
                    ? argumentList.OpenParenToken.GetLocation()
                    : argumentList.CloseParenToken.GetLocation();
            }
        }

        private static bool HasWrongFlags(ReflectedMember member, Flags flags, out Location location, out string flagText)
        {
            if (member.Match == FilterMatch.WrongFlags &&
                Flags.TryGetExpectedBindingFlags(member.ReflectedType, member.Symbol, out var correctFlags))
            {
                flagText = correctFlags.ToDisplayString(flags.Argument);
                if (flags.Argument is ArgumentSyntax argument)
                {
                    location = argument.GetLocation();
                    return true;
                }

                if (member.Invocation?.ArgumentList is ArgumentListSyntax argumentList)
                {
                    location = member.GetX == KnownSymbol.Type.GetConstructor
                        ? argumentList.OpenParenToken.GetLocation()
                        : argumentList.CloseParenToken.GetLocation();
                    return true;
                }
            }

            location = null;
            flagText = null;
            return false;
        }

        private static bool HasRedundantFlag(ReflectedMember member, Flags flags, out string flagsText)
        {
            if (member.Match != FilterMatch.Single ||
                !member.ReflectedType.Locations.Any(x => x.IsInSource))
            {
                flagsText = null;
                return false;
            }

            if (flags.Argument is ArgumentSyntax argument &&
                Flags.TryGetExpectedBindingFlags(member.ReflectedType, member.Symbol, out var expectedFlags))
            {
                if (member.Symbol is IMethodSymbol method &&
                    method.MethodKind == MethodKind.Constructor &&
                    (flags.Explicit.HasFlagFast(BindingFlags.DeclaredOnly) ||
                     flags.Explicit.HasFlagFast(BindingFlags.FlattenHierarchy)))
                {
                    flagsText = expectedFlags.ToDisplayString(argument);
                    return true;
                }

                if (member.Symbol is ITypeSymbol &&
                    (flags.Explicit.HasFlagFast(BindingFlags.Instance) ||
                     flags.Explicit.HasFlagFast(BindingFlags.Static) ||
                     flags.Explicit.HasFlagFast(BindingFlags.DeclaredOnly) ||
                     flags.Explicit.HasFlagFast(BindingFlags.FlattenHierarchy)))
                {
                    flagsText = expectedFlags.ToDisplayString(argument);
                    return true;
                }

                if ((member.Symbol.DeclaredAccessibility == Accessibility.Public &&
                     flags.Explicit.HasFlagFast(BindingFlags.NonPublic)) ||
                    (member.Symbol.DeclaredAccessibility != Accessibility.Public &&
                     flags.Explicit.HasFlagFast(BindingFlags.Public)) ||
                    (member.Symbol.IsStatic &&
                     flags.Explicit.HasFlagFast(BindingFlags.Instance)) ||
                    (!member.Symbol.IsStatic &&
                     flags.Explicit.HasFlagFast(BindingFlags.Static)) ||
                    (!member.Symbol.IsStatic &&
                     flags.Explicit.HasFlagFast(BindingFlags.FlattenHierarchy)) ||
                    (Equals(member.Symbol.ContainingType, member.ReflectedType) &&
                     flags.Explicit.HasFlagFast(BindingFlags.FlattenHierarchy)) ||
                    (!Equals(member.Symbol.ContainingType, member.ReflectedType) &&
                     flags.Explicit.HasFlagFast(BindingFlags.DeclaredOnly)) ||
                    flags.Explicit.HasFlagFast(BindingFlags.IgnoreCase))
                {
                    flagsText = expectedFlags.ToDisplayString(argument);
                    return true;
                }
            }

            flagsText = null;
            return false;
        }

        private static bool ShouldUseNameof(ReflectedMember member, Name name, SyntaxNodeAnalysisContext context, out Location location, out string nameText)
        {
            if (name.Argument is ArgumentSyntax argument &&
                NameOf.CanUseFor(member.Symbol) &&
                (member.Match == FilterMatch.Single ||
                 member.Match == FilterMatch.Ambiguous ||
                 member.Match == FilterMatch.WrongFlags ||
                 member.Match == FilterMatch.WrongTypes ||
                 (member.Match == FilterMatch.PotentiallyInvisible && member.Symbol is IMethodSymbol)))
            {
                if (argument.Expression is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.StringLiteralExpression) &&
                    NameOf.TryGetExpressionText(member, context, out var expressionText) &&
                    !expressionText.StartsWith("\"", StringComparison.OrdinalIgnoreCase))
                {
                    nameText = $"nameof({expressionText})";
                    location = literal.GetLocation();
                    return true;
                }
            }

            location = null;
            nameText = null;
            return false;
        }

        private static bool UsesNameOfWrongMember(ReflectedMember member, Name name, SyntaxNodeAnalysisContext context, out Location location, out string nameText)
        {
            if (name.Argument is ArgumentSyntax argument &&
                NameOf.IsNameOf(argument, out var expression))
            {
                if (member.Match == FilterMatch.NoMatch ||
                    (member.Match == FilterMatch.PotentiallyInvisible &&
                     !(member.Symbol is IMethodSymbol)))
                {
                    nameText = $"\"{name.MetadataName}\"";
                    location = argument.GetLocation();
                    return true;
                }

                if (member.Symbol is ISymbol memberSymbol &&
                    TryGetSymbol(expression, out var symbol) &&
                    !symbol.ContainingType.IsAssignableTo(memberSymbol.ContainingType, context.Compilation) &&
                    NameOf.TryGetExpressionText(member, context, out nameText))
                {
                    location = expression.GetLocation();
                    return true;
                }
            }

            location = null;
            nameText = null;
            return false;

            bool TryGetSymbol(ExpressionSyntax e, out ISymbol symbol)
            {
                return context.SemanticModel.TryGetSymbol(e, context.CancellationToken, out symbol) ||
                       context.SemanticModel.GetSymbolInfo(e, context.CancellationToken)
                              .CandidateSymbols.TryFirst(out symbol);
            }
        }

        private static bool IsPreferGetMemberThenAccessor(ReflectedMember member, Name name, Flags flags, Types types, SyntaxNodeAnalysisContext context, out string callText)
        {
            if (member.Invocation?.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (member.Symbol is IMethodSymbol method &&
                    member.Match == FilterMatch.Single)
                {
                    if (method.AssociatedSymbol is IPropertySymbol property &&
                        Flags.TryGetExpectedBindingFlags(property.ContainingType, property, out var bindingFlags))
                    {
                        return TryGetPropertyAccessor(MemberName(property), bindingFlags, property.Type, out callText);
                    }

                    if (method.AssociatedSymbol is IEventSymbol eventSymbol &&
                        Flags.TryGetExpectedBindingFlags(eventSymbol.ContainingType, eventSymbol, out bindingFlags))
                    {
                        return TryGetEventAccessor(MemberName(eventSymbol), bindingFlags, out callText);
                    }
                }
                else if (member.Match == FilterMatch.PotentiallyInvisible &&
                         types.Argument == null &&
                         flags.Explicit.HasFlagFast(BindingFlags.NonPublic))
                {
                    if (TryGetInvisibleMemberName("get_", out var memberName) ||
                        TryGetInvisibleMemberName("set_", out memberName))
                    {
                        return TryGetPropertyAccessor(memberName, flags.Explicit, null, out callText);
                    }

                    if (TryGetInvisibleMemberName("add_", out memberName) ||
                        TryGetInvisibleMemberName("remove_", out memberName) ||
                        TryGetInvisibleMemberName("raise_", out memberName))
                    {
                        return TryGetEventAccessor(memberName, flags.Explicit, out callText);
                    }
                }
            }

            callText = null;
            return false;

            bool TryGetPropertyAccessor(string propertyName, BindingFlags bindingFlags, ITypeSymbol type, out string result)
            {
                if (name.MetadataName.StartsWith("get_", StringComparison.Ordinal))
                {
                    result = $"{GetProperty()}.GetMethod";
                    return true;
                }

                if (name.MetadataName.StartsWith("set_", StringComparison.Ordinal))
                {
                    result = $"{GetProperty()}.SetMethod";
                    return true;
                }

                result = null;
                return false;

                string GetProperty()
                {
                    if (flags.Argument == null)
                    {
                        return $"{memberAccess.Expression}.GetProperty({propertyName})";
                    }

                    if (types.Argument == null)
                    {
                        return $"{memberAccess.Expression}.GetProperty({propertyName}, {bindingFlags.ToDisplayString(memberAccess)})";
                    }

                    if (name.MetadataName.StartsWith("get_", StringComparison.Ordinal))
                    {
                        return $"{memberAccess.Expression}.GetProperty({propertyName}, {bindingFlags.ToDisplayString(memberAccess)}, null, typeof({type.ToString(context)}), {types.Argument}, null)";
                    }

                    if (member.Symbol is IMethodSymbol method &&
                        method.AssociatedSymbol is IPropertySymbol property &&
                        property.IsIndexer)
                    {
                        if (property.GetMethod is IMethodSymbol getMethod &&
                            Types.TryGetTypesArrayText(getMethod.Parameters, context.SemanticModel, context.Node.SpanStart, out var typesArrayText))
                        {
                            return $"{memberAccess.Expression}.GetProperty({propertyName}, {bindingFlags.ToDisplayString(memberAccess)}, null, typeof({type.ToString(context)}), {typesArrayText}, null)";
                        }

                        if (property.SetMethod is IMethodSymbol setMethod &&
                            Types.TryGetTypesArrayText(setMethod.Parameters.RemoveAt(setMethod.Parameters.Length - 1), context.SemanticModel, context.Node.SpanStart, out typesArrayText))
                        {
                            return $"{memberAccess.Expression}.GetProperty({propertyName}, {bindingFlags.ToDisplayString(memberAccess)}, null, typeof({type.ToString(context)}), {typesArrayText}, null)";
                        }
                    }

                    return $"{memberAccess.Expression}.GetProperty({propertyName}, {bindingFlags.ToDisplayString(memberAccess)}, null, typeof({type.ToString(context)}), Type.EmptyTypes, null)";
                }
            }

            bool TryGetEventAccessor(string eventName, BindingFlags bindingFlags, out string result)
            {
                if (name.MetadataName.StartsWith("add_", StringComparison.OrdinalIgnoreCase))
                {
                    result = $"{GetEvent()}.AddMethod";
                    return true;
                }

                if (name.MetadataName.StartsWith("remove_", StringComparison.OrdinalIgnoreCase))
                {
                    result = $"{GetEvent()}.RemoveMethod";
                    return true;
                }

                if (name.MetadataName.StartsWith("raise_", StringComparison.OrdinalIgnoreCase))
                {
                    result = $"{GetEvent()}.RaiseMethod";
                    return true;
                }

                result = null;
                return false;

                string GetEvent() => flags.Argument == null
                    ? $"{memberAccess.Expression}.GetEvent({eventName})"
                    : $"{memberAccess.Expression}.GetEvent({eventName}, {bindingFlags.ToDisplayString(memberAccess)})";
            }

            bool TryGetInvisibleMemberName(string prefix, out string memberName)
            {
                if (name.MetadataName is string metadataName &&
                    metadataName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    memberName = $"\"{metadataName.Substring(prefix.Length)}\"";
                    return true;
                }

                memberName = null;
                return false;
            }

            string MemberName(ISymbol associatedSymbol)
            {
                if (associatedSymbol is IPropertySymbol property &&
                    property.IsIndexer)
                {
                    return $"\"{associatedSymbol.MetadataName}\"";
                }

                if (context.ContainingSymbol.ContainingType == associatedSymbol.ContainingType)
                {
                    if (member.Symbol.IsStatic)
                    {
                        return $"nameof({associatedSymbol.Name})";
                    }

                    return context.SemanticModel.UnderscoreFields() ? associatedSymbol.Name : $"nameof(this.{associatedSymbol.Name})";
                }

                return context.SemanticModel.IsAccessible(context.Node.SpanStart, associatedSymbol)
                    ? $"nameof({associatedSymbol.ContainingType.ToString(context)}.{associatedSymbol.Name})"
                    : $"\"{associatedSymbol.Name}\"";
            }
        }

        private static bool HasMissingTypes(ReflectedMember member, Types types, SyntaxNodeAnalysisContext context, out string typesArrayText)
        {
            if ((member.Symbol as IMethodSymbol)?.AssociatedSymbol != null)
            {
                typesArrayText = null;
                return false;
            }

            if (member.Match == FilterMatch.Single &&
                types.Argument == null &&
                member.GetX == KnownSymbol.Type.GetMethod &&
                member.Symbol is IMethodSymbol method &&
                !method.IsGenericMethod)
            {
                return Types.TryGetTypesArrayText(method.Parameters, context.SemanticModel, context.Node.SpanStart, out typesArrayText);
            }

            typesArrayText = null;
            return false;
        }

        private static bool ShouldUseSameTypeAsParameter(ReflectedMember member, Types types, SyntaxNodeAnalysisContext context, out Location location, out string typeText)
        {
            if (types.Argument is ArgumentSyntax argument &&
                member.Symbol is IMethodSymbol method)
            {
                if (method.Parameters.Length != types.Expressions.Length)
                {
                    location = null;
                    typeText = null;
                    return false;
                }

                for (var i = 0; i < method.Parameters.Length; i++)
                {
                    if (!types.Symbols[i].Equals(method.Parameters[i].Type) &&
                        context.SemanticModel.IsAccessible(context.Node.SpanStart, method.Parameters[i].Type))
                    {
                        typeText = method.Parameters[i].Type.ToString(context);
                        var expression = types.Expressions[i];
                        location = argument.Contains(expression)
                            ? expression is TypeOfExpressionSyntax typeOf
                                ? typeOf.Type.GetLocation()
                                : expression.GetLocation()
                            : argument.GetLocation();
                        return true;
                    }
                }
            }

            location = null;
            typeText = null;
            return false;
        }
    }
}
