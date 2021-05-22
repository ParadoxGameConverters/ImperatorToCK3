using commonItems;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace ImperatorToCK3.UnitTests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void AbsorbBOMAbsorbsBOM()
        {
            Stream input = Parser.GenerateStreamFromString("\xEF\xBB\xBFMore text");
            var stream = new StreamReader(input);
            Parser.AbsorbBOM(stream);
            Assert.AreEqual("More text", stream.ReadToEnd());
        }

        [TestMethod]
        public void AbsorbBOMDoesNotAbsorbNonBOM()
        {
            Stream input = Parser.GenerateStreamFromString("More text");
            var stream = new StreamReader(input);
            Parser.AbsorbBOM(stream);
            Assert.AreEqual("More text", stream.ReadToEnd());
        }

        public class Test : Parser
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

        [TestMethod]
        public void KeywordsAreMatched()
        {
            Stream input = Parser.GenerateStreamFromString("key = value");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.AreEqual("key", test.key);
            Assert.AreEqual("value", test.value);
        }

        [TestMethod]
        public void QuotedKeywordsAreMatched()
        {
            Stream input = Parser.GenerateStreamFromString("\"key\" = value");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.AreEqual("\"key\"", test.key);
            Assert.AreEqual("value", test.value);
        }

        public class Test2 : Parser
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

        [TestMethod]
        public void QuotedKeywordsAreQuotedlyMatched()
        {
            Stream input = Parser.GenerateStreamFromString("\"key\" = value");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.AreEqual("\"key\"", test.key);
            Assert.AreEqual("value", test.value);
        }

        [TestMethod]
        public void QuotedValuesAreParsed()
        {
            Stream input = Parser.GenerateStreamFromString(@"key = ""value quote""");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.AreEqual("key", test.key);
            Assert.AreEqual("value quote", test.value);
        }

        [TestMethod]
        public void QuotedValuesWithEscapedQuotesAreParsed()
        {
            Stream input = Parser.GenerateStreamFromString(@"key = ""value \""quote\"" string""");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.AreEqual("key", test.key);
            Assert.AreEqual(@"value \""quote\"" string", test.value);
        }

        [TestMethod]
        public void StringLiteralsAreParsed()
        {
            Stream input = Parser.GenerateStreamFromString(@"key = R""(value ""quote"" string)""");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.AreEqual("key", test.key);
            Assert.AreEqual(@"value ""quote"" string", test.value);
        }

        [TestMethod]
        public void WrongKeywordsAreIgnored()
        {
            Stream input = Parser.GenerateStreamFromString(@"wrongkey = value");
            var streamReader = new StreamReader(input);
            var test = new Test(streamReader);
            Assert.IsTrue(string.IsNullOrEmpty(test.key));
            Assert.IsTrue(string.IsNullOrEmpty(test.value));
        }

        public class Test3 : Parser
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

        [TestMethod]
        public void QuotedRegexesAreMatched()
        {
            Stream input = Parser.GenerateStreamFromString("\"key\" = value");
            var streamReader = new StreamReader(input);
            var test = new Test3(streamReader);
            Assert.AreEqual("\"key\"", test.key);
            Assert.AreEqual("value", test.value);
        }

        public class Test4 : Parser
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

        [TestMethod]
        public void QuotedRegexesAreQuotedlyMatched()
        {
            Stream input = Parser.GenerateStreamFromString("\"key\" = value");
            var streamReader = new StreamReader(input);
            var test = new Test4(streamReader);
            Assert.AreEqual("\"key\"", test.key);
            Assert.AreEqual("value", test.value);
        }

        public class Test5 : Parser
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

        [TestMethod]
        public void CatchAllCatchesQuotedKeys()
        {
            Stream input = Parser.GenerateStreamFromString("\"key\" = value");
            var streamReader = new StreamReader(input);
            var test = new Test5(streamReader);
            Assert.AreEqual("\"key\"", test.key);
            Assert.AreEqual("value", test.value);
        }

        [TestMethod]
        public void CatchAllCatchesQuotedKeysWithWhitespaceInside()
        {
            Stream input = Parser.GenerateStreamFromString("\"this\tis a\nkey\n\" = value");
            var streamReader = new StreamReader(input);
            var test = new Test5(streamReader);
            Assert.AreEqual("\"this\tis a key \"", test.key);
            Assert.AreEqual("value", test.value);
        }

        [TestMethod]
        public void CatchAllCatchesQuotedKeysWithFigurativeCrapInside()
        {
            Stream input = Parser.GenerateStreamFromString("\"this = is a silly { key\t} \" = value");
            var streamReader = new StreamReader(input);
            var test = new Test5(streamReader);
            Assert.AreEqual("\"this = is a silly { key\t} \"", test.key);
            Assert.AreEqual("value", test.value);
        }
    }
}
