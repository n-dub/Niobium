using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundVariableExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;
        public VariableSymbol Variable { get; }
        public override TypeSymbol Type => Variable.Type;
        public override BoundConstant? ConstantValue => Variable.Constant;

        public BoundVariableExpression(VariableSymbol variable)
        {
            Variable = variable;
        }
    }

    internal static partial class BoundNodeFactory
    {
        public static BoundVariableExpression Variable(VariableSymbol variable)
        {
            return new BoundVariableExpression(variable);
        }

        public static BoundVariableExpression Variable(BoundVariableDeclarationStatement variable)
        {
            return new BoundVariableExpression(variable.Variable);
        }
    }
}
