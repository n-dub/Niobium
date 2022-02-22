using System.Collections.Generic;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundCallExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => Function.Type;
        public FunctionSymbol Function { get; }
        public IReadOnlyList<BoundExpression> Arguments { get; }

        public BoundCallExpression(FunctionSymbol function, IReadOnlyList<BoundExpression> arguments)
        {
            Function = function;
            Arguments = arguments;
        }
    }
}
