﻿namespace LanguageCore.CodeAnalysis.Binding
{
    public abstract class BoundNode
    {
        public abstract BoundNodeKind Kind { get; }
    }
}
