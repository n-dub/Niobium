using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Text;

namespace LanguageCore.CodeAnalysis.Syntax
{
    internal sealed class Parser
    {
        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        private readonly SourceText sourceText;
        private readonly SyntaxToken[] tokens;
        private int position;

        private SyntaxToken Current => Peek(0);

        public Parser(SourceText text)
        {
            var tokenList = new List<SyntaxToken>();

            var lexer = new Lexer(text);
            SyntaxToken token;
            do
            {
                token = lexer.Lex();

                if (token.Kind != SyntaxKind.WhitespaceToken && token.Kind != SyntaxKind.BadToken)
                {
                    tokenList.Add(token);
                }
            } while (token.Kind != SyntaxKind.EndOfFileToken);

            sourceText = text;
            tokens = tokenList.ToArray();
            Diagnostics.AddRange(lexer.Diagnostics);
        }

        public SyntaxTree Parse()
        {
            var expresion = ParseExpression();
            var endOfFileToken = MatchToken(SyntaxKind.EndOfFileToken);
            return new SyntaxTree(sourceText, Diagnostics.ToArray(), expresion, endOfFileToken);
        }

        private SyntaxToken Peek(int offset)
        {
            var index = position + offset;
            return index >= tokens.Length
                ? tokens[tokens.Length - 1]
                : tokens[index];
        }

        private SyntaxToken NextToken()
        {
            var current = Current;
            position++;
            return current;
        }

        private SyntaxToken MatchToken(SyntaxKind kind)
        {
            if (Current.Kind == kind)
            {
                return NextToken();
            }

            Diagnostics.ReportUnexpectedToken(Current.Span, Current.Kind, kind);
            return new SyntaxToken(kind, Current.Position, null, null);
        }

        private ExpressionSyntax ParseExpression()
        {
            return ParseAssignmentExpression();
        }

        private ExpressionSyntax ParseAssignmentExpression()
        {
            if (Peek(0).Kind == SyntaxKind.IdentifierToken &&
                Peek(1).Kind == SyntaxKind.EqualsToken)
            {
                var identifierToken = NextToken();
                var operatorToken = NextToken();
                var right = ParseExpression();
                return new AssignmentExpressionSyntax(identifierToken, operatorToken, right);
            }

            return ParseBinaryExpression();
        }

        private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            var unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
            if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
            {
                var operatorToken = NextToken();
                var operand = ParseBinaryExpression(unaryOperatorPrecedence);
                left = new UnaryExpressionSyntax(operatorToken, operand);
            }
            else
            {
                left = ParsePrimaryExpression();
            }

            while (true)
            {
                var precedence = Current.Kind.GetBinaryOperatorPrecedence();
                if (precedence == 0 || precedence <= parentPrecedence)
                {
                    break;
                }

                var operatorToken = NextToken();
                var right = ParseBinaryExpression(precedence);
                left = new BinaryExpressionSyntax(left, operatorToken, right);
            }

            return left;
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            switch (Current.Kind)
            {
                case SyntaxKind.OpenParenthesisToken:
                    return ParseParenthesizedExpression();
                case SyntaxKind.FalseKeyword:
                case SyntaxKind.TrueKeyword:
                    return ParseBooleanLiteral();
                case SyntaxKind.NumberToken:
                    return ParseNumberLiteral();
                default:
                    return ParseNameExpression();
            }
        }

        private ExpressionSyntax ParseParenthesizedExpression()
        {
            var left = MatchToken(SyntaxKind.OpenParenthesisToken);
            var expression = ParseExpression();
            var right = MatchToken(SyntaxKind.CloseParenthesisToken);
            return new ParenthesizedExpressionSyntax(left, expression, right);
        }

        private ExpressionSyntax ParseBooleanLiteral()
        {
            var isTrue = Current.Kind == SyntaxKind.TrueKeyword;
            var keywordToken = isTrue ? MatchToken(SyntaxKind.TrueKeyword) : MatchToken(SyntaxKind.FalseKeyword);
            return new LiteralExpressionSyntax(keywordToken, isTrue);
        }

        private ExpressionSyntax ParseNumberLiteral()
        {
            var numberToken = MatchToken(SyntaxKind.NumberToken);
            return new LiteralExpressionSyntax(numberToken);
        }

        private ExpressionSyntax ParseNameExpression()
        {
            var identifierToken = MatchToken(SyntaxKind.IdentifierToken);
            return new NameExpressionSyntax(identifierToken);
        }
    }
}
