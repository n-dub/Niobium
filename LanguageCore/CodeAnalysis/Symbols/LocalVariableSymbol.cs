﻿using LanguageCore.CodeAnalysis.Binding;

namespace LanguageCore.CodeAnalysis.Symbols
{
    public class LocalVariableSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.LocalVariable;

        internal LocalVariableSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant constant)
            : base(name, isReadOnly, type, constant)
        {
        }
    }
}
