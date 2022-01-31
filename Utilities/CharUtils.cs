namespace Utilities
{
    public static class CharUtils
    {
        /// <summary>
        ///     Indicates whether the specified character is a letter, a digit or an underscore '_'.
        /// </summary>
        /// <param name="c">The character to evaluate.</param>
        public static bool IsValidIdentifier(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }
    }
}
