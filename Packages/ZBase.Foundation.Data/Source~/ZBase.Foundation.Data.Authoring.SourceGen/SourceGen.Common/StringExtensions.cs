namespace ZBase.Foundation.SourceGen
{
    public static class StringExtensions
    {
        public static string ToValidIdentifier(this string value)
            => value
                .Replace('.', '_')
                .Replace("-", "__")
                .Replace('<', 'ᐸ')
                .Replace('>', 'ᐳ')
                .Replace("[]", "Array")
                ;

        public static string ToValidNamespace(this string value)
            => value
                .Replace("-", "_")
                .Replace('<', 'ᐸ')
                .Replace('>', 'ᐳ')
                .Replace("[]", "Array")
                ;
    }
}
