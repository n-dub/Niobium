using System;
using System.Linq;

namespace Utilities
{
    public static class UnitTestDetector
    {
        public static bool IsRunningFromNUnit { get; }

        static UnitTestDetector()
        {
            IsRunningFromNUnit = AppDomain.CurrentDomain.GetAssemblies()
                .Select(x => x.FullName.ToLowerInvariant().StartsWith("nunit.framework"))
                .Any();
        }
    }
}
