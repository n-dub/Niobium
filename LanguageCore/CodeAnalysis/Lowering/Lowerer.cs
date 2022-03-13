using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;

namespace LanguageCore.CodeAnalysis.Lowering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private int labelCount;

        private Lowerer()
        {
        }

        public static BoundBlockStatement Lower(FunctionSymbol function, BoundStatement statement)
        {
            var lowerer = new Lowerer();
            var result = lowerer.RewriteStatement(statement);
            return RemoveDeadCode(Flatten(function, result));
        }

        protected override BoundStatement RewriteForStatement(BoundForStatement node)
        {
            var variableDeclaration = new BoundVariableDeclarationStatement(node.Variable, node.LowerBound);
            var variableExpression = new BoundVariableExpression(node.Variable);
            var upperBoundSymbol =
                new LocalVariableSymbol("__upperBound", true, TypeSymbol.Int32, node.UpperBound.ConstantValue);
            var upperBoundDeclaration = new BoundVariableDeclarationStatement(upperBoundSymbol, node.UpperBound);

            var condition = new BoundBinaryExpression(
                variableExpression,
                BoundBinaryOperator.Bind(SyntaxKind.LessToken, TypeSymbol.Int32, TypeSymbol.Int32),
                new BoundVariableExpression(upperBoundSymbol)
            );
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            var increment = new BoundExpressionStatement(
                new BoundAssignmentExpression(
                    node.Variable,
                    new BoundBinaryExpression(
                        variableExpression,
                        BoundBinaryOperator.Bind(SyntaxKind.PlusToken, TypeSymbol.Int32, TypeSymbol.Int32),
                        new BoundLiteralExpression(1)
                    )
                )
            );
            var whileBody = new BoundBlockStatement(new BoundStatement[]
            {
                node.Body, continueLabelStatement, increment
            });
            var whileStatement = new BoundWhileStatement(condition, whileBody, node.BreakLabel, GenerateLabel());
            var result = new BoundBlockStatement(new BoundStatement[]
            {
                upperBoundDeclaration,
                variableDeclaration,
                whileStatement
            });

            return RewriteBlockStatement(result);
        }

        protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            if (node.ElseStatement == null)
            {
                var endLabel = GenerateLabel();

                var gotoFalse = new BoundConditionalGotoStatement(endLabel, node.Condition, false);
                var endLabelStatement = new BoundLabelStatement(endLabel);
                var result = new BoundBlockStatement(new BoundStatement[]
                {
                    gotoFalse,
                    node.ThenStatement,
                    endLabelStatement
                });

                return RewriteBlockStatement(result);
            }
            else
            {
                var elseLabel = GenerateLabel();
                var endLabel = GenerateLabel();

                var gotoFalse = new BoundConditionalGotoStatement(elseLabel, node.Condition, false);
                var gotoEndStatement = new BoundGotoStatement(endLabel);
                var elseLabelStatement = new BoundLabelStatement(elseLabel);
                var endLabelStatement = new BoundLabelStatement(endLabel);
                var result = new BoundBlockStatement(new BoundStatement[]
                {
                    gotoFalse,
                    node.ThenStatement,
                    gotoEndStatement,
                    elseLabelStatement,
                    node.ElseStatement,
                    endLabelStatement
                });

                return RewriteBlockStatement(result);
            }
        }

        protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            var bodyLabel = GenerateLabel();

            var gotoContinue = new BoundGotoStatement(node.ContinueLabel);
            var bodyLabelStatement = new BoundLabelStatement(bodyLabel);
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            var gotoTrue = new BoundConditionalGotoStatement(bodyLabel, node.Condition);
            var breakLabelStatement = new BoundLabelStatement(node.BreakLabel);
            var result = new BoundBlockStatement(new BoundStatement[]
            {
                gotoContinue,
                bodyLabelStatement,
                node.Body,
                continueLabelStatement,
                gotoTrue,
                breakLabelStatement
            });

            return RewriteBlockStatement(result);
        }

        protected override BoundStatement RewriteRepeatWhileStatement(BoundRepeatWhileStatement node)
        {
            var bodyLabel = GenerateLabel();

            var bodyLabelStatement = new BoundLabelStatement(bodyLabel);
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            var gotoTrue = new BoundConditionalGotoStatement(bodyLabel, node.Condition);
            var breakLabelStatement = new BoundLabelStatement(node.BreakLabel);
            var result = new BoundBlockStatement(new BoundStatement[]
            {
                bodyLabelStatement,
                node.Body,
                continueLabelStatement,
                gotoTrue,
                breakLabelStatement
            });

            return RewriteBlockStatement(result);
        }

        protected override BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            if (node.Condition.ConstantValue != null)
            {
                var condition = (bool) node.Condition.ConstantValue.Value;
                condition = node.JumpIfTrue ? condition : !condition;
                if (condition)
                {
                    return new BoundGotoStatement(node.Label);
                }

                return new BoundNopStatement();
            }

            return base.RewriteConditionalGotoStatement(node);
        }

        private static BoundBlockStatement Flatten(FunctionSymbol function, BoundStatement statement)
        {
            var statements = new List<BoundStatement>();
            var stack = new Stack<BoundStatement>();
            stack.Push(statement);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current is BoundBlockStatement block)
                {
                    foreach (var s in block.Statements.Reverse())
                    {
                        stack.Push(s);
                    }
                }
                else
                {
                    statements.Add(current);
                }
            }

            if (function.Type == TypeSymbol.Void)
            {
                if (statements.Count == 0 || CanFallThrough(statements.Last()))
                {
                    statements.Add(new BoundReturnStatement(null));
                }
            }

            return new BoundBlockStatement(statements.ToArray());
        }

        private static bool CanFallThrough(BoundStatement boundStatement)
        {
            return boundStatement.Kind != BoundNodeKind.ReturnStatement &&
                   boundStatement.Kind != BoundNodeKind.GotoStatement;
        }

        private static BoundBlockStatement RemoveDeadCode(BoundBlockStatement node)
        {
            var controlFlow = ControlFlowGraph.Create(node);
            var reachableStatements = new HashSet<BoundStatement>(
                controlFlow.Blocks.SelectMany(b => b.Statements));

            var builder = node.Statements.ToList();
            for (var i = builder.Count - 1; i >= 0; i--)
            {
                if (!reachableStatements.Contains(builder[i]))
                {
                    builder.RemoveAt(i);
                }
            }

            return new BoundBlockStatement(builder);
        }

        private BoundLabel GenerateLabel()
        {
            return new BoundLabel($"label{++labelCount}");
        }
    }
}
