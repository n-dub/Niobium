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

        private readonly Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> loopStack =
            new Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)>();

        private int labelCounter;
        private BoundScope scope;
        private readonly bool isScript;
        private readonly FunctionSymbol function;

        public Binder(bool isScript, BoundScope parent, FunctionSymbol function)
        {
            scope = new BoundScope(parent);
            this.isScript = isScript;
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

        public static BoundGlobalScope BindGlobalScope(bool isScript, BoundGlobalScope previous,
            IReadOnlyList<SyntaxTree> syntaxTrees)
        {
            var parentScope = CreateParentScope(previous);
            var binder = new Binder(isScript, parentScope, null);

            var functionDeclarations = syntaxTrees
                .SelectMany(st => st.Root.Members)
                .OfType<FunctionDeclarationSyntax>();

            foreach (var function in functionDeclarations)
            {
                binder.BindFunctionDeclaration(function);
            }

            var globalStatements = syntaxTrees.SelectMany(st => st.Root.Members)
                .OfType<GlobalStatementSyntax>()
                .ToArray();

            var statements = globalStatements
                .Select(x => x.Statement)
                .Select(binder.BindGlobalStatement)
                .ToArray();

            var firstGlobalStatementPerSyntaxTree = syntaxTrees
                .Select(st => st.Root.Members.OfType<GlobalStatementSyntax>().FirstOrDefault())
                .Where(g => g != null)
                .ToArray();

            if (firstGlobalStatementPerSyntaxTree.Length > 1)
            {
                foreach (var globalStatement in firstGlobalStatementPerSyntaxTree)
                {
                    binder.Diagnostics.ReportOnlyOneFileCanHaveGlobalStatements(globalStatement.Location);
                }
            }

            var functions = binder.scope.GetDeclaredFunctions();

            FunctionSymbol mainFunction;
            FunctionSymbol scriptFunction;

            if (isScript)
            {
                mainFunction = null;
                if (globalStatements.Any())
                {
                    scriptFunction = new FunctionSymbol("__eval", Array.Empty<ParameterSymbol>(), TypeSymbol.Any);
                }
                else
                {
                    scriptFunction = null;
                }
            }
            else
            {
                mainFunction = functions.FirstOrDefault(f => f.Name == "main");
                scriptFunction = null;

                if (mainFunction != null)
                {
                    if (mainFunction.Type != TypeSymbol.Void || mainFunction.Parameters.Any())
                    {
                        binder.Diagnostics.ReportMainMustHaveCorrectSignature(mainFunction.Declaration.Identifier
                            .Location);
                    }
                }

                if (globalStatements.Any())
                {
                    if (mainFunction != null)
                    {
                        binder.Diagnostics.ReportCannotMixMainAndGlobalStatements(mainFunction.Declaration.Identifier
                            .Location);

                        foreach (var globalStatement in firstGlobalStatementPerSyntaxTree)
                        {
                            binder.Diagnostics.ReportCannotMixMainAndGlobalStatements(globalStatement.Location);
                        }
                    }
                    else
                    {
                        mainFunction = new FunctionSymbol("main", Array.Empty<ParameterSymbol>(), TypeSymbol.Void);
                    }
                }
            }

            var diagnostics = (previous?.Diagnostics ?? Enumerable.Empty<Diagnostic>())
                .Concat(binder.Diagnostics)
                .ToArray();
            var variables = binder.scope.GetDeclaredVariables();

            return new BoundGlobalScope(previous, diagnostics, mainFunction, scriptFunction, functions, variables,
                statements);
        }

        public static BoundProgram BindProgram(bool isScript, BoundProgram previous, BoundGlobalScope globalScope)
        {
            var parentScope = CreateParentScope(globalScope);

            var functionBodies = new Dictionary<FunctionSymbol, BoundBlockStatement>();
            var diagnostics = new List<Diagnostic>();

            foreach (var function in globalScope.Functions)
            {
                var binder = new Binder(isScript, parentScope, function);
                var body = binder.BindStatement(function.Declaration.Body);
                var loweredBody = Lowerer.Lower(function, body);

                if (function.Type != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(loweredBody))
                {
                    binder.Diagnostics.ReportAllPathsMustReturn(function.Declaration.Identifier.Location);
                }

                functionBodies.Add(function, loweredBody);
                diagnostics.AddRange(binder.Diagnostics);
            }

            if (globalScope.MainFunction != null && globalScope.Statements.Any())
            {
                var body = Lowerer.Lower(globalScope.MainFunction, new BoundBlockStatement(globalScope.Statements));
                functionBodies.Add(globalScope.MainFunction, body);
            }
            else if (globalScope.ScriptFunction != null)
            {
                var statements = globalScope.Statements.ToList();
                if (statements.Count == 1 &&
                    statements[0] is BoundExpressionStatement es &&
                    es.Expression.Type != TypeSymbol.Void)
                {
                    statements[0] = new BoundReturnStatement(es.Expression);
                }
                else if (statements.Any() && statements.Last().Kind != BoundNodeKind.ReturnStatement)
                {
                    var nullValue = new BoundLiteralExpression("");
                    statements.Add(new BoundReturnStatement(nullValue));
                }

                var body = Lowerer.Lower(globalScope.ScriptFunction, new BoundBlockStatement(statements));
                functionBodies.Add(globalScope.ScriptFunction, body);
            }

            return new BoundProgram(previous, diagnostics, globalScope.MainFunction, globalScope.ScriptFunction,
                functionBodies);
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
                    Diagnostics.ReportParameterAlreadyDeclared(parameterSyntax.Location, parameterName);
                }
                else
                {
                    var parameter = new ParameterSymbol(parameterName, parameterType, parameters.Count);
                    parameters.Add(parameter);
                }
            }

            var type = BindTypeClause(syntax.Type) ?? TypeSymbol.Void;

            var f = new FunctionSymbol(syntax.Identifier.Text, parameters, type, syntax);
            if (f.Declaration.Identifier.Text != null
                && !scope.TryDeclareFunction(f))
            {
                Diagnostics.ReportSymbolAlreadyDeclared(syntax.Identifier.Location, f.Name);
            }
        }

        private BoundStatement BindErrorStatement()
        {
            return new BoundExpressionStatement(new BoundErrorExpression());
        }

        private BoundStatement BindGlobalStatement(StatementSyntax syntax)
        {
            return BindStatement(syntax, true);
        }

        private BoundStatement BindStatement(StatementSyntax syntax, bool isGlobal = false)
        {
            var result = BindStatementInternal(syntax);

            if (!isScript || !isGlobal)
            {
                if (result is BoundExpressionStatement es)
                {
                    var isAllowedExpression = es.Expression.Kind == BoundNodeKind.ErrorExpression ||
                                              es.Expression.Kind == BoundNodeKind.AssignmentExpression ||
                                              es.Expression.Kind == BoundNodeKind.CallExpression;
                    if (!isAllowedExpression)
                    {
                        Diagnostics.ReportInvalidExpressionStatement(syntax.Location);
                    }
                }
            }

            return result;
        }

        private BoundStatement BindStatementInternal(StatementSyntax syntax)
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
                case SyntaxKind.BreakStatement:
                    return BindBreakStatement((BreakStatementSyntax) syntax);
                case SyntaxKind.ContinueStatement:
                    return BindContinueStatement((ContinueStatementSyntax) syntax);
                case SyntaxKind.ReturnStatement:
                    return BindReturnStatement((ReturnStatementSyntax) syntax);
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
            var body = BindLoopBody(syntax.Body, out var breakLabel, out var continueLabel);
            return new BoundWhileStatement(condition, body, breakLabel, continueLabel);
        }

        private BoundStatement BindRepeatWhileStatement(RepeatWhileStatementSyntax syntax)
        {
            var condition = BindExpression(syntax.Condition, TypeSymbol.Bool);
            var body = BindLoopBody(syntax.Body, out var breakLabel, out var continueLabel);
            return new BoundRepeatWhileStatement(condition, body, breakLabel, continueLabel);
        }

        private BoundStatement BindForStatement(ForStatementSyntax syntax)
        {
            var lowerBound = BindExpression(syntax.LowerBound, TypeSymbol.Int32);
            var upperBound = BindExpression(syntax.UpperBound, TypeSymbol.Int32);

            scope = new BoundScope(scope);

            var variable = BindVariableDeclaration(syntax.Identifier, true, TypeSymbol.Int32);

            var body = BindLoopBody(syntax.Body, out var breakLabel, out var continueLabel);

            scope = scope.Parent;

            return new BoundForStatement(variable, lowerBound, upperBound, body, breakLabel, continueLabel);
        }

        private BoundBlockStatement BindLoopBody(BlockStatementSyntax body,
            out BoundLabel breakLabel, out BoundLabel continueLabel)
        {
            labelCounter++;
            breakLabel = new BoundLabel($"break{labelCounter}");
            continueLabel = new BoundLabel($"continue{labelCounter}");

            loopStack.Push((breakLabel, continueLabel));
            var boundBody = BindBlockStatement(body);
            loopStack.Pop();

            return boundBody;
        }

        private BoundStatement BindBreakStatement(BreakStatementSyntax syntax)
        {
            if (loopStack.Count == 0)
            {
                Diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement();
            }

            var breakLabel = loopStack.Peek().BreakLabel;
            return new BoundGotoStatement(breakLabel);
        }

        private BoundStatement BindContinueStatement(ContinueStatementSyntax syntax)
        {
            if (loopStack.Count == 0)
            {
                Diagnostics.ReportInvalidBreakOrContinue(syntax.Keyword.Location, syntax.Keyword.Text);
                return BindErrorStatement();
            }

            var continueLabel = loopStack.Peek().ContinueLabel;
            return new BoundGotoStatement(continueLabel);
        }

        private BoundStatement BindReturnStatement(ReturnStatementSyntax syntax)
        {
            var expression = syntax.Expression == null ? null : BindExpression(syntax.Expression);

            if (function == null)
            {
                if (isScript)
                {
                    if (expression == null)
                    {
                        expression = new BoundLiteralExpression("");
                    }
                }
                else if (expression != null)
                {
                    Diagnostics.ReportInvalidReturnWithValueInGlobalStatements(syntax.Expression.Location);
                }
            }
            else
            {
                if (function.Type == TypeSymbol.Void)
                {
                    if (expression != null)
                    {
                        Diagnostics.ReportInvalidReturnExpression(syntax.Expression.Location, function.Name);
                    }
                }
                else
                {
                    if (expression == null)
                    {
                        Diagnostics.ReportMissingReturnExpression(syntax.ReturnKeyword.Location, function.Type);
                    }
                    else
                    {
                        expression = BindConversion(syntax.Expression.Location, expression, function.Type);
                    }
                }
            }

            return new BoundReturnStatement(expression);
        }

        private BoundBlockStatement BindBlockStatement(BlockStatementSyntax syntax)
        {
            scope = new BoundScope(scope);
            var statements = syntax.Statements
                .Select(x => BindStatement(x))
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
            var variable =
                BindVariableDeclaration(syntax.Identifier, immutable, variableType, initializer.ConstantValue);
            var convertedInitializer = BindConversion(syntax.Initializer.Location, initializer, variableType);

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
                Diagnostics.ReportUndefinedType(syntax.Identifier.Location, syntax.Identifier.Text);
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
                Diagnostics.ReportExpressionMustHaveValue(syntax.Location);
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
            if (syntax.IdentifierToken.IsMissing)
            {
                return new BoundErrorExpression();
            }

            var variable = BindVariableReference(syntax.IdentifierToken);
            if (variable == null)
            {
                return new BoundErrorExpression();
            }

            return new BoundVariableExpression(variable);
        }

        private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
        {
            var name = syntax.IdentifierToken.Text;
            var boundExpression = BindExpression(syntax.Expression);

            var variable = BindVariableReference(syntax.IdentifierToken);
            if (variable == null)
            {
                return boundExpression;
            }

            if (variable.IsImmutable)
            {
                Diagnostics.ReportCannotAssign(syntax.EqualsToken.Location, name);
            }

            var convertedExpression = BindConversion(syntax.Expression.Location, boundExpression, variable.Type);

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
                Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text,
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
                Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Location, syntax.OperatorToken.Text,
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

            var symbol = scope.TryLookupSymbol(syntax.Identifier.Text);
            if (symbol == null)
            {
                Diagnostics.ReportUndefinedFunction(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression();
            }

            if (!(symbol is FunctionSymbol f))
            {
                Diagnostics.ReportNotAFunction(syntax.Identifier.Location, syntax.Identifier.Text);
                return new BoundErrorExpression();
            }

            if (syntax.Arguments.Count != f.Parameters.Count)
            {
                TextSpan span;
                if (syntax.Arguments.Count > f.Parameters.Count)
                {
                    SyntaxNode firstExceedingNode;
                    if (f.Parameters.Count > 0)
                    {
                        firstExceedingNode = syntax.Arguments.GetSeparator(f.Parameters.Count - 1);
                    }
                    else
                    {
                        firstExceedingNode = syntax.Arguments[0];
                    }

                    var lastExceedingArgument = syntax.Arguments[syntax.Arguments.Count - 1];
                    span = TextSpan.FromBounds(firstExceedingNode.Span.Start, lastExceedingArgument.Span.End);
                }
                else
                {
                    span = syntax.CloseParenthesisToken.Span;
                }

                var location = new TextLocation(syntax.SyntaxTree.SourceText, span);
                Diagnostics.ReportWrongArgumentCount(location, f.Name, f.Parameters.Count,
                    syntax.Arguments.Count);
                return new BoundErrorExpression();
            }

            for (var i = 0; i < syntax.Arguments.Count; i++)
            {
                var argument = boundArguments[i];
                var parameter = f.Parameters[i];

                boundArguments[i] = BindConversion(syntax.Arguments[i].Location, argument, parameter.Type);
            }

            return new BoundCallExpression(f, boundArguments.ToArray());
        }

        private BoundExpression BindConversion(TextLocation diagnosticLocation, BoundExpression expression,
            TypeSymbol type,
            bool allowExplicit = false)
        {
            var conversion = Conversion.Classify(expression.Type, type);

            if (conversion.Exists)
            {
                if (!allowExplicit && conversion.IsExplicit)
                {
                    Diagnostics.ReportCannotConvertImplicitly(diagnosticLocation, expression.Type, type);
                }

                return !conversion.IsIdentity
                    ? new BoundConversionExpression(type, expression)
                    : expression;
            }

            if (expression.Type != TypeSymbol.Error && type != TypeSymbol.Error)
            {
                Diagnostics.ReportCannotConvert(diagnosticLocation, expression.Type, type);
            }

            return new BoundErrorExpression();
        }

        private BoundExpression BindConversion(ExpressionSyntax syntax, TypeSymbol type, bool allowExplicit = false)
        {
            var expression = BindExpression(syntax);
            return BindConversion(syntax.Location, expression, type, allowExplicit);
        }

        private VariableSymbol BindVariableDeclaration(SyntaxToken identifier, bool isImmutable, TypeSymbol type,
            BoundConstant constant = null)
        {
            var name = identifier.Text ?? "?";
            var variable = function == null
                ? (VariableSymbol) new GlobalVariableSymbol(name, isImmutable, type, constant)
                : new LocalVariableSymbol(name, isImmutable, type, constant);

            if (!identifier.IsMissing && !scope.TryDeclareVariable(variable))
            {
                Diagnostics.ReportSymbolAlreadyDeclared(identifier.Location, name);
            }

            return variable;
        }

        private VariableSymbol BindVariableReference(SyntaxToken identifierToken)
        {
            var name = identifierToken.Text;
            var location = identifierToken.Location;
            switch (scope.TryLookupSymbol(name))
            {
                case VariableSymbol variable:
                    return variable;

                case null:
                    Diagnostics.ReportUndefinedVariable(location, name);
                    return null;

                default:
                    Diagnostics.ReportNotAVariable(location, name);
                    return null;
            }
        }

        private TypeSymbol LookupType(string name)
        {
            return TypeSymbol.TryParse(name, out var type) ? type : null;
        }
    }
}
