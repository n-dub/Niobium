namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundIfStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.IfStatement;
        public BoundExpression Condition { get; }
        public BoundBlockStatement ThenStatement { get; }
        public BoundBlockStatement ElseStatement { get; }

        public BoundIfStatement(BoundExpression condition, BoundBlockStatement thenStatement,
            BoundBlockStatement elseStatement)
        {
            Condition = condition;
            ThenStatement = thenStatement;
            ElseStatement = elseStatement;
        }
    }
}
