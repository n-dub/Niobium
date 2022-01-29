using System;
using System.Linq;
using System.Reflection;

namespace Utilities
{
    /// <summary>
    ///     The source of data about the programming language - Name, Copyright, Version, etc.
    /// </summary>
    public static class LanguageInfo
    {
        public static readonly string Name;
        public static readonly string Description;
        public static readonly string Copyright;
        public static readonly string ShortVersion;
        public static readonly string FullVersion;

        static LanguageInfo()
        {
            var assembly = Assembly.GetEntryAssembly();
            Name = GetAssemblyAttribute<AssemblyTitleAttribute>(assembly)?.Title;
            Description = GetAssemblyAttribute<AssemblyDescriptionAttribute>(assembly)?.Description;
            Copyright = GetAssemblyAttribute<AssemblyCopyrightAttribute>(assembly)?.Copyright;
            FullVersion = GetAssemblyAttribute<AssemblyFileVersionAttribute>(assembly)?.Version;
            ShortVersion = FullVersion?.Substring(0, FullVersion.LastIndexOf('.'));
        }

        private static T GetAssemblyAttribute<T>(ICustomAttributeProvider assembly)
            where T : Attribute
        {
            var attributes = assembly.GetCustomAttributes(typeof(T), true);
            return (T) attributes.FirstOrDefault();
        }
    }
}
