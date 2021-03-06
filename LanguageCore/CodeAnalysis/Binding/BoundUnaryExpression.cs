using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override TypeSymbol Type => Op.Type;
        public BoundUnaryOperator Op { get; }
        public BoundExpression Operand { get; }
        public override BoundConstant? ConstantValue { get; }

        public BoundUnaryExpression(BoundUnaryOperator op, BoundExpression operand)
        {
            Op = op;
            Operand = operand;
            ConstantValue = ConstantFolding.Fold(op, operand);
        }
    }
}
