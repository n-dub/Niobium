namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class TypeClauseSyntax : SyntaxNode
    {
        public override SyntaxKind Kind => SyntaxKind.TypeClause;
        public SyntaxToken ColonOrArrowToken { get; }
        public SyntaxToken Identifier { get; }

        public TypeClauseSyntax(SyntaxToken colonOrArrowToken, SyntaxToken identifier)
        {
            ColonOrArrowToken = colonOrArrowToken;
            Identifier = identifier;
        }
    }
}
