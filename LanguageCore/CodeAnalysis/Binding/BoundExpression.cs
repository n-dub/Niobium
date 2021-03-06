using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    public abstract class BoundExpression : BoundNode
    {
        public abstract TypeSymbol Type { get; }
        public virtual BoundConstant? ConstantValue => null;
    }
}
