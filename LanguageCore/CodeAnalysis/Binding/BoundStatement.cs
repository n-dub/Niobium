namespace LanguageCore.CodeAnalysis.Binding
{
    internal abstract class BoundStatement : BoundNode
    {
    }

    internal sealed class BoundNopStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.NopStatement;
    }
}
