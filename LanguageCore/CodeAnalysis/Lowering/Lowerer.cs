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

        public static BoundBlockStatement Lower(BoundStatement statement)
        {
            var lowerer = new Lowerer();
            var result = lowerer.RewriteStatement(statement);
            return Flatten(result);
        }

        protected override BoundStatement RewriteForStatement(BoundForStatement node)
        {
            var variableDeclaration = new BoundVariableDeclarationStatement(node.Variable, node.LowerBound);
            var variableExpression = new BoundVariableExpression(node.Variable);
            var upperBoundSymbol = new LocalVariableSymbol("__upperBound", true, TypeSymbol.Int32);
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
                variableDeclaration,
                upperBoundDeclaration,
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
            var checkLabel = GenerateLabel();

            var gotoCheck = new BoundGotoStatement(checkLabel);
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            var checkLabelStatement = new BoundLabelStatement(checkLabel);
            var gotoTrue = new BoundConditionalGotoStatement(node.ContinueLabel, node.Condition);
            var breakLabelStatement = new BoundLabelStatement(node.BreakLabel);
            var result = new BoundBlockStatement(new BoundStatement[]
            {
                gotoCheck,
                continueLabelStatement,
                node.Body,
                checkLabelStatement,
                gotoTrue,
                breakLabelStatement
            });

            return RewriteBlockStatement(result);
        }

        protected override BoundStatement RewriteRepeatWhileStatement(BoundRepeatWhileStatement node)
        {
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            var gotoTrue = new BoundConditionalGotoStatement(node.ContinueLabel, node.Condition);
            var breakLabelStatement = new BoundLabelStatement(node.BreakLabel);
            var result = new BoundBlockStatement(new BoundStatement[]
            {
                continueLabelStatement,
                node.Body,
                gotoTrue,
                breakLabelStatement
            });

            return RewriteBlockStatement(result);
        }

        private static BoundBlockStatement Flatten(BoundStatement statement)
        {
            var builder = new List<BoundStatement>();
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
                    builder.Add(current);
                }
            }

            return new BoundBlockStatement(builder.ToArray());
        }

        private BoundLabel GenerateLabel()
        {
            return new BoundLabel($"label{++labelCount}");
        }
    }
}
