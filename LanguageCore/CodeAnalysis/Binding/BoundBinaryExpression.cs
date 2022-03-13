﻿using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundBinaryExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
        public override TypeSymbol Type => Op.Type;
        public BoundExpression Left { get; }
        public BoundBinaryOperator Op { get; }
        public BoundExpression Right { get; }
        public override BoundConstant ConstantValue { get; }

        public BoundBinaryExpression(BoundExpression left, BoundBinaryOperator op, BoundExpression right)
        {
            Left = left;
            Op = op;
            Right = right;
            ConstantValue = ConstantFolding.ComputeConstant(left, op, right);
        }
    }
}
