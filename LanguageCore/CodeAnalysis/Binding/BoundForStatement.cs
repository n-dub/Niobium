using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundForStatement : BoundLoopStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.ForStatement;
        public VariableSymbol Variable { get; }
        public BoundExpression LowerBound { get; }
        public BoundExpression UpperBound { get; }
        public BoundBlockStatement Body { get; }

        public BoundForStatement(VariableSymbol variable, BoundExpression lowerBound, BoundExpression upperBound,
            BoundBlockStatement body, BoundLabel breakLabel, BoundLabel continueLabel)
            : base(breakLabel, continueLabel)
        {
            Variable = variable;
            LowerBound = lowerBound;
            UpperBound = upperBound;
            Body = body;
        }
    }
}
