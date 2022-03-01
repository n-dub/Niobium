﻿namespace LanguageCore.CodeAnalysis.Syntax
{
    internal sealed class UnaryExpressionSyntax : ExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.UnaryExpression;
        public SyntaxToken OperatorToken { get; }
        public ExpressionSyntax Operand { get; }

        public UnaryExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken operatorToken, ExpressionSyntax operand)
            : base(syntaxTree)
        {
            OperatorToken = operatorToken;
            Operand = operand;
        }
    }
}
