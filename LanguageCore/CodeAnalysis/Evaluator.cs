﻿using System;
using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Binding;

namespace LanguageCore.CodeAnalysis
{
    internal sealed class Evaluator
    {
        private readonly BoundStatement root;
        private readonly Dictionary<VariableSymbol, object> variables;
        private object lastValue;

        public Evaluator(BoundStatement root, Dictionary<VariableSymbol, object> variables)
        {
            this.root = root;
            this.variables = variables;
        }

        public object Evaluate()
        {
            EvaluateStatement(root);
            return lastValue;
        }

        private void EvaluateStatement(BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    EvaluateBlockStatement((BoundBlockStatement) node);
                    break;
                case BoundNodeKind.VariableDeclarationStatement:
                    EvaluateVariableDeclaration((BoundVariableDeclarationStatement) node);
                    break;
                case BoundNodeKind.IfStatement:
                    EvaluateIfStatement((BoundIfStatement) node);
                    break;
                case BoundNodeKind.WhileStatement:
                    EvaluateWhileStatement((BoundWhileStatement) node);
                    break;
                case BoundNodeKind.ForStatement:
                    EvaluateForStatement((BoundForStatement) node);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    EvaluateExpressionStatement((BoundExpressionStatement) node);
                    break;
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        private void EvaluateVariableDeclaration(BoundVariableDeclarationStatement node)
        {
            var value = EvaluateExpression(node.Initializer);
            variables[node.Variable] = value;
            lastValue = value;
        }

        private void EvaluateBlockStatement(BoundBlockStatement node)
        {
            foreach (var statement in node.Statements)
            {
                EvaluateStatement(statement);
            }
        }

        private void EvaluateIfStatement(BoundIfStatement node)
        {
            var condition = (bool) EvaluateExpression(node.Condition);
            if (condition)
            {
                EvaluateBlockStatement(node.ThenStatement);
            }
            else if (node.ElseStatement != null)
            {
                EvaluateBlockStatement(node.ElseStatement);
            }
        }

        private void EvaluateWhileStatement(BoundWhileStatement node)
        {
            while ((bool) EvaluateExpression(node.Condition))
            {
                EvaluateStatement(node.Body);
            }
        }

        private void EvaluateForStatement(BoundForStatement node)
        {
            var lowerBound = (int) EvaluateExpression(node.LowerBound);
            var upperBound = (int) EvaluateExpression(node.UpperBound);

            for (var i = lowerBound; i < upperBound; ++i)
            {
                variables[node.Variable] = i;
                EvaluateBlockStatement(node.Body);
            }
        }

        private void EvaluateExpressionStatement(BoundExpressionStatement node)
        {
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
                case BoundBinaryOperatorKind.BitwiseAnd when binary.Type == typeof(int):
                    return (int) left & (int) right;
                case BoundBinaryOperatorKind.BitwiseAnd:
                    return (bool) left & (bool) right;
                case BoundBinaryOperatorKind.BitwiseOr when binary.Type == typeof(int):
                    return (int) left | (int) right;
                case BoundBinaryOperatorKind.BitwiseOr:
                    return (bool) left | (bool) right;
                case BoundBinaryOperatorKind.BitwiseXor when binary.Type == typeof(int):
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
