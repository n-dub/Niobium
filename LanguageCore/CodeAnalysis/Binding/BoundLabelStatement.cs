namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundLabelStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;
        public BoundLabel Label { get; }

        public BoundLabelStatement(BoundLabel label)
        {
            Label = label;
        }
    }
}
