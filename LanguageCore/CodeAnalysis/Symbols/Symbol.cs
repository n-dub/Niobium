using System.IO;

namespace LanguageCore.CodeAnalysis.Symbols
{
    public abstract class Symbol
    {
        public abstract SymbolKind Kind { get; }
        public string Name { get; }

        private protected Symbol(string name)
        {
            Name = name;
        }
        
        public void WriteTo(TextWriter writer)
        {
            SymbolPrinter.WriteTo(this, writer);
        }

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                WriteTo(writer);
                return writer.ToString();
            }
        }
    }
}
