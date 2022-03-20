using LanguageCore.CodeAnalysis.Text;

namespace Repl.Authoring
{
    public enum Classification
    {
        Text,
        Keyword,
        LiteralKeyword,
        Identifier,
        Number,
        String,
        Comment
    }

    public sealed class ClassifiedSpan
    {
        public TextSpan Span { get; }
        public Classification Classification { get; }

        public ClassifiedSpan(TextSpan span, Classification classification)
        {
            Span = span;
            Classification = classification;
        }
    }
}
