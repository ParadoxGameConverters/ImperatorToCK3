using System.IO;
using commonItems;
using ImperatorToCK3;
using Xunit;

namespace ImperatorToCK3.UnitTests
{
    public class ParserTests
    {
        [Fact]
        public void AbsorbBOMAbsorbsBOM()
        {
            Stream input = Parser.GenerateStreamFromString("\xEF\xBB\xBFMore text");
            var stream = new StreamReader(input);
            Parser.AbsorbBOM(stream);
            Assert.Equal("More text", stream.ReadToEnd());
        }

        [Fact]
        public void AbsorbBOMDoesNotAbsorbNonBOM()
        {
            Stream input = Parser.GenerateStreamFromString("More text");
            var stream = new StreamReader(input);
            Parser.AbsorbBOM(stream);
            Assert.Equal("More text", stream.ReadToEnd());
        }

        private class Test : Parser
        {
            public string key;
            public string value;
            public Test(StreamReader streamReader)
            {
                RegisterKeyword("key", (StreamReader sr, string k) =>
                {
                    key = k;
                    value = new SingleString(sr).String;
                });
                ParseStream(streamReader);
            }
        };

        [Fact]
        public void KeywordsAreMatched()
        {
            Stream input = Parser.GenerateStreamFromString("key = value");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.Equal("key", test.key);
            Assert.Equal("value", test.value);
        }

        [Fact]
        public void QuotedKeywordsAreMatched()
        {
            Stream input = Parser.GenerateStreamFromString("\"key\" = value");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.Equal("\"key\"", test.key);
            Assert.Equal("value", test.value);
        }

        private class Test2 : Parser
        {
            public string key;
            public string value;
            public Test2(StreamReader streamReader)
            {
                RegisterKeyword("\"key\"", (StreamReader sr, string k) =>
                {
                    key = k;
                    value = new SingleString(sr).String;
                });
                ParseStream(streamReader);
            }
        };

        [Fact]
        public void QuotedKeywordsAreQuotedlyMatched()
        {
            Stream input = Parser.GenerateStreamFromString("\"key\" = value");
            var streamReader = new StreamReader(input);
            var test = new Test2(streamReader);
            Assert.Equal("\"key\"", test.key);
            Assert.Equal("value", test.value);
        }

        [Fact]
        public void QuotedValuesAreParsed()
        {
            Stream input = Parser.GenerateStreamFromString(@"key = ""value quote""");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.Equal("key", test.key);
            Assert.Equal("value quote", test.value);
        }

        [Fact]
        public void QuotedValuesWithEscapedQuotesAreParsed()
        {
            Stream input = Parser.GenerateStreamFromString(@"key = ""value \""quote\"" string""");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.Equal("key", test.key);
            Assert.Equal(@"value \""quote\"" string", test.value);
        }

        [Fact]
        public void StringLiteralsAreParsed()
        {
            Stream input = Parser.GenerateStreamFromString(@"key = R""(value ""quote"" string)""");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.Equal("key", test.key);
            Assert.Equal(@"value ""quote"" string", test.value);
        }

        [Fact]
        public void WrongKeywordsAreIgnored()
        {
            Stream input = Parser.GenerateStreamFromString(@"wrongkey = value");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.True(string.IsNullOrEmpty(test.key));
            Assert.True(string.IsNullOrEmpty(test.value));
        }

        private class Test3 : Parser
        {
            public string key;
            public string value;
            public Test3(StreamReader streamReader)
            {
                RegisterRegex("[key]+", (StreamReader sr, string k) =>
                {
                    key = k;
                    value = new SingleString(sr).String;
                });
                ParseStream(streamReader);
            }
        };

        [Fact]
        public void QuotedRegexesAreMatched()
        {
            Stream input = Parser.GenerateStreamFromString("\"key\" = value");
            var streamReader = new StreamReader(input);
            var test = new Test3(streamReader);
            Assert.Equal("\"key\"", test.key);
            Assert.Equal("value", test.value);
        }

        private class Test4 : Parser
        {
            public string key;
            public string value;
            public Test4(StreamReader streamReader)
            {
                RegisterRegex("[k\"ey]+", (StreamReader sr, string k) =>
                {
                    key = k;
                    value = new SingleString(sr).String;
                });
                ParseStream(streamReader);
            }
        };

        [Fact]
        public void QuotedRegexesAreQuotedlyMatched()
        {
            Stream input = Parser.GenerateStreamFromString("\"key\" = value");
            var streamReader = new StreamReader(input);
            var test = new Test4(streamReader);
            Assert.Equal("\"key\"", test.key);
            Assert.Equal("value", test.value);
        }

        private class Test5 : Parser
        {
            public string key;
            public string value;
            public Test5(StreamReader streamReader)
            {
                RegisterRegex(CommonRegexes.Catchall, (StreamReader sr, string k) =>
                {
                    key = k;
                    value = new SingleString(sr).String;
                });
                ParseStream(streamReader);
            }
        };

        [Fact]
        public void CatchAllCatchesQuotedKeys()
        {
            Stream input = Parser.GenerateStreamFromString("\"key\" = value");
            var streamReader = new StreamReader(input);
            var test = new Test5(streamReader);
            Assert.Equal("\"key\"", test.key);
            Assert.Equal("value", test.value);
        }

        [Fact]
        public void CatchAllCatchesQuotedKeysWithWhitespaceInside()
        {
            Stream input = Parser.GenerateStreamFromString("\"this\tis a\nkey\n\" = value");
            var streamReader = new StreamReader(input);
            var test = new Test5(streamReader);
            Assert.Equal("\"this\tis a key \"", test.key);
            Assert.Equal("value", test.value);
        }

        [Fact]
        public void CatchAllCatchesQuotedKeysWithFigurativeCrapInside()
        {
            Stream input = Parser.GenerateStreamFromString("\"this = is a silly { key\t} \" = value");
            var streamReader = new StreamReader(input);
            var test = new Test5(streamReader);
            Assert.Equal("\"this = is a silly { key\t} \"", test.key);
            Assert.Equal("value", test.value);
        }
    }
}
