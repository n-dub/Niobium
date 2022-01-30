using System;
using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Binding;

namespace LanguageCore.CodeAnalysis
{
    public sealed class Evaluator
    {
        private readonly BoundExpression root;
        private readonly Dictionary<string, object> variables;

        public Evaluator(BoundExpression root, Dictionary<string, object> variables)
        {
            this.root = root;
            this.variables = variables;
        }

        public object Evaluate()
        {
            return EvaluateExpression(root);
        }

        private object EvaluateExpression(BoundExpression node)
        {
            switch (node)
            {
                case BoundLiteralExpression literal:
                {
                    return literal.Value;
                }
                case BoundUnaryExpression unary:
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
                        default:
                            throw new Exception($"Unexpected unary operator {unary.Op}");
                    }
                }
                case BoundVariableExpression variable:
                {
                    return variables[variable.Name];
                }
                case BoundAssignmentExpression assignment:
                {
                    var value = EvaluateExpression(assignment.Expression);
                    variables[assignment.Name] = value;
                    return value;
                }
                case BoundBinaryExpression binary:
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
                        case BoundBinaryOperatorKind.LogicalAnd:
                            return (bool) left && (bool) right;
                        case BoundBinaryOperatorKind.LogicalOr:
                            return (bool) left || (bool) right;
                        case BoundBinaryOperatorKind.Equals:
                            return Equals(left, right);
                        case BoundBinaryOperatorKind.NotEquals:
                            return !Equals(left, right);
                        default:
                            throw new Exception($"Unexpected binary operator {binary.Op}");
                    }
                }
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }
    }
}
