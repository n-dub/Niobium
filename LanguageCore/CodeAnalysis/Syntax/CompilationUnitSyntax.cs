using System.Collections.Generic;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public sealed class CompilationUnitSyntax : SyntaxNode
    {
        public override SyntaxKind Kind => SyntaxKind.CompilationUnit;
        public IReadOnlyList<MemberSyntax> Members { get; }
        public SyntaxToken EndOfFileToken { get; }

        public CompilationUnitSyntax(IReadOnlyList<MemberSyntax> members, SyntaxToken endOfFileToken)
        {
            Members = members;
            EndOfFileToken = endOfFileToken;
        }
    }
}
