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

    internal static partial class BoundNodeFactory
    {
        public static BoundLabelStatement Label(BoundLabel label)
        {
            return new BoundLabelStatement(label);
        }
    }
}
