using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LanguageCore.CodeAnalysis.Lowering;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;
using LanguageCore.CodeAnalysis.Text;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class Binder
    {
        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        private BoundScope scope;
        private readonly FunctionSymbol function;

        public Binder(BoundScope parent, FunctionSymbol function)
        {
            scope = new BoundScope(parent);
            this.function = function;

            if (function == null)
            {
                return;
            }

            foreach (var p in function.Parameters)
            {
                scope.TryDeclareVariable(p);
            }
        }

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope previous, CompilationUnitSyntax syntax)
        {
            var parentScope = CreateParentScope(previous);
            var binder = new Binder(parentScope, null);

            foreach (var function in syntax.Members.OfType<FunctionDeclarationSyntax>())
            {
                binder.BindFunctionDeclaration(function);
            }

            var statements = syntax.Members
                .OfType<GlobalStatementSyntax>()
                .Select(x => x.Statement)
                .Select(binder.BindStatement)
                .ToArray();

            var statement = new BoundBlockStatement(statements);

            var functions = binder.scope.GetDeclaredFunctions();
            var variables = binder.scope.GetDeclaredVariables();
            var diagnostics = (previous?.Diagnostics ?? Enumerable.Empty<Diagnostic>())
                .Concat(binder.Diagnostics)
                .ToArray();

            return new BoundGlobalScope(previous, diagnostics, functions, variables, statement);
        }

        public static BoundProgram BindProgram(BoundGlobalScope globalScope)
        {
            var parentScope = CreateParentScope(globalScope);

            var functionBodies = new Dictionary<FunctionSymbol, BoundBlockStatement>();
            var diagnostics = new DiagnosticBag();

            var scope = globalScope;
            while (scope != null)
            {
                foreach (var function in scope.Functions)
                {
                    var binder = new Binder(parentScope, function);
                    var body = binder.BindStatement(function.Declaration.Body);
                    var loweredBody = Lowerer.Lower(body);
                    functionBodies.Add(function, loweredBody);

                    diagnostics.AddRange(binder.Diagnostics);
                }

                scope = scope.Previous;
            }

            return new BoundProgram(globalScope, diagnostics, functionBodies);
        }

        private void BindFunctionDeclaration(FunctionDeclarationSyntax syntax)
        {
            var parameters = new List<ParameterSymbol>();

            var seenParameterNames = new HashSet<string>();

            foreach (var parameterSyntax in syntax.Parameters)
            {
                var parameterName = parameterSyntax.Identifier.Text;
                var parameterType = BindTypeClause(parameterSyntax.Type);
                if (!seenParameterNames.Add(parameterName))
                {
                    Diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Span, parameterName);
                }
                else
                {
                    var parameter = new ParameterSymbol(parameterName, parameterType);
                    parameters.Add(parameter);
                }
            }

            var type = BindTypeClause(syntax.Type) ?? TypeSymbol.Void;

            if (type != TypeSymbol.Void)
            {
                Diagnostics.ReportFunctionsAreUnsupported(syntax.Type.Span);
            }

            var f = new FunctionSymbol(syntax.Identifier.Text, parameters, type, syntax);
            if (!scope.TryDeclareFunction(f))
            {
                Diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Span, f.Name);
            }
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
                case SyntaxKind.RepeatWhileStatement:
                    return BindRepeatWhileStatement((RepeatWhileStatementSyntax) syntax);
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
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var thenStatement = BindBlockStatement(syntax.ThenStatement);
            var elseStatement = syntax.ElseClause == null ? null : BindBlockStatement(syntax.ElseClause.ElseStatement);
            return new BoundIfStatement(condition, thenStatement, elseStatement);
        }

        private BoundStatement BindWhileStatement(WhileStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var body = BindBlockStatement(syntax.Body);
            return new BoundWhileStatement(condition, body);
        }

        private BoundStatement BindRepeatWhileStatement(RepeatWhileStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var body = BindBlockStatement(syntax.Body);
            return new BoundRepeatWhileStatement(condition, body);
        }

        private BoundStatement BindForStatement(ForStatementSyntax syntax)
        {
            var lowerBound = BindExpression(syntax.LowerBound, TypeSymbol.Int32);
            var upperBound = BindExpression(syntax.UpperBound, TypeSymbol.Int32);

            scope = new BoundScope(scope);

            var variable = BindVariable(syntax.Identifier, true, TypeSymbol.Int32);

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
            var immutable = syntax.Keyword.Kind == SyntaxKind.LetKeyword;
            var type = BindTypeClause(syntax.TypeClause);
            var initializer = BindExpression(syntax.Initializer);
            var variableType = type ?? initializer.Type;
            var variable = BindVariable(syntax.Identifier, immutable, variableType);
            var convertedInitializer = BindConversion(syntax.Initializer.Span, initializer, variableType);

            return new BoundVariableDeclarationStatement(variable, convertedInitializer);
        }

        private TypeSymbol BindTypeClause(TypeClauseSyntax syntax)
        {
            if (syntax == null)
            {
                return null;
            }

            var type = LookupType(syntax.Identifier.Text);
            if (type == null)
            {
                Diagnostics.ReportUndefinedType(syntax.Identifier.Span, syntax.Identifier.Text);
            }

            return type;
        }

        private BoundStatement BindExpressionStatement(ExpressionStatementSyntax syntax)
        {
            var expression = BindExpression(syntax.Expression, true);
            return new BoundExpressionStatement(expression);
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, TypeSymbol targetType)
        {
            return BindConversion(syntax, targetType);
        }

        private BoundExpression BindExpression(ExpressionSyntax syntax, bool canBeVoid = false)
        {
            var result = BindExpressionInternal(syntax);
            if (!canBeVoid && result.Type == TypeSymbol.Void)
            {
                Diagnostics.ReportExpressionMustHaveValue(syntax.Span);
                return new BoundErrorExpression();
            }

            return result;
        }

        private BoundExpression BindExpressionInternal(ExpressionSyntax syntax)
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
                case SyntaxKind.CallExpression:
                    return BindCallExpression((CallExpressionSyntax) syntax);
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

            var parent = CreateRootScope();

            while (stack.Count > 0)
            {
                previous = stack.Pop();
                var scope = new BoundScope(parent);

                var success = previous.Functions
                    .Select(scope.TryDeclareFunction)
                    .All(x => x);
                success &= previous.Variables
                    .Select(scope.TryDeclareVariable)
                    .All(x => x);

                Debug.Assert(success);

                parent = scope;
            }

            return parent;
        }

        private static BoundScope CreateRootScope()
        {
            var result = new BoundScope(null);
            var success = BuiltinFunctions.GetAll()
                .Select(result.TryDeclareFunction)
                .All(x => x);
            Debug.Assert(success);

            return result;
        }

        private BoundExpression BindNameExpression(NameExpressionSyntax syntax)
        {
            var name = syntax.IdentifierToken.Text;

            if (syntax.IdentifierToken.IsMissing)
            {
                return new BoundErrorExpression();
            }

            if (scope.TryLookupVariable(name, out var variable))
            {
                return new BoundVariableExpression(variable);
            }

            Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name);
            return new BoundErrorExpression();
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
        {
            var name = syntax.IdentifierToken.Text;
            var boundExpression = BindExpression(syntax.Expression);

            if (!scope.TryLookupVariable(name, out var variable))
            {
                Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name);
                return boundExpression;
            }

            if (variable.IsImmutable)
            {
                Diagnostics.ReportCannotAssign(syntax.EqualsToken.Span, name);
            }

            var convertedExpression = BindConversion(syntax.Expression.Span, boundExpression, variable.Type);

            return new BoundAssignmentExpression(variable, convertedExpression);
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

            if (boundOperand.Type == TypeSymbol.Error)
            {
                return new BoundErrorExpression();
            }

            if (boundOperator == null)
            {
                Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text,
                    boundOperand.Type);
                return new BoundErrorExpression();
            }

            return new BoundUnaryExpression(boundOperator, boundOperand);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            var boundLeft = BindExpression(syntax.Left);
            var boundRight = BindExpression(syntax.Right);

            if (boundLeft.Type == TypeSymbol.Error || boundRight.Type == TypeSymbol.Error)
            {
                return new BoundErrorExpression();
            }

            var boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);

            if (boundOperator == null)
            {
                Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text,
                    boundLeft.Type, boundRight.Type);
                return new BoundErrorExpression();
            }

            return new BoundBinaryExpression(boundLeft, boundOperator, boundRight);
        }

        private BoundExpression BindCallExpression(CallExpressionSyntax syntax)
        {
            if (syntax.Arguments.Count == 1 && LookupType(syntax.Identifier.Text) is TypeSymbol type)
            {
                return BindConversion(syntax.Arguments[0], type, true);
            }

            var boundArguments = syntax.Arguments
                .Select(argument => BindExpression(argument))
                .ToList();

            if (!scope.TryLookupFunction(syntax.Identifier.Text, out var f))
            {
                Diagnostics.ReportUndefinedFunction(syntax.Identifier.Span, syntax.Identifier.Text);
                return new BoundErrorExpression();
            }

            if (syntax.Arguments.Count != f.Parameters.Count)
            {
                Diagnostics.ReportWrongArgumentCount(syntax.Span, f.Name, f.Parameters.Count,
                    syntax.Arguments.Count);
                return new BoundErrorExpression();
            }

            for (var i = 0; i < syntax.Arguments.Count; i++)
            {
                var argument = boundArguments[i];
                var parameter = f.Parameters[i];

                if (argument.Type != parameter.Type)
                {
                    Diagnostics.ReportWrongArgumentType(syntax.Arguments[i].Span, parameter.Name, parameter.Type,
                        argument.Type);
                    return new BoundErrorExpression();
                }
            }

            return new BoundCallExpression(f, boundArguments.ToArray());
        }

        private BoundExpression BindConversion(TextSpan diagnosticSpan, BoundExpression expression, TypeSymbol type,
            bool allowExplicit = false)
        {
            var conversion = Conversion.Classify(expression.Type, type);

            if (conversion.Exists)
            {
                if (!allowExplicit && conversion.IsExplicit)
                {
                    Diagnostics.ReportCannotConvertImplicitly(diagnosticSpan, expression.Type, type);
                }

                return !conversion.IsIdentity
                    ? new BoundConversionExpression(type, expression)
                    : expression;
            }

            if (expression.Type != TypeSymbol.Error && type != TypeSymbol.Error)
            {
                Diagnostics.ReportCannotConvert(diagnosticSpan, expression.Type, type);
            }

            return new BoundErrorExpression();
        }

        private BoundExpression BindConversion(ExpressionSyntax syntax, TypeSymbol type, bool allowExplicit = false)
        {
            var expression = BindExpression(syntax);
            return BindConversion(syntax.Span, expression, type, allowExplicit);
        }

        private VariableSymbol BindVariable(SyntaxToken identifier, bool isReadOnly, TypeSymbol type)
        {
            var name = identifier.Text ?? "?";
            var variable = function == null
                ? (VariableSymbol) new GlobalVariableSymbol(name, isReadOnly, type)
                : new LocalVariableSymbol(name, isReadOnly, type);

            if (!identifier.IsMissing && !scope.TryDeclareVariable(variable))
            {
                Diagnostics.ReportSymbolAlreadyDeclared(identifier.Span, name);
            }

            return variable;
        }

        private TypeSymbol LookupType(string name)
        {
            return TypeSymbol.TryParse(name, out var type) ? type : null;
        }
    }
}
