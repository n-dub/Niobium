using System;

namespace LanguageCore.CodeAnalysis.Binding
{
    public abstract class BoundExpression : BoundNode
    {
        public abstract Type Type { get; }
    }
}
