namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundNopStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.NopStatement;
    }

    internal static partial class BoundNodeFactory
    {
        public static BoundNopStatement Nop()
        {
            return new BoundNopStatement();
        }
    }
}
