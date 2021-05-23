using System.IO;
using System.Numerics;
using System.Collections.Generic;

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

    public class SingleInt
    {
        public SingleInt(StreamReader sr)
        {
            var intString = Parser.RemQuotes(new SingleString(sr).String);
            if (!int.TryParse(intString, out int theInt))
            {
                Log.WriteLine(LogLevel.Warning, "Could not convert string " + intString + " to int!");
                return;
            }
            Int = theInt;
        }
        public int Int { get; }
    }

    public class SingleDouble
    {
        public SingleDouble(StreamReader sr)
        {
            var doubleString = Parser.RemQuotes(new SingleString(sr).String);
            if (!double.TryParse(doubleString, out double theDouble))
            {
                Log.WriteLine(LogLevel.Warning, "Could not convert string " + doubleString + " to double!");
                return;
            }
            Double = theDouble;
        }
        public double Double { get; }
    }

    public class StringList : Parser
    {
        public StringList(StreamReader sr)
        {
            RegisterKeyword(@"""""", (StreamReader sr) => {});
            RegisterRegex(CommonRegexes.StringRegex, (StreamReader sr, string theString) =>
            {
                Strings.Add(theString);
            });
            RegisterRegex(CommonRegexes.QuotedString, (StreamReader sr, string theString) =>
            {
                Strings.Add(RemQuotes(theString));
            });
            ParseStream(sr);
        }
        public List<string> Strings { get; } = new List<string>();
    }

    public class IntList : Parser
    {
        public IntList(StreamReader sr)
        {
            RegisterRegex(CommonRegexes.Integer, (StreamReader sr, string intString) =>
            {
                Ints.Add(int.Parse(intString));
            });
            RegisterRegex(CommonRegexes.QuotedInteger, (StreamReader sr, string intString) =>
            {
                intString = intString[1..^1];
                Ints.Add(int.Parse(intString));
            });
            ParseStream(sr);
        }
        public List<int> Ints { get; } = new List<int>();
    }

    public class DoubleList : Parser
    {
        public DoubleList(StreamReader sr)
        {
            RegisterRegex(CommonRegexes.Float, (StreamReader sr, string floatString) =>
            {
                Doubles.Add(double.Parse(floatString));
            });
            RegisterRegex(CommonRegexes.QuotedFloat, (StreamReader sr, string floatString) =>
            {
                floatString = floatString[1..^1];
                Doubles.Add(double.Parse(floatString));
            });
            ParseStream(sr);
        }
        public List<double> Doubles { get; } = new List<double>();
    }
}
