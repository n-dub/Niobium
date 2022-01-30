using System;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundVariableExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;
        public VariableSymbol Variable { get; }
        public override Type Type => Variable.Type;

        public BoundVariableExpression(VariableSymbol variable)
        {
            Variable = variable;
        }
    }
}
