using System.Collections.Generic;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class CompilationUnitSyntax : SyntaxNode
    {
        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;
        public IReadOnlyList<MemberSyntax> Members { get; }
        public SyntaxToken EndOfFileToken { get; }

        public CompilationUnitSyntax(SyntaxTree syntaxTree, IReadOnlyList<MemberSyntax> members,
            SyntaxToken endOfFileToken)
            : base(syntaxTree)
        {
            Members = members;
            EndOfFileToken = endOfFileToken;
        }
    }
}
