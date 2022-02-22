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

        public bool TryLookupVariable(string name, out VariableSymbol variable)
        {
            return TryLookupSymbol(name, out variable);
        }

        public bool TryDeclareFunction(FunctionSymbol function)
        {
            return TryDeclareSymbol(function);
        }

        public bool TryLookupFunction(string name, out FunctionSymbol function)
        {
            return TryLookupSymbol(name, out function);
        }

        public IReadOnlyList<VariableSymbol> GetDeclaredVariables()
        {
            return GetDeclaredSymbols().OfType<VariableSymbol>().ToArray();
        }

        public IReadOnlyList<FunctionSymbol> GetDeclaredFunctions()
        {
            return GetDeclaredSymbols().OfType<FunctionSymbol>().ToArray();
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

        private bool TryLookupSymbol<TSymbol>(string name, out TSymbol symbol) where TSymbol : Symbol
        {
            symbol = null;

            if (symbols != null && symbols.TryGetValue(name, out var declaredSymbol))
            {
                if (declaredSymbol is TSymbol matchingSymbol)
                {
                    symbol = matchingSymbol;
                    return true;
                }

                return false;
            }

            return Parent?.TryLookupSymbol(name, out symbol) ?? false;
        }

        private IEnumerable<Symbol> GetDeclaredSymbols()
        {
            return symbols?.Values ?? Enumerable.Empty<Symbol>();
        }
    }
}
