using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal class BoundScope
    {
        public BoundScope Parent { get; }

        private Dictionary<string, Symbol> symbols;

        public BoundScope(BoundScope parent)
        {
            Parent = parent;
        }

        public bool TryDeclareVariable(VariableSymbol variable)
        {
            return TryDeclareSymbol(variable);
        }

        public bool TryDeclareFunction(FunctionSymbol function)
        {
            return TryDeclareSymbol(function);
        }

        public IReadOnlyList<VariableSymbol> GetDeclaredVariables()
        {
            return GetDeclaredSymbols().OfType<VariableSymbol>().ToArray();
        }

        public IReadOnlyList<FunctionSymbol> GetDeclaredFunctions()
        {
            return GetDeclaredSymbols().OfType<FunctionSymbol>().ToArray();
        }

        public Symbol TryLookupSymbol(string name)
        {
            if (symbols != null && symbols.TryGetValue(name, out var symbol))
            {
                return symbol;
            }

            return Parent?.TryLookupSymbol(name);
        }

        private bool TryDeclareSymbol<TSymbol>(TSymbol symbol) where TSymbol : Symbol
        {
            symbols = symbols ?? new Dictionary<string, Symbol>();

            if (symbols.ContainsKey(symbol.Name))
            {
                return false;
            }

            symbols.Add(symbol.Name, symbol);
            return true;
        }

        private IEnumerable<Symbol> GetDeclaredSymbols()
        {
            return symbols?.Values ?? Enumerable.Empty<Symbol>();
        }
    }
}
