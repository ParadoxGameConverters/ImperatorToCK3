using System.IO;

namespace commonItems
{
    public class ParserHelpers
    {
        public static void IgnoreItem(StreamReader sr)
        {
            var next = Parser.GetNextLexeme(sr);
            if (next == "=")
            {
                next = Parser.GetNextLexeme(sr);
            }
            if (next == "rgb" || next == "hsv") // Needed for ignoring color. Example: "color = rgb { 2 4 8 }"
            {
                if ((char)sr.Peek() == '{')
                {
                    next = Parser.GetNextLexeme(sr);
                }
                else // don't go further in cases like "type = rgb"
                {
                    return;
                }
            }
            if (next == "{")
            {
                var braceDepth = 1;
                while (true)
                {
                    if (sr.EndOfStream)
                    {
                        return;
                    }
                    var token = Parser.GetNextLexeme(sr);
                    if (token == "{")
                    {
                        ++braceDepth;
                    }
                    else if (token == "}")
                    {
                        --braceDepth;
                        if (braceDepth == 0)
                        {
                            return;
                        }
                    }
                }
            }
        }

        public static void IgnoreAndLogItem(StreamReader sr, string keyword)
        {
            Log.WriteLine(LogLevel.Debug, "Ignoring keyword: " + keyword);
            IgnoreItem(sr);
        }
    }
    public class SingleString : Parser
    {
        public SingleString(StreamReader sr)
        {
            GetNextTokenWithoutMatching(sr); // remove equals
            var token = GetNextTokenWithoutMatching(sr);
            if (token == null)
            {
                Log.WriteLine(LogLevel.Error, "SingleString: next token not found!"); ;
            }
            else
            {
                String = RemQuotes(token);
            }
        }

        public string String { get; } = "";
    }

    public class SingleInt : Parser
    {
        public SingleInt(StreamReader sr)
        {
            GetNextTokenWithoutMatching(sr); // remove equals
            var token = GetNextTokenWithoutMatching(sr);
            if (token == null)
            {
                Log.WriteLine(LogLevel.Error, "SingleInt: next token not found!");
            }
            else
            {
                Int = int.Parse(RemQuotes(token));
            }
        }

        public int Int { get; }
    }
}
