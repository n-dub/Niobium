using System;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundAssignmentExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;
        public override Type Type => Expression.Type;
        public string Name { get; }
        public BoundExpression Expression { get; }

        public BoundAssignmentExpression(string name, BoundExpression expression)
        {
            Name = name;
            Expression = expression;
        }
    }
}
