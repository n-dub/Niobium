using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundVariableExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;
        public VariableSymbol Variable { get; }
        public override TypeSymbol Type => Variable.Type;
        public override BoundConstant ConstantValue => Variable.Constant;

        public BoundVariableExpression(VariableSymbol variable)
        {
            Variable = variable;
        }
    }
}
