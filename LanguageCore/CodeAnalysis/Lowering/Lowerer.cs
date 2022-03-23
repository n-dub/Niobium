using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Symbols;
using static LanguageCore.CodeAnalysis.Binding.BoundNodeFactory;

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
            var lowerBound = VariableDeclaration(node.Variable, node.LowerBound);
            var upperBound = ConstantDeclaration("__upperBound", node.UpperBound);
            var result = Block(lowerBound,
                upperBound,
                While(Less(Variable(lowerBound), Variable(upperBound)),
                    Block(node.Body,
                        Label(node.ContinueLabel),
                        Increment(Variable(lowerBound))),
                    node.BreakLabel,
                    GenerateLabel())
            );

            return RewriteBlockStatement(result);
        }

        protected override BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            if (node.ElseStatement == null)
            {
                var endLabel = Label(GenerateLabel());
                var result = Block(
                    GotoFalse(endLabel, node.Condition),
                    node.ThenStatement,
                    endLabel);

                return RewriteBlockStatement(result);
            }
            else
            {
                var elseLabel = Label(GenerateLabel());
                var endLabel = Label(GenerateLabel());
                var result = Block(
                    GotoFalse(elseLabel, node.Condition),
                    node.ThenStatement,
                    Goto(endLabel),
                    elseLabel,
                    node.ElseStatement,
                    endLabel);

                return RewriteBlockStatement(result);
            }
        }

        protected override BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            var bodyLabel = Label(GenerateLabel());
            var result = Block(Goto(node.ContinueLabel),
                bodyLabel,
                node.Body,
                Label(node.ContinueLabel),
                GotoTrue(bodyLabel, node.Condition),
                Label(node.BreakLabel));

            return RewriteBlockStatement(result);
        }

        protected override BoundStatement RewriteRepeatWhileStatement(BoundRepeatWhileStatement node)
        {
            var bodyLabel = Label(GenerateLabel());
            var result = Block(bodyLabel,
                node.Body,
                Label(node.ContinueLabel),
                GotoTrue(bodyLabel, node.Condition),
                Label(node.BreakLabel));

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
                    return RewriteGotoStatement(Goto(node.Label));
                }

                return RewriteNopStatement(Nop());
            }

            return base.RewriteConditionalGotoStatement(node);
        }

        protected override BoundExpression RewriteCompoundAssignmentExpression(BoundCompoundAssignmentExpression node)
        {
            var variableExpression = Variable(node.Variable);
            var expression = Binary(variableExpression, node.Op, node.Expression);
            var assignment = Assignment(node.Variable, expression);

            return RewriteAssignmentExpression(assignment);
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

            return Block(statements.ToArray());
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

            return Block(builder.ToArray());
        }

        private BoundLabel GenerateLabel()
        {
            return new BoundLabel($"label{++labelCount}");
        }
    }
}
