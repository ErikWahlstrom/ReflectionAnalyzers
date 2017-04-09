namespace Gu.Analyzers
{
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static partial class Disposable
    {
        private class RecursiveValues : IEnumerator<ExpressionSyntax>
        {
            private static readonly ConcurrentQueue<RecursiveValues> Cache = new ConcurrentQueue<RecursiveValues>();
            private readonly List<ExpressionSyntax> values = new List<ExpressionSyntax>();
            private readonly HashSet<SyntaxNode> checkedLocations = new HashSet<SyntaxNode>();

            private int rawIndex = -1;
            private int recursiveIndex = -1;
            private IReadOnlyList<ExpressionSyntax> rawValues;
            private SemanticModel semanticModel;
            private CancellationToken cancellationToken;

            private RecursiveValues()
            {
            }

            object IEnumerator.Current => this.Current;

            public int Count => this.rawValues.Count;

            public ExpressionSyntax Current => this.values[this.recursiveIndex];

            public static RecursiveValues Create(IReadOnlyList<ExpressionSyntax> rawValues, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var item = Cache.GetOrCreate(() => new RecursiveValues());
                item.rawValues = rawValues;
                item.semanticModel = semanticModel;
                item.cancellationToken = cancellationToken;
                return item;
            }

            public bool MoveNext()
            {
                if (this.recursiveIndex < this.values.Count - 1)
                {
                    this.recursiveIndex++;
                    return true;
                }

                if (this.rawIndex < this.rawValues.Count - 1)
                {
                    this.rawIndex++;
                    if (!this.AddRecursiveValues(this.rawValues[this.rawIndex]))
                    {
                        return this.MoveNext();
                    }

                    this.recursiveIndex++;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                this.recursiveIndex = -1;
            }

            public void Dispose()
            {
                this.values.Clear();
                this.checkedLocations.Clear();
                this.rawValues = null;
                this.rawIndex = -1;
                this.recursiveIndex = -1;
                this.semanticModel = null;
                this.cancellationToken = CancellationToken.None;
                Cache.Enqueue(this);
            }

            private bool AddRecursiveValues(ExpressionSyntax assignedValue)
            {
                if (assignedValue == null ||
                    assignedValue.IsMissing ||
                    !this.checkedLocations.Add(assignedValue))
                {
                    return false;
                }

                if (assignedValue is LiteralExpressionSyntax ||
                    assignedValue is DefaultExpressionSyntax ||
                    assignedValue is TypeOfExpressionSyntax ||
                    assignedValue is ObjectCreationExpressionSyntax ||
                    assignedValue is ArrayCreationExpressionSyntax ||
                    assignedValue is ImplicitArrayCreationExpressionSyntax ||
                    assignedValue is InitializerExpressionSyntax)
                {
                    this.values.Add(assignedValue);
                    return true;
                }

                var argument = assignedValue.Parent as ArgumentSyntax;
                if (argument?.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword) == true)
                {
                    var invocation = assignedValue.FirstAncestor<InvocationExpressionSyntax>();
                    var invokedMethod = this.semanticModel.GetSymbolSafe(invocation, this.cancellationToken);
                    if (invokedMethod == null ||
                        invokedMethod.DeclaringSyntaxReferences.Length == 0)
                    {
                        this.values.Add(invocation);
                        return true;
                    }

                    var before = this.values.Count;
                    foreach (var reference in invokedMethod.DeclaringSyntaxReferences)
                    {
                        var methodDeclaration = reference.GetSyntax(this.cancellationToken) as MethodDeclarationSyntax;
                        if (methodDeclaration.TryGetMatchingParameter(argument, out ParameterSyntax parameter))
                        {
                            using (var pooled = AssignedValueWalker.Create(this.semanticModel.GetDeclaredSymbolSafe(parameter, this.cancellationToken), this.semanticModel, this.cancellationToken))
                            {
                                pooled.Item.HandleInvoke(invokedMethod, invocation.ArgumentList);
                                return this.AddManyRecursively(pooled.Item);
                            }
                        }
                    }

                    return before != this.values.Count;
                }

                var binaryExpression = assignedValue as BinaryExpressionSyntax;
                if (binaryExpression != null)
                {
                    switch (binaryExpression.Kind())
                    {
                        case SyntaxKind.CoalesceExpression:
                            var left = this.AddRecursiveValues(binaryExpression.Left);
                            var right = this.AddRecursiveValues(binaryExpression.Right);
                            return left || right;
                        case SyntaxKind.AsExpression:
                            return this.AddRecursiveValues(binaryExpression.Left);
                        default:
                            return false;
                    }
                }

                var cast = assignedValue as CastExpressionSyntax;
                if (cast != null)
                {
                    return this.AddRecursiveValues(cast.Expression);
                }

                var conditional = assignedValue as ConditionalExpressionSyntax;
                if (conditional != null)
                {
                    var whenTrue = this.AddRecursiveValues(conditional.WhenTrue);
                    var whenFalse = this.AddRecursiveValues(conditional.WhenFalse);
                    return whenTrue || whenFalse;
                }

                var @await = assignedValue as AwaitExpressionSyntax;
                if (@await != null)
                {
                    using (var pooled = ReturnValueWalker.Create(@await, true, this.semanticModel, this.cancellationToken))
                    {
                        return this.AddManyRecursively(pooled.Item);
                    }
                }

                var symbol = this.semanticModel.GetSymbolSafe(assignedValue, this.cancellationToken);
                if (symbol == null)
                {
                    return false;
                }

                if (symbol is IFieldSymbol)
                {
                    this.values.Add(assignedValue);
                    return true;
                }

                if (symbol is IParameterSymbol)
                {
                    this.values.Add(assignedValue);
                    using (var pooled = AssignedValueWalker.Create(assignedValue, this.semanticModel, this.cancellationToken))
                    {
                        return this.AddManyRecursively(pooled.Item);
                    }
                }

                if (symbol is ILocalSymbol)
                {
                    using (var pooled = AssignedValueWalker.Create(assignedValue, this.semanticModel, this.cancellationToken))
                    {
                        return this.AddManyRecursively(pooled.Item);
                    }
                }

                var property = symbol as IPropertySymbol;
                if (property != null)
                {
                    if (property.DeclaringSyntaxReferences.Length == 0)
                    {
                        this.values.Add(assignedValue);
                        return true;
                    }

                    using (var returnValues = ReturnValueWalker.Create(assignedValue, true, this.semanticModel, this.cancellationToken))
                    {
                        return this.AddManyRecursively(returnValues.Item);
                    }
                }

                var method = symbol as IMethodSymbol;
                if (method != null)
                {
                    if (method.DeclaringSyntaxReferences.Length == 0)
                    {
                        this.values.Add(assignedValue);
                        return true;
                    }

                    using (var pooled = ReturnValueWalker.Create(assignedValue, true, this.semanticModel, this.cancellationToken))
                    {
                        return this.AddManyRecursively(pooled.Item);
                    }
                }

                return false;
            }

            private bool AddManyRecursively(IReadOnlyList<ExpressionSyntax> newValues)
            {
                var addedAny = false;
                foreach (var value in newValues)
                {
                    addedAny |= this.AddRecursiveValues(value);
                }

                return addedAny;
            }
        }
    }
}