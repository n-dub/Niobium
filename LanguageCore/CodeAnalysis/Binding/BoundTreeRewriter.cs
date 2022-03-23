using System;
using System.Collections.Generic;
using System.Linq;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal abstract class BoundTreeRewriter
    {
        public virtual BoundStatement RewriteStatement(BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    return RewriteBlockStatement((BoundBlockStatement) node);
                case BoundNodeKind.NopStatement:
                    return RewriteNopStatement((BoundNopStatement) node);
                case BoundNodeKind.VariableDeclarationStatement:
                    return RewriteVariableDeclaration((BoundVariableDeclarationStatement) node);
                case BoundNodeKind.IfStatement:
                    return RewriteIfStatement((BoundIfStatement) node);
                case BoundNodeKind.WhileStatement:
                    return RewriteWhileStatement((BoundWhileStatement) node);
                case BoundNodeKind.RepeatWhileStatement:
                    return RewriteRepeatWhileStatement((BoundRepeatWhileStatement) node);
                case BoundNodeKind.ForStatement:
                    return RewriteForStatement((BoundForStatement) node);
                case BoundNodeKind.LabelStatement:
                    return RewriteLabelStatement((BoundLabelStatement) node);
                case BoundNodeKind.GotoStatement:
                    return RewriteGotoStatement((BoundGotoStatement) node);
                case BoundNodeKind.ConditionalGotoStatement:
                    return RewriteConditionalGotoStatement((BoundConditionalGotoStatement) node);
                case BoundNodeKind.ReturnStatement:
                    return RewriteReturnStatement((BoundReturnStatement) node);
                case BoundNodeKind.ExpressionStatement:
                    return RewriteExpressionStatement((BoundExpressionStatement) node);
                default:
                    throw new Exception($"Unexpected node: {node.Kind}");
            }
        }

        public virtual BoundExpression RewriteExpression(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.ErrorExpression:
                    return RewriteErrorExpression((BoundErrorExpression) node);
                case BoundNodeKind.LiteralExpression:
                    return RewriteLiteralExpression((BoundLiteralExpression) node);
                case BoundNodeKind.VariableExpression:
                    return RewriteVariableExpression((BoundVariableExpression) node);
                case BoundNodeKind.AssignmentExpression:
                    return RewriteAssignmentExpression((BoundAssignmentExpression) node);
                case BoundNodeKind.UnaryExpression:
                    return RewriteUnaryExpression((BoundUnaryExpression) node);
                case BoundNodeKind.BinaryExpression:
                    return RewriteBinaryExpression((BoundBinaryExpression) node);
                case BoundNodeKind.CallExpression:
                    return RewriteCallExpression((BoundCallExpression) node);
                case BoundNodeKind.ConversionExpression:
                    return RewriteConversionExpression((BoundConversionExpression) node);
                default:
                    throw new Exception($"Unexpected node: {node.Kind}");
            }
        }

        protected virtual BoundBlockStatement RewriteBlockStatement(BoundBlockStatement node)
        {
            List<BoundStatement>? statements = null;

            for (var i = 0; i < node.Statements.Count; i++)
            {
                var oldStatement = node.Statements[i];
                var newStatement = RewriteStatement(oldStatement);
                if (newStatement != oldStatement)
                {
                    statements ??= node.Statements.Take(i).ToList();
                }

                statements?.Add(newStatement);
            }

            return statements != null
                ? new BoundBlockStatement(statements.ToArray())
                : node;
        }

        protected virtual BoundStatement RewriteNopStatement(BoundNopStatement node)
        {
            return node;
        }

        protected virtual BoundStatement RewriteVariableDeclaration(BoundVariableDeclarationStatement node)
        {
            var initializer = RewriteExpression(node.Initializer);

            return initializer != node.Initializer
                ? new BoundVariableDeclarationStatement(node.Variable, initializer)
                : node;
        }

        protected virtual BoundStatement RewriteIfStatement(BoundIfStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            var thenBranch = RewriteBlockStatement(node.ThenStatement);
            var elseBranch = node.ElseStatement == null ? null : RewriteBlockStatement(node.ElseStatement);

            return condition != node.Condition || thenBranch != node.ThenStatement || elseBranch != node.ElseStatement
                ? new BoundIfStatement(condition, thenBranch, elseBranch)
                : node;
        }

        protected virtual BoundStatement RewriteWhileStatement(BoundWhileStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            var body = RewriteBlockStatement(node.Body);

            return condition != node.Condition || body != node.Body
                ? new BoundWhileStatement(condition, body, node.BreakLabel, node.ContinueLabel)
                : node;
        }

        protected virtual BoundStatement RewriteRepeatWhileStatement(BoundRepeatWhileStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            var body = RewriteBlockStatement(node.Body);

            return condition != node.Condition || body != node.Body
                ? new BoundRepeatWhileStatement(condition, body, node.BreakLabel, node.ContinueLabel)
                : node;
        }

        protected virtual BoundStatement RewriteForStatement(BoundForStatement node)
        {
            var lowerBound = RewriteExpression(node.LowerBound);
            var upperBound = RewriteExpression(node.UpperBound);
            var body = RewriteBlockStatement(node.Body);

            return lowerBound != node.LowerBound || upperBound != node.UpperBound || body != node.Body
                ? new BoundForStatement(node.Variable, lowerBound, upperBound, body, node.BreakLabel,
                    node.ContinueLabel)
                : node;
        }

        protected virtual BoundStatement RewriteLabelStatement(BoundLabelStatement node)
        {
            return node;
        }

        protected virtual BoundStatement RewriteGotoStatement(BoundGotoStatement node)
        {
            return node;
        }

        protected virtual BoundStatement RewriteConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            var condition = RewriteExpression(node.Condition);
            return condition != node.Condition
                ? new BoundConditionalGotoStatement(node.Label, condition, node.JumpIfTrue)
                : node;
        }

        protected virtual BoundStatement RewriteReturnStatement(BoundReturnStatement node)
        {
            var expression = node.Expression == null ? null : RewriteExpression(node.Expression);
            if (expression == node.Expression)
            {
                return node;
            }

            return new BoundReturnStatement(expression);
        }

        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            var expression = RewriteExpression(node.Expression);

            return expression != node.Expression
                ? new BoundExpressionStatement(expression)
                : node;
        }

        protected virtual BoundExpression RewriteErrorExpression(BoundErrorExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node)
        {
            return node;
        }

        protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            var expression = RewriteExpression(node.Expression);

            return expression != node.Expression
                ? new BoundAssignmentExpression(node.Variable, expression)
                : node;
        }

        protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            var operand = RewriteExpression(node.Operand);

            return operand != node.Operand
                ? new BoundUnaryExpression(node.Op, operand)
                : node;
        }

        protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            var left = RewriteExpression(node.Left);
            var right = RewriteExpression(node.Right);

            return left != node.Left || right != node.Right
                ? new BoundBinaryExpression(left, node.Op, right)
                : node;
        }

        protected virtual BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            List<BoundExpression>? arguments = null;

            for (var i = 0; i < node.Arguments.Count; i++)
            {
                var oldStatement = node.Arguments[i];
                var newStatement = RewriteExpression(oldStatement);
                if (newStatement != oldStatement)
                {
                    arguments ??= node.Arguments.Take(i).ToList();
                }

                arguments?.Add(newStatement);
            }

            return arguments != null
                ? new BoundCallExpression(node.Function, arguments.ToArray())
                : node;
        }

        protected virtual BoundExpression RewriteConversionExpression(BoundConversionExpression node)
        {
            var expression = RewriteExpression(node.Expression);
            if (expression == node.Expression)
            {
                return node;
            }

            return new BoundConversionExpression(node.Type, expression);
        }
    }
}
