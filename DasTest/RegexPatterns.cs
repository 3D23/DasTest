using System.Text.RegularExpressions;

namespace DasTest
{
    public static partial class RegexPatterns
    {
        public static readonly Regex Email = EmailRegex();

        [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled, "ru-RU")]
        private static partial Regex EmailRegex();
    }
}
