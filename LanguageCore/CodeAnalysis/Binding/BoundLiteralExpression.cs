using System;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundLiteralExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
        public override TypeSymbol Type { get; }
        public object Value => ConstantValue.Value;
        public override BoundConstant ConstantValue { get; }

        public BoundLiteralExpression(object value)
        {
            switch (value)
            {
                case bool _:
                    Type = TypeSymbol.Bool;
                    break;
                case int _:
                    Type = TypeSymbol.Int32;
                    break;
                case string _:
                    Type = TypeSymbol.String;
                    break;
                default:
                    throw new Exception($"Unexpected literal '{value}' of type {value.GetType()}");
            }

            ConstantValue = new BoundConstant(value);
        }
    }
}
