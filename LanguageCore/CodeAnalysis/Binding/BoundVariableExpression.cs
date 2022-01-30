using System;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundVariableExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;
        public string Name { get; }
        public override Type Type { get; }

        public BoundVariableExpression(string name, Type type)
        {
            Name = name;
            Type = type;
        }
    }
}
