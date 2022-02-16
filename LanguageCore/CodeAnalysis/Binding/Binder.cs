using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Syntax;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class Binder
    {
        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        private BoundScope scope;

        public Binder(BoundScope parent)
        {
            scope = new BoundScope(parent);
        }

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope previous, CompilationUnitSyntax syntax)
        {
            var parentScope = CreateParentScope(previous);
            var binder = new Binder(parentScope);
            var expression = binder.BindStatement(syntax.Statement);
            var variables = binder.scope.GetDeclaredVariables();
            var diagnostics = (previous?.Diagnostics ?? Enumerable.Empty<Diagnostic>())
                .Concat(binder.Diagnostics)
                .ToArray();

            return new BoundGlobalScope(previous, diagnostics, variables, expression);
        }

        private BoundStatement BindStatement(StatementSyntax syntax)
        {
            switch (syntax.Kind)
            {
                case SyntaxKind.BlockStatement:
                    return BindBlockStatement((BlockStatementSyntax) syntax);
                case SyntaxKind.VariableDeclarationStatement:
                    return BindVariableDeclaration((VariableDeclarationSyntax) syntax);
                case SyntaxKind.IfStatement:
                    return BindIfStatement((IfStatementSyntax) syntax);
                case SyntaxKind.WhileStatement:
                    return BindWhileStatement((WhileStatementSyntax) syntax);
                case SyntaxKind.ForStatement:
                    return BindForStatement((ForStatementSyntax) syntax);
                case SyntaxKind.ExpressionStatement:
                    return BindExpressionStatement((ExpressionStatementSyntax) syntax);
                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private BoundStatement BindIfStatement(IfStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition, typeof(bool));
            var thenStatement = BindBlockStatement(syntax.ThenStatement);
            var elseStatement = syntax.ElseClause == null ? null : BindBlockStatement(syntax.ElseClause.ElseStatement);
            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }

        private BoundStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition, typeof(bool));
            var body = BindBlockStatement(syntax.Body);
            return new BoundWhileStatement(condition, body);
        }

        private BoundStatement BindForStatement(ForStatementSyntax syntax)
        {
            var lowerBound = BindExpression(syntax.LowerBound, typeof(int));
            var upperBound = BindExpression(syntax.UpperBound, typeof(int));

            scope = new BoundScope(scope);

            var name = syntax.Identifier.Text;
            var variable = new VariableSymbol(name, true, typeof(int));
            if (!scope.TryDeclare(variable))
            {
                Diagnostics.ReportVariableAlreadyDeclared(syntax.Identifier.Span, name);
            }

            var body = BindBlockStatement(syntax.Body);

            scope = scope.Parent;

            return new BoundForStatement(variable, lowerBound, upperBound, body);
        }

        private BoundBlockStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            scope = new BoundScope(scope);
            var statements = syntax.Statements
                .Select(BindStatement)
                .ToArray();
            scope = scope.Parent;

            return new BoundBlockStatement(statements);
        }

        private BoundStatement BindVariableDeclaration(VariableDeclarationSyntax syntax)
        {
            var name = syntax.Identifier.Text;
            var immutable = syntax.Keyword.Kind == SyntaxKind.LetKeyword;
            var initializer = BindExpression(syntax.Initializer);
            var variable = new VariableSymbol(name, immutable, initializer.Type);

            if (!scope.TryDeclare(variable))
            {
                Diagnostics.ReportVariableAlreadyDeclared(syntax.Identifier.Span, name);
            }

            return new BoundVariableDeclarationStatement(variable, initializer);
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            var expression = BindExpression(syntax.Expression);
            return new BoundExpressionStatement(expression);
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, Type targetType)
        {
            var result = BindExpression(syntax);
            if (result.Type != targetType)
            {
                Diagnostics.ReportCannotConvert(syntax.Span, result.Type, targetType);
            }

            return result;
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax)
        {
            switch (syntax.Kind)
            {
                case SyntaxKind.LiteralExpression:
                    return BindLiteralExpression((LiteralExpressionSyntax) syntax);
                case SyntaxKind.NameExpression:
                    return BindNameExpression((NameExpressionSyntax) syntax);
                case SyntaxKind.AssignmentExpression:
                    return BindAssignmentExpression((AssignmentExpressionSyntax) syntax);
                case SyntaxKind.UnaryExpression:
                    return BindUnaryExpression((UnaryExpressionSyntax) syntax);
                case SyntaxKind.BinaryExpression:
                    return BindBinaryExpression((BinaryExpressionSyntax) syntax);
                case SyntaxKind.ParenthesizedExpression:
                    return BindParenthesizedExpression((ParenthesizedExpressionSyntax) syntax);
                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private static BoundScope CreateParentScope(BoundGlobalScope previous)
        {
            var stack = new Stack<BoundGlobalScope>();
            while (previous != null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            BoundScope parent = null;

            while (stack.Count > 0)
            {
                previous = stack.Pop();
                var scope = new BoundScope(parent);
                foreach (var v in previous.Variables)
                {
                    scope.TryDeclare(v);
                }

                parent = scope;
            }

            return parent;
        }

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax)
        {
            var name = syntax.IdentifierToken.Text;

            if (scope.TryLookup(name, out var variable))
            {
                return new BoundVariableExpression(variable);
            }

            Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name);
            return new BoundLiteralExpression(0);
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
        {
            var name = syntax.IdentifierToken.Text;
            var boundExpression = BindExpression(syntax.Expression);

            if (!scope.TryLookup(name, out var variable))
            {
                Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name);
                return boundExpression;
            }

            if (variable.IsImmutable)
            {
                Diagnostics.ReportCannotAssign(syntax.EqualsToken.Span, name);
            }

            if (boundExpression.Type != variable.Type)
            {
                Diagnostics.ReportCannotConvert(syntax.Expression.Span, boundExpression.Type, variable.Type);
                return boundExpression;
            }

            return new BoundAssignmentExpression(variable, boundExpression);
        }

        private BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax)
        {
            return BindExpression(syntax.Expression);
        }

        private BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            var value = syntax.Value ?? 0;
            return new BoundLiteralExpression(value);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
        {
            var boundOperand = BindExpression(syntax.Operand);
            var boundOperator = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);

            if (boundOperator == null)
            {
                Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text,
                    boundOperand.Type);
                return boundOperand;
            }

            return new BoundUnaryExpression(boundOperator, boundOperand);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            var boundLeft = BindExpression(syntax.Left);
            var boundRight = BindExpression(syntax.Right);
            var boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);

            if (boundOperator == null)
            {
                Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text,
                    boundLeft.Type, boundRight.Type);
                return boundLeft;
            }

            return new BoundBinaryExpression(boundLeft, boundOperator, boundRight);
        }
    }
}
