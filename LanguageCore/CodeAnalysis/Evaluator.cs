﻿using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis
{
    internal sealed class Evaluator
    {
        private readonly IReadOnlyDictionary<FunctionSymbol, BoundBlockStatement> functionBodies;
        private readonly BoundBlockStatement root;
        private readonly Dictionary<VariableSymbol, object> globals;
        private readonly Stack<Dictionary<VariableSymbol, object>> locals;
        private readonly Random random = new Random();
        private object lastValue;
        private TypeSymbol lastType;

        public Evaluator(IReadOnlyDictionary<FunctionSymbol, BoundBlockStatement> functionBodies,
            BoundBlockStatement root, Dictionary<VariableSymbol, object> variables)
        {
            this.root = root;
            this.functionBodies = functionBodies;
            globals = variables;
            locals = new Stack<Dictionary<VariableSymbol, object>>();
            locals.Push(new Dictionary<VariableSymbol, object>());
        }

        public object Evaluate(out TypeSymbol type)
        {
            return EvaluateStatement(root, out type);
        }

        private object EvaluateStatement(BoundBlockStatement body, out TypeSymbol type)
        {
            var labelToIndex = new Dictionary<BoundLabel, int>();

            for (var i = 0; i < body.Statements.Count; ++i)
            {
                if (body.Statements[i] is BoundLabelStatement l)
                {
                    labelToIndex.Add(l.Label, i + 1);
                }
            }

            var index = 0;
            while (index < body.Statements.Count)
            {
                var s = body.Statements[index];

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
            lastType = node.Initializer.Type;
            lastValue = value;
            Assign(node.Variable, value);
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
                case BoundNodeKind.CallExpression:
                    return EvaluateCallExpression((BoundCallExpression) node);
                case BoundNodeKind.ConversionExpression:
                    return EvaluateConversionExpression((BoundConversionExpression) node);
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
                case BoundBinaryOperatorKind.Addition when binary.Type == TypeSymbol.String:
                    return (string) left + (string) right;
                case BoundBinaryOperatorKind.Addition:
                    return (int) left + (int) right;
                case BoundBinaryOperatorKind.Subtraction:
                    return (int) left - (int) right;
                case BoundBinaryOperatorKind.Multiplication when binary.Type == TypeSymbol.String:
                    return string.Join("", Enumerable.Repeat((string) left, (int) right));
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

        private object EvaluateCallExpression(BoundCallExpression node)
        {
            if (node.Function == BuiltinFunctions.ReadLine)
            {
                return Console.ReadLine();
            }

            if (node.Function == BuiltinFunctions.Print)
            {
                var message = (string) EvaluateExpression(node.Arguments[0]);
                Console.WriteLine(message);
                return null;
            }

            if (node.Function == BuiltinFunctions.Random)
            {
                var minValue = (int) EvaluateExpression(node.Arguments[0]);
                var maxValue = (int) EvaluateExpression(node.Arguments[1]);
                return random.Next(minValue, maxValue);
            }

            var newLocals = new Dictionary<VariableSymbol, object>();
            for (var i = 0; i < node.Arguments.Count; i++)
            {
                var parameter = node.Function.Parameters[i];
                var value = EvaluateExpression(node.Arguments[i]);
                newLocals.Add(parameter, value);
            }

            locals.Push(newLocals);

            var statement = functionBodies[node.Function];
            var result = EvaluateStatement(statement, out _);

            locals.Pop();

            return result;
        }

        private object EvaluateAssignmentExpression(BoundAssignmentExpression assignment)
        {
            var value = EvaluateExpression(assignment.Expression);
            Assign(assignment.Variable, value);
            return value;
        }

        private object EvaluateVariableExpression(BoundVariableExpression variable)
        {
            if (variable.Variable.Kind == SymbolKind.GlobalVariable)
            {
                return globals[variable.Variable];
            }

            var localFrame = locals.Peek();
            return localFrame[variable.Variable];
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

        private object EvaluateConversionExpression(BoundConversionExpression node)
        {
            var value = EvaluateExpression(node.Expression);
            return Convert.ChangeType(value, node.Type.ToSystemType());
        }
        
        private void Assign(VariableSymbol variable, object value)
        {
            if (variable.Kind == SymbolKind.GlobalVariable)
            {
                globals[variable] = value;
            }
            else
            {
                var localFrame = locals.Peek();
                localFrame[variable] = value;
            }
        }
    }
}
