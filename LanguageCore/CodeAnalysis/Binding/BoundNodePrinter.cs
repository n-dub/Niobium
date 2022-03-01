using System;
using System.CodeDom.Compiler;
using System.IO;
using LanguageCore.CodeAnalysis.IO;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal static class BoundNodePrinter
    {
        public static void WriteTo(this BoundNode node, TextWriter writer)
        {
            if (writer is IndentedTextWriter iw)
            {
                WriteTo(node, iw);
            }
            else
            {
                WriteTo(node, new IndentedTextWriter(writer));
            }
        }

        public static void WriteTo(this BoundNode node, IndentedTextWriter writer)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.BlockStatement:
                    WriteBlockStatement((BoundBlockStatement) node, writer);
                    break;
                case BoundNodeKind.VariableDeclarationStatement:
                    WriteVariableDeclaration((BoundVariableDeclarationStatement) node, writer);
                    break;
                case BoundNodeKind.IfStatement:
                    WriteIfStatement((BoundIfStatement) node, writer);
                    break;
                case BoundNodeKind.WhileStatement:
                    WriteWhileStatement((BoundWhileStatement) node, writer);
                    break;
                case BoundNodeKind.RepeatWhileStatement:
                    WriteDoWhileStatement((BoundRepeatWhileStatement) node, writer);
                    break;
                case BoundNodeKind.ForStatement:
                    WriteForStatement((BoundForStatement) node, writer);
                    break;
                case BoundNodeKind.LabelStatement:
                    WriteLabelStatement((BoundLabelStatement) node, writer);
                    break;
                case BoundNodeKind.GotoStatement:
                    WriteGotoStatement((BoundGotoStatement) node, writer);
                    break;
                case BoundNodeKind.ConditionalGotoStatement:
                    WriteConditionalGotoStatement((BoundConditionalGotoStatement) node, writer);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    WriteExpressionStatement((BoundExpressionStatement) node, writer);
                    break;
                case BoundNodeKind.ErrorExpression:
                    WriteErrorExpression((BoundErrorExpression) node, writer);
                    break;
                case BoundNodeKind.LiteralExpression:
                    WriteLiteralExpression((BoundLiteralExpression) node, writer);
                    break;
                case BoundNodeKind.VariableExpression:
                    WriteVariableExpression((BoundVariableExpression) node, writer);
                    break;
                case BoundNodeKind.AssignmentExpression:
                    WriteAssignmentExpression((BoundAssignmentExpression) node, writer);
                    break;
                case BoundNodeKind.UnaryExpression:
                    WriteUnaryExpression((BoundUnaryExpression) node, writer);
                    break;
                case BoundNodeKind.BinaryExpression:
                    WriteBinaryExpression((BoundBinaryExpression) node, writer);
                    break;
                case BoundNodeKind.CallExpression:
                    WriteCallExpression((BoundCallExpression) node, writer);
                    break;
                case BoundNodeKind.ConversionExpression:
                    WriteConversionExpression((BoundConversionExpression) node, writer);
                    break;
                case BoundNodeKind.ReturnStatement:
                    WriteReturnStatement((BoundReturnStatement) node, writer);
                    break;
                default:
                    throw new Exception($"Unexpected node {node.Kind}");
            }
        }

        private static void WriteNestedStatement(this IndentedTextWriter writer, BoundStatement node)
        {
            var needsIndentation = !(node is BoundBlockStatement);

            if (needsIndentation)
            {
                writer.Indent++;
            }

            node.WriteTo(writer);

            if (needsIndentation)
            {
                writer.Indent--;
            }
        }

        private static void WriteNestedExpression(this TextWriter writer, int parentPrecedence,
            BoundExpression expression)
        {
            if (expression is BoundUnaryExpression unary)
            {
                writer.WriteNestedExpression(parentPrecedence, unary.Op.SyntaxKind.GetUnaryOperatorPrecedence(), unary);
            }
            else if (expression is BoundBinaryExpression binary)
            {
                writer.WriteNestedExpression(parentPrecedence, binary.Op.SyntaxKind.GetBinaryOperatorPrecedence(),
                    binary);
            }
            else
            {
                expression.WriteTo(writer);
            }
        }

        private static void WriteNestedExpression(this TextWriter writer, int parentPrecedence, int currentPrecedence,
            BoundExpression expression)
        {
            var needsParenthesis = parentPrecedence >= currentPrecedence;

            if (needsParenthesis)
            {
                writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);
            }

            expression.WriteTo(writer);

            if (needsParenthesis)
            {
                writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
            }
        }

        private static void WriteBlockStatement(BoundBlockStatement node, IndentedTextWriter writer)
        {
            writer.WritePunctuation(SyntaxKind.OpenBraceToken);
            writer.WriteLine();
            writer.Indent++;

            foreach (var s in node.Statements)
            {
                s.WriteTo(writer);
            }

            writer.Indent--;
            writer.WritePunctuation(SyntaxKind.CloseBraceToken);
            writer.WriteLine();
        }

        private static void WriteVariableDeclaration(BoundVariableDeclarationStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(node.Variable.IsImmutable ? SyntaxKind.LetKeyword : SyntaxKind.VarKeyword);
            writer.WriteSpace();
            writer.WriteIdentifier(node.Variable.Name);
            writer.WriteSpace();
            writer.WritePunctuation(SyntaxKind.EqualsToken);
            writer.WriteSpace();
            node.Initializer.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteIfStatement(BoundIfStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.IfKeyword);
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
            writer.WriteNestedStatement(node.ThenStatement);

            if (node.ElseStatement != null)
            {
                writer.WriteKeyword(SyntaxKind.ElseKeyword);
                writer.WriteLine();
                writer.WriteNestedStatement(node.ElseStatement);
            }
        }

        private static void WriteWhileStatement(BoundWhileStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.WhileKeyword);
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
            writer.WriteNestedStatement(node.Body);
        }

        private static void WriteDoWhileStatement(BoundRepeatWhileStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.RepeatKeyword);
            writer.WriteLine();
            writer.WriteNestedStatement(node.Body);
            writer.WriteKeyword(SyntaxKind.WhileKeyword);
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteForStatement(BoundForStatement node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.ForKeyword);
            writer.WriteSpace();
            writer.WriteIdentifier(node.Variable.Name);
            writer.WriteSpace();
            writer.WritePunctuation(SyntaxKind.EqualsToken);
            writer.WriteSpace();
            node.LowerBound.WriteTo(writer);
            writer.WriteSpace();
            writer.WriteKeyword(SyntaxKind.InKeyword);
            writer.WriteSpace();
            node.UpperBound.WriteTo(writer);
            writer.WriteLine();
            writer.WriteNestedStatement(node.Body);
        }

        private static void WriteLabelStatement(BoundLabelStatement node, IndentedTextWriter writer)
        {
            var unIndent = writer.Indent > 0;
            if (unIndent)
            {
                writer.Indent--;
            }

            writer.WritePunctuation(node.Label.Name);
            writer.WritePunctuation(SyntaxKind.ColonToken);
            writer.WriteLine();

            if (unIndent)
            {
                writer.Indent++;
            }
        }

        private static void WriteGotoStatement(BoundGotoStatement node, TextWriter writer)
        {
            writer.WriteKeyword("goto ");
            writer.WriteIdentifier(node.Label.Name);
            writer.WriteLine();
        }

        private static void WriteConditionalGotoStatement(BoundConditionalGotoStatement node, TextWriter writer)
        {
            writer.WriteKeyword("goto ");
            writer.WriteIdentifier(node.Label.Name);
            writer.WriteKeyword(node.JumpIfTrue ? " if " : " unless ");
            node.Condition.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteReturnStatement(BoundReturnStatement node, TextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.ReturnKeyword);
            if (node.Expression != null)
            {
                writer.WriteSpace();
                node.Expression.WriteTo(writer);
            }

            writer.WriteLine();
        }

        private static void WriteExpressionStatement(BoundExpressionStatement node, TextWriter writer)
        {
            node.Expression.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteErrorExpression(BoundErrorExpression node, TextWriter writer)
        {
            writer.WriteKeyword("?");
        }

        private static void WriteLiteralExpression(BoundLiteralExpression node, TextWriter writer)
        {
            var value = node.Value.ToString();

            if (node.Type == TypeSymbol.Bool)
            {
                var kind = (bool) node.Value ? SyntaxKind.TrueKeyword : SyntaxKind.FalseKeyword;
                writer.WriteKeyword(kind);
            }
            else if (node.Type == TypeSymbol.Int32)
            {
                writer.WriteNumber(value);
            }
            else if (node.Type == TypeSymbol.String)
            {
                value = "\"" + value.Replace("\"", "\\\"") + "\"";
                writer.WriteString(value);
            }
            else
            {
                throw new Exception($"Unexpected type {node.Type}");
            }
        }

        private static void WriteVariableExpression(BoundVariableExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Variable.Name);
        }

        private static void WriteAssignmentExpression(BoundAssignmentExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Variable.Name);
            writer.WriteSpace();
            writer.WritePunctuation(SyntaxKind.EqualsToken);
            writer.WriteSpace();
            node.Expression.WriteTo(writer);
        }

        private static void WriteUnaryExpression(BoundUnaryExpression node, IndentedTextWriter writer)
        {
            var precedence = node.Op.SyntaxKind.GetUnaryOperatorPrecedence();

            writer.WritePunctuation(node.Op.SyntaxKind);
            writer.WriteNestedExpression(precedence, node.Operand);
        }

        private static void WriteBinaryExpression(BoundBinaryExpression node, IndentedTextWriter writer)
        {
            var precedence = node.Op.SyntaxKind.GetBinaryOperatorPrecedence();

            writer.WriteNestedExpression(precedence, node.Left);
            writer.WriteSpace();
            writer.WritePunctuation(node.Op.SyntaxKind);
            writer.WriteSpace();
            writer.WriteNestedExpression(precedence, node.Right);
        }

        private static void WriteCallExpression(BoundCallExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Function.Name);
            writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);

            var isFirst = true;
            foreach (var argument in node.Arguments)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    writer.WritePunctuation(SyntaxKind.CommaToken);
                    writer.WriteSpace();
                }

                argument.WriteTo(writer);
            }

            writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
        }

        private static void WriteConversionExpression(BoundConversionExpression node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Type.Name);
            writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);
            node.Expression.WriteTo(writer);
            writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);
        }
    }
}
