using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal class BoundScope
    {
        public BoundScope Parent { get; }

        private Dictionary<string, VariableSymbol> variables;
        private Dictionary<string, FunctionSymbol> functions;

        public BoundScope(BoundScope parent)
        {
            Parent = parent;
        }

        public bool TryDeclareVariable(VariableSymbol variable)
        {
            variables = variables ?? new Dictionary<string, VariableSymbol>();

            if (variables.ContainsKey(variable.Name))
            {
                return false;
            }

            variables.Add(variable.Name, variable);
            return true;
        }

        public bool TryLookupVariable(string name, out VariableSymbol variable)
        {
            variable = null;

            if (variables != null && variables.TryGetValue(name, out variable))
            {
                return true;
            }

            return Parent?.TryLookupVariable(name, out variable) ?? false;
        }
        
        public bool TryDeclareFunction(FunctionSymbol function)
        {
            functions = functions ?? new Dictionary<string, FunctionSymbol>();

            if (functions.ContainsKey(function.Name))
            {
                return false;
            }

            functions.Add(function.Name, function);
            return true;
        }

        public bool TryLookupFunction(string name, out FunctionSymbol function)
        {
            function = null;

            if (functions != null && functions.TryGetValue(name, out function))
                return true;

            return Parent?.TryLookupFunction(name, out function) ?? false;
        }

        public IReadOnlyList<VariableSymbol> GetDeclaredVariables()
        {
            return variables?.Values.ToArray() ?? Array.Empty<VariableSymbol>();
        }
        
        public IReadOnlyList<FunctionSymbol> GetDeclaredFunctions()
        {
            return functions?.Values.ToArray() ?? Array.Empty<FunctionSymbol>();
        }
    }
}
