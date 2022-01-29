using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Utilities
{
    /// <summary>
    ///     The source of data about the programming language - Name, Copyright, Version, etc.
    /// </summary>
    public class LanguageInfo
    {
        public static readonly string Name;
        public static readonly string Description;
        public static readonly string Copyright;
        public static readonly string Version;

        static LanguageInfo()
        {
            var assembly = Assembly.GetEntryAssembly();
            Name = GetAssemblyAttribute<AssemblyTitleAttribute>(assembly)?.Title;
            Description = GetAssemblyAttribute<AssemblyDescriptionAttribute>(assembly)?.Description;
            Copyright = GetAssemblyAttribute<AssemblyCopyrightAttribute>(assembly)?.Copyright;
            Version = GetAssemblyAttribute<AssemblyVersionAttribute>(assembly)?.Version;
        }

        private static T GetAssemblyAttribute<T>(ICustomAttributeProvider assembly)
            where T : Attribute
        {
            var attributes = assembly.GetCustomAttributes(typeof(T), true);
            return (T) attributes.FirstOrDefault();
        }
    }
}
