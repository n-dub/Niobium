using System;

namespace LanguageCore.CodeAnalysis
{
    public sealed class VariableSymbol
    {
        public string Name { get; }
        public bool IsImmutable { get; }
        public Type Type { get; }

        internal VariableSymbol(string name, bool isImmutable, Type type)
        {
            Name = name;
            IsImmutable = isImmutable;
            Type = type;
        }
    }
}
