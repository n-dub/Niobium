using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundCompoundAssignmentExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.CompoundAssignmentExpression;
        public override TypeSymbol Type => Expression.Type;
        public VariableSymbol Variable { get; }
        public BoundBinaryOperator Op { get; }
        public BoundExpression Expression { get; }

        public BoundCompoundAssignmentExpression(VariableSymbol variable, BoundBinaryOperator op,
            BoundExpression expression)
        {
            Variable = variable;
            Op = op;
            Expression = expression;
        }
    }
}
