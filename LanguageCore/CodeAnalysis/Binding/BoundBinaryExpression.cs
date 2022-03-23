using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundBinaryExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
        public override TypeSymbol Type => Op.Type;
        public BoundExpression Left { get; }
        public BoundBinaryOperator Op { get; }
        public BoundExpression Right { get; }
        public override BoundConstant? ConstantValue { get; }

        public BoundBinaryExpression(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            Left = left;
            Op = op;
            Right = right;
            ConstantValue = ConstantFolding.Fold(left, op, right);
        }
    }

    internal static partial class BoundNodeFactory
    {
        public static BoundBinaryExpression Binary(BoundExpression left, SyntaxKind kind, BoundExpression right)
        {
            var op = BoundBinaryOperator.Bind(kind, left.Type, right.Type)!;
            return Binary(left, op, right);
        }

        public static BoundBinaryExpression Binary(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            return new BoundBinaryExpression(left, op, right);
        }

        public static BoundBinaryExpression Add(BoundExpression left, BoundExpression right)
        {
            return Binary(left, SyntaxKind.PlusToken, right);
        }

        public static BoundBinaryExpression Less(BoundExpression left, BoundExpression right)
        {
            return Binary(left, SyntaxKind.LessToken, right);
        }

        public static BoundExpressionStatement Increment(BoundVariableExpression variable)
        {
            var increment = Add(variable, Literal(1));
            var incrementAssign = new BoundAssignmentExpression(variable.Variable, increment);
            return new BoundExpressionStatement(incrementAssign);
        }
    }
}
