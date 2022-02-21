using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal class BoundScope
    {
        public BoundScope Parent { get; }

        private readonly Dictionary<string, VariableSymbol> variables = new Dictionary<string, VariableSymbol>();

        public BoundScope(BoundScope parent)
        {
            Parent = parent;
        }

        public bool TryDeclare(VariableSymbol variable)
        {
            if (variables.ContainsKey(variable.Name))
            {
                return false;
            }

            variables.Add(variable.Name, variable);
            return true;
        }

        public bool TryLookup(string name, out VariableSymbol variable)
        {
            if (variables.TryGetValue(name, out variable))
            {
                return true;
            }

            return Parent?.TryLookup(name, out variable) ?? false;
        }

        public IReadOnlyList<VariableSymbol> GetDeclaredVariables()
        {
            return variables.Values.ToArray();
        }
    }
}
