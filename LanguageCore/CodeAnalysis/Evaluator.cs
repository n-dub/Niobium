using System;
using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis
{
    internal sealed class Evaluator
    {
        private readonly BoundBlockStatement root;
        private readonly Dictionary<VariableSymbol, object> variables;
        private object lastValue;
        private TypeSymbol lastType;

        public Evaluator(BoundBlockStatement root, Dictionary<VariableSymbol, object> variables)
        {
            this.root = root;
            this.variables = variables;
        }

        public object Evaluate(out TypeSymbol type)
        {
            var labelToIndex = new Dictionary<BoundLabel, int>();

            for (var i = 0; i < root.Statements.Count; ++i)
            {
                if (root.Statements[i] is BoundLabelStatement l)
                {
                    labelToIndex.Add(l.Label, i + 1);
                }
            }

            var index = 0;
            while (index < root.Statements.Count)
            {
                var s = root.Statements[index];

                switch (s.Kind)
                {
                    case BoundNodeKind.VariableDeclarationStatement:
                        EvaluateVariableDeclaration((BoundVariableDeclarationStatement) s);
                        index++;
                        break;
                    case BoundNodeKind.ExpressionStatement:
                        EvaluateExpressionStatement((BoundExpressionStatement) s);
                        index++;
                        break;
                    case BoundNodeKind.GotoStatement:
                        var gs = (BoundGotoStatement) s;
                        index = labelToIndex[gs.Label];
                        break;
                    case BoundNodeKind.ConditionalGotoStatement:
                        var cgs = (BoundConditionalGotoStatement) s;
                        var condition = (bool) EvaluateExpression(cgs.Condition);
                        if (condition == cgs.JumpIfTrue)
                        {
                            index = labelToIndex[cgs.Label];
                        }
                        else
                        {
                            index++;
                        }

                        break;
                    case BoundNodeKind.LabelStatement:
                        index++;
                        break;
                    default:
                        throw new Exception($"Unexpected node {s.Kind}");
                }
            }

            type = lastType;
            return lastValue;
        }

        private void EvaluateVariableDeclaration(BoundVariableDeclarationStatement node)
        {
            var value = EvaluateExpression(node.Initializer);
            variables[node.Variable] = value;
            lastType = node.Initializer.Type;
            lastValue = value;
        }

        private void EvaluateExpressionStatement(BoundExpressionStatement node)
        {
            lastType = node.Expression.Type;
            lastValue = EvaluateExpression(node.Expression);
        }

        private object EvaluateExpression(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.LiteralExpression:
                    return EvaluateLiteralExpression((BoundLiteralExpression) node);
                case BoundNodeKind.VariableExpression:
                    return EvaluateVariableExpression((BoundVariableExpression) node);
                case BoundNodeKind.AssignmentExpression:
                    return EvaluateAssignmentExpression((BoundAssignmentExpression) node);
                case BoundNodeKind.UnaryExpression:
                    return EvaluateUnaryExpression((BoundUnaryExpression) node);
                case BoundNodeKind.BinaryExpression:
                    return EvaluateBinaryExpression((BoundBinaryExpression) node);
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        private object EvaluateBinaryExpression(BoundBinaryExpression binary)
        {
            var left = EvaluateExpression(binary.Left);
            var right = EvaluateExpression(binary.Right);

            switch (binary.Op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    return (int) left + (int) right;
                case BoundBinaryOperatorKind.Subtraction:
                    return (int) left - (int) right;
                case BoundBinaryOperatorKind.Multiplication:
                    return (int) left * (int) right;
                case BoundBinaryOperatorKind.Division:
                    return (int) left / (int) right;
                case BoundBinaryOperatorKind.BitwiseAnd when binary.Type == TypeSymbol.Int32:
                    return (int) left & (int) right;
                case BoundBinaryOperatorKind.BitwiseAnd:
                    return (bool) left & (bool) right;
                case BoundBinaryOperatorKind.BitwiseOr when binary.Type == TypeSymbol.Int32:
                    return (int) left | (int) right;
                case BoundBinaryOperatorKind.BitwiseOr:
                    return (bool) left | (bool) right;
                case BoundBinaryOperatorKind.BitwiseXor when binary.Type == TypeSymbol.Int32:
                    return (int) left ^ (int) right;
                case BoundBinaryOperatorKind.BitwiseXor:
                    return (bool) left ^ (bool) right;
                case BoundBinaryOperatorKind.LogicalAnd:
                    return (bool) left && (bool) right;
                case BoundBinaryOperatorKind.LogicalOr:
                    return (bool) left || (bool) right;
                case BoundBinaryOperatorKind.Equals:
                    return Equals(left, right);
                case BoundBinaryOperatorKind.NotEquals:
                    return !Equals(left, right);
                case BoundBinaryOperatorKind.Less:
                    return (int) left < (int) right;
                case BoundBinaryOperatorKind.LessOrEquals:
                    return (int) left <= (int) right;
                case BoundBinaryOperatorKind.Greater:
                    return (int) left > (int) right;
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    return (int) left >= (int) right;
                default:
                    throw new Exception($"Unexpected binary operator {binary.Op}");
            }
        }

        private object EvaluateAssignmentExpression(BoundAssignmentExpression assignment)
        {
            var value = EvaluateExpression(assignment.Expression);
            variables[assignment.Variable] = value;
            return value;
        }

        private object EvaluateVariableExpression(BoundVariableExpression variable)
        {
            return variables[variable.Variable];
        }

        private object EvaluateUnaryExpression(BoundUnaryExpression unary)
        {
            var operand = EvaluateExpression(unary.Operand);

            switch (unary.Op.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    return (int) operand;
                case BoundUnaryOperatorKind.Negation:
                    return -(int) operand;
                case BoundUnaryOperatorKind.LogicalNegation:
                    return !(bool) operand;
                case BoundUnaryOperatorKind.OnesComplement:
                    return ~(int) operand;
                default:
                    throw new Exception($"Unexpected unary operator {unary.Op}");
            }
        }

        private static object EvaluateLiteralExpression(BoundLiteralExpression literal)
        {
            return literal.Value;
        }
    }
}
