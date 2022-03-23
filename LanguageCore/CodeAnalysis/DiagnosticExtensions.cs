using System.Collections.Generic;
using System.Linq;

namespace LanguageCore.CodeAnalysis
{
    public static class DiagnosticExtensions
    {
        public static bool HasErrors(this IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.Any(d => d.Kind == DiagnosticKind.Error);
        }
    }
}
