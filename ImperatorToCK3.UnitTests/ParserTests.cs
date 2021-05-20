using Microsoft.VisualStudio.TestTools.UnitTesting;
using commonItems;
using ImperatorToCK3;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace ImperatorToCK3.UnitTests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void AbsorbBOMAbsorbsBOM()
        {
            Stream input = Parser.GenerateStreamFromString("\xEF\xBB\xBFMore text");
            var stream = new BufferedStreamReader(input);
            Parser.AbsorbBOM(stream);
            Assert.AreEqual("More text", stream.ReadToEnd());
        }

        [TestMethod]
        public void AbsorbBOMDoesNotAbsorbNonBOM()
        {
            Stream input = Parser.GenerateStreamFromString("More text");
            var stream = new BufferedStreamReader(input);
            Parser.AbsorbBOM(stream);
            Assert.AreEqual("More text", stream.ReadToEnd());
        }

        public class Test : Parser
        {
            public string key;
            public string value;
            public Test(BufferedStreamReader streamReader)
            {
                RegisterKeyword("key", (BufferedStreamReader sr, string k) => {
                    Log.WriteLine(commonItems.LogLevel.Debug, "FUCKING K IS: "+k);
                    key = k;
                    value = new SingleString(sr).GetString();
                });
                ParseStream(streamReader);
            }
        };

        [TestMethod]
        public void KeywordsAreMatched()
        {
            Stream input = Parser.GenerateStreamFromString("key = value");
            var streamReader = new BufferedStreamReader(input);
            var test = new Test(streamReader);
            Assert.AreEqual("key", test.key);
            Assert.AreEqual("value", test.value);
        }
    }
}
