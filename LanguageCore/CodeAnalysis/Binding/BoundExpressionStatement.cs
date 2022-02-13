﻿namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundExpressionStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;
        public BoundExpression Expression { get; }

        public BoundExpressionStatement(BoundExpression expression)
        {
            Expression = expression;
        }
    }
}
