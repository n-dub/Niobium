namespace LanguageCore.CodeAnalysis
{
    internal sealed class LabelSymbol
    {
        public string Name { get; }

        internal LabelSymbol(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
