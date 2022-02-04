using LanguageCore.CodeAnalysis.Text;
using NUnit.Framework;

namespace LanguageCore.Tests.CodeAnalysis.Text
{
    public class SourceTextTests
    {
        [TestCase(".", 1)]
        [TestCase(".\r\n", 2)]
        [TestCase(".\r\n\r\n", 3)]
        public void SourceText_IncludesLastLine(string text, int expectedLineCount)
        {
            var sourceText = SourceText.From(text);
            Assert.AreEqual(expectedLineCount, sourceText.Lines.Count);
        }
    }
}
