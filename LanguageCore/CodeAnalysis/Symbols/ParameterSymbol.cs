﻿namespace LanguageCore.CodeAnalysis.Symbols
{
    public sealed class ParameterSymbol : LocalVariableSymbol
    {
        public int Ordinal { get; }
        public override SymbolKind Kind => SymbolKind.Parameter;

        internal ParameterSymbol(string name, TypeSymbol type, int ordinal)
            : base(name, true, type, null)
        {
            Ordinal = ordinal;
        }
    }
}
