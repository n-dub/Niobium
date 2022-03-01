namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class TypeClauseSyntax : SyntaxNode
    {
        public override SyntaxKind Kind => SyntaxKind.TypeClause;
        public SyntaxToken ColonOrArrowToken { get; }
        public SyntaxToken Identifier { get; }

        public TypeClauseSyntax(SyntaxTree syntaxTree, SyntaxToken colonOrArrowToken, SyntaxToken identifier)
            : base(syntaxTree)
        {
            ColonOrArrowToken = colonOrArrowToken;
            Identifier = identifier;
        }
    }
}
