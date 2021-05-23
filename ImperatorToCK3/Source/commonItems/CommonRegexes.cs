namespace commonItems
{
    public class CommonRegexes
    {
        // catchall:
        //		We grab everything that's NOT =, { or }, OR we grab everything within quotes, except newlines, which we already drop
        //		in the parser.
        public const string Catchall = @""".+""|[^={}]+";

        // numbers
        public const string Integer = @"-?\d+";
        public const string QuotedInteger = @"""-?\d+""";
        public const string Float = @"-?\d+(.\d+)?";
        public const string QuotedFloat = @"""-?\d+(.\d+)?""";

        // strings
        public const string StringRegex = @"[^[:s:]^=^\{^\}^\""]+";
        public const string QuotedString = @"""[^\n^=^\{^\}^\""]+""";
    }
}
