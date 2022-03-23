using LanguageCore.CodeAnalysis.Text;

namespace LanguageCore.CodeAnalysis
{
    public sealed class Diagnostic
    {
        public TextLocation Location { get; }
        public string Message { get; }
        public DiagnosticKind Kind { get; }
        public bool Expired { get; }

        private Diagnostic(DiagnosticKind kind, TextLocation location, string message, bool expired = false)
        {
            Kind = kind;
            Location = location;
            Message = message;
            Expired = expired;
        }

        public Diagnostic Expire()
        {
            return new Diagnostic(Kind, Location, Message, true);
        }

        public override string ToString()
        {
            return $"{Kind.ToString().ToLowerInvariant()}: {Message}";
        }

        public static Diagnostic Error(TextLocation location, string message)
        {
            return new Diagnostic(DiagnosticKind.Error, location, message);
        }

        public static Diagnostic Warning(TextLocation location, string message)
        {
            return new Diagnostic(DiagnosticKind.Warning, location, message);
        }
    }
}
