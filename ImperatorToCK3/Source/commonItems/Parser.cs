using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel.Design;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace commonItems
{
    public delegate void Del(BufferedStreamReader stream, string? keyword = null);

    public class Parser
    {
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
        public static void AbsorbBOM(BufferedStreamReader stream)
        {
            var firstChar = stream.Peek();
            if (firstChar == '\xEF')
            {
                stream.Read(new char[3]); // skip 3 bytes
            }
        }


        public void RegisterKeyword(string keyword, Del del)
        {
            registeredStuff.Add(new Regex(keyword), del);
        }
        public void RegisterRegex(string keyword, Del del)
        {
            registeredStuff.Add(new Regex(keyword), del);
        }

        bool TryToMatchAgainstRegexes(string token, BufferedStreamReader stream)
        {
            foreach (var (regex, fun) in registeredStuff) {
                if (regex.IsMatch(token))
                {
                    fun(stream, token);
                }
            }
            return false;
        }

        public static string GetNextLexeme(BufferedStreamReader stream)
        {
            var sb = new StringBuilder();

            var inQuotes = false;
            var inLiteralQuote = false;
            char previousCharacter = '\0';

            while (true)
            {
                char inputChar = (char)stream.Read();

                if (stream.EndOfStream)
                {
                    break;
                }

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
                        else // fix Paradox' mistake and don't break proper names in half
                        {
                            sb.Append(' ');
                        }
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
                        stream.PushBack('{');
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
                        stream.PushBack('}');
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
                        stream.PushBack('='); // this is where I think it breaks (endofSstream is not changed)
                    }
                    break;
                }
                else
                {
                    sb.Append(inputChar);
                }

                previousCharacter = inputChar;
            }
            Log.WriteLine(LogLevel.Debug, sb.ToString()); // TODO: REMOVE DEBUG
            return sb.ToString();
        }

        public static string? GetNextTokenWithoutMatching(BufferedStreamReader sr)
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

        public string? GetNextToken(BufferedStreamReader stream)
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

                var strippedLexeme = RemQuotes(sb.ToString());
                var isLexemeQuoted = (strippedLexeme.Length < sb.ToString().Length);

                var matched = TryToMatchAgainstRegexes(sb.ToString(), stream);

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

        public void ParseStream(BufferedStreamReader stream)
        {
            var braceDepth = 0;
            var value = false; // tracker to indicate whether we reached the value part of key=value pair
            var tokensSoFar = new StringBuilder("");

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
            var file = new BufferedStreamReader(File.OpenText(filename).BaseStream);
            AbsorbBOM(file);
            ParseStream(file);
        }

        Dictionary<Regex, Del> registeredStuff = new();
    }
}
