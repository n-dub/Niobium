﻿using System.Collections.Generic;
using System.Linq;

namespace LanguageCore.CodeAnalysis
{
    public sealed class EvaluationResult
    {
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public object Value { get; }
        public string Name { get; }

        public EvaluationResult(IEnumerable<Diagnostic> diagnostics, object value, string name)
        {
            Diagnostics = diagnostics.ToArray();
            Value = value;
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name}: {Value.GetType().Name} = {Value}";
        }
    }
}
