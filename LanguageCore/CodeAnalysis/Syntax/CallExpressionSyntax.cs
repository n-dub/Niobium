﻿namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class CallExpressionSyntax : ExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.CallExpression;
        public SyntaxToken Identifier { get; }
        public SyntaxToken OpenParenthesisToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken CloseParenthesisToken { get; }

        public CallExpressionSyntax(SyntaxToken identifier, SyntaxToken openParenthesisToken,
            SeparatedSyntaxList<ExpressionSyntax> arguments, SyntaxToken closeParenthesisToken)
        {
            Identifier = identifier;
            OpenParenthesisToken = openParenthesisToken;
            Arguments = arguments;
            CloseParenthesisToken = closeParenthesisToken;
        }
    }
}