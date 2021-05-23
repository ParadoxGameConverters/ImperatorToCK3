using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace commonItems
{
    public delegate void Del(StreamReader sr, string keyword);
    public delegate void SimpleDel(StreamReader sr);

    abstract class AbstractDelegate
    {
        public abstract void Execute(StreamReader sr, string token);
    }
    class TwoArgDelegate : AbstractDelegate
    {
        readonly Del del;
        public TwoArgDelegate(Del del_) { del = del_; }
        public override void Execute(StreamReader sr, string token)
        {
            del(sr, token);
        }
    }
    class OneArgDelegate : AbstractDelegate
    {
        readonly SimpleDel del;
        public OneArgDelegate(SimpleDel del_) { del = del_; }
        public override void Execute(StreamReader sr, string token)
        {
            del(sr);
        }
    }



    public class Parser
    {
        private abstract class RegisteredKeywordOrRegex
        {
            public abstract bool Match(string token);
        }
        private class RegisteredKeyword : RegisteredKeywordOrRegex
        {
            readonly string keyword;
            public RegisteredKeyword(string keyword_)
            {
                keyword = keyword_;
            }
            public override bool Match(string token) { return keyword == token; }
        }
        private class RegisteredRegex : RegisteredKeywordOrRegex
        {
            readonly Regex regex;
            public RegisteredRegex(string regex_) { regex = new Regex(regex_); }
            public override bool Match(string token)
            {
                var match = regex.Match(token);
                return match.Success && match.Length == token.Length;
            }
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        public static string RemQuotes(string str)
        {
            int length = str.Length;
            if (length < 2)
            {
                return str;
            }
            if (!str.StartsWith('"') || !str.EndsWith('"'))
            {
                return str;
            }
            return str.Substring(1, length - 2);
        }
        public static void AbsorbBOM(StreamReader stream)
        {
            var firstChar = stream.Peek();
            if (firstChar == '\xEF')
            {
                stream.Read(new char[3]); // skip 3 bytes
            }
        }


        public void RegisterKeyword(string keyword, Del del)
        {
            registeredDict.Add(new RegisteredKeyword(keyword), new TwoArgDelegate(del));
        }
        public void RegisterKeyword(string keyword, SimpleDel del)
        {
            registeredDict.Add(new RegisteredKeyword(keyword), new OneArgDelegate(del));
        }
        public void RegisterRegex(string keyword, Del del)
        {
            registeredDict.Add(new RegisteredRegex(keyword), new TwoArgDelegate(del));
        }
        public void RegisterRegex(string keyword, SimpleDel del)
        {
            registeredDict.Add(new RegisteredRegex(keyword), new OneArgDelegate(del));
        }

        public void ClearRegisteredDict()
        {
            registeredDict.Clear();
        }

        bool TryToMatch(string token, string strippedToken, bool isTokenQuoted, StreamReader stream)
        {
            foreach (var (regex, fun) in registeredDict)
            {
                if (regex.Match(token))
                {
                    fun.Execute(stream, token);
                    return true;
                }
            }
            if (isTokenQuoted)
            {
                foreach (var (regex, fun) in registeredDict)
                {
                    if (regex.Match(strippedToken))
                    {
                        fun.Execute(stream, token);
                        return true;
                    }
                }
            }
            return false;
        }

        public static string GetNextLexeme(StreamReader stream)
        {
            var sb = new StringBuilder();

            var inQuotes = false;
            var inLiteralQuote = false;
            char previousCharacter = '\0';

            while (true)
            {

                if (stream.EndOfStream)
                {
                    break;
                }

                char inputChar = (char)stream.Read();

                if (!inQuotes && inputChar == '#')
                {
                    stream.ReadLine();
                    if (sb.Length != 0)
                    {
                        break;
                    }
                }
                else if (inputChar == '\n')
                {
                    if (!inQuotes)
                    {
                        if (sb.Length != 0)
                        {
                            break;
                        }
                    }
                    else // fix Paradox' mistake and don't break proper names in half
                    {
                        sb.Append(' ');
                    }
                }
                else if (inputChar == '\"' && !inQuotes && sb.Length == 0)
                {
                    inQuotes = true;
                    sb.Append(inputChar);
                }
                else if (inputChar == '\"' && !inQuotes && sb.Length == 1 && sb.ToString().Last() == 'R')
                {
                    inLiteralQuote = true;
                    --sb.Length;
                    sb.Append(inputChar);
                }
                else if (inputChar == '(' && inLiteralQuote && sb.Length == 1)
                {
                    continue;
                }
                else if (inputChar == '\"' && inLiteralQuote && previousCharacter == ')')
                {
                    --sb.Length;
                    sb.Append(inputChar);
                    break;
                }
                else if (inputChar == '\"' && inQuotes && previousCharacter != '\\')
                {
                    sb.Append(inputChar);
                    break;
                }
                else if (!inQuotes && !inLiteralQuote && char.IsWhiteSpace(inputChar))
                {
                    if (sb.Length != 0)
                    {
                        break;
                    }
                }
                else if (!inQuotes && !inLiteralQuote && inputChar == '{')
                {
                    if (sb.Length == 0)
                    {
                        sb.Append(inputChar);
                    }
                    else
                    {
                        ExtensionMethods.SetPosition(stream, -1);
                    }
                    break;
                }
                else if (!inQuotes && !inLiteralQuote && inputChar == '}')
                {
                    if (sb.Length == 0)
                    {
                        sb.Append(inputChar);
                    }
                    else
                    {
                        ExtensionMethods.SetPosition(stream, -1);
                    }
                    break;
                }
                else if (!inQuotes && !inLiteralQuote && inputChar == '=')
                {
                    if (sb.Length == 0)
                    {
                        sb.Append(inputChar);
                    }
                    else
                    {
                        ExtensionMethods.SetPosition(stream, -1);
                    }
                    break;
                }
                else
                {
                    sb.Append(inputChar);
                }

                previousCharacter = inputChar;
            }
            return sb.ToString();
        }

        public static string? GetNextTokenWithoutMatching(StreamReader sr)
        {
            string? toReturn = null;
            bool gotToken = false;
            while (!gotToken)
            {
                if (sr.EndOfStream)
                {
                    return null;
                }
                toReturn = GetNextLexeme(sr);
                gotToken = true;
            }

            return toReturn;
        }

        public string? GetNextToken(StreamReader stream)
        {
            var sb = new StringBuilder();

            var gotToken = false;
            while (!gotToken)
            {
                if (stream.EndOfStream)
                {
                    return null;
                }

                sb.Length = 0;
                sb.Append(GetNextLexeme(stream));

                var strippedToken = RemQuotes(sb.ToString());
                var isTokenQuoted = (strippedToken.Length < sb.ToString().Length);

                var matched = TryToMatch(sb.ToString(), strippedToken, isTokenQuoted, stream);

                if (!matched)
                {
                    gotToken = true;
                }
            }
            if (sb.Length != 0)
            {
                return sb.ToString();
            }
            return null;
        }

        public void ParseStream(StreamReader stream)
        {
            var braceDepth = 0;
            var value = false; // tracker to indicate whether we reached the value part of key=value pair
            var tokensSoFar = new StringBuilder();

            while (true)
            {
                string? token = GetNextToken(stream);
                if (token != null)
                {
                    tokensSoFar.Append(token);
                    if (token == "=")
                    {
                        if (!value)
                        {
                            value = true; // swapping to value part.
                            continue;
                        }
                        else // leaving else to be noticeable.
                        {
                            // value is positive, meaning we were at value, and now we're hitting an equal. This is bad. We need to
                            // manually fast-forward to brace-lvl 0 and die.
                            char inputChar;
                            while (braceDepth != 0)
                            {
                                inputChar = (char)stream.Read();
                                if (inputChar == '{')
                                {
                                    ++braceDepth;
                                }
                                else if (inputChar == '}')
                                {
                                    --braceDepth;
                                }
                                else if (!char.IsWhiteSpace(inputChar))
                                {
                                    tokensSoFar.Append(inputChar);
                                }
                            }
                            Log.WriteLine(LogLevel.Warning, "Broken token syntax at " + tokensSoFar.ToString());
                            return;
                        }
                    }
                    else if (token == "{")
                    {
                        ++braceDepth;
                    }
                    else if (token == "}")
                    {
                        --braceDepth;
                        if (braceDepth == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        Log.WriteLine(LogLevel.Warning, "Unknown token while parsing stream: " + token);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public void ParseFile(string filename)
        {
            if (!File.Exists(filename))
            {
                Log.WriteLine(LogLevel.Error, "Could not open " + filename + " for parsing");
                return;
            }
            var file = new StreamReader(File.OpenText(filename).BaseStream);
            AbsorbBOM(file);
            ParseStream(file);
        }

        readonly Dictionary<RegisteredKeywordOrRegex, AbstractDelegate> registeredDict = new();
    }
}
