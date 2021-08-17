using System.Collections.Generic;
using commonItems;


namespace ImperatorToCK3.Imperator.Pops {
    public class Pops {
        private PopFactory popFactory = new();
        public Dictionary<ulong, Pop> StoredPops { get; } = new();
        public void LoadPops(BufferedReader reader) {
            var parser = new Parser();
            RegisterKeys(parser);
            parser.ParseStream(reader);
            parser.ClearRegisteredRules();
        }
        private void RegisterKeys(Parser parser) {
            parser.RegisterRegex(CommonRegexes.Integer, (reader, thePopID) => {
                var popStr = new StringOfItem(reader).String;
                if (popStr.IndexOf('{') != -1) {
                    var tempStream = new BufferedReader(popStr);
                    var pop = popFactory.GetPop(thePopID, tempStream);
                    StoredPops.Add(pop.ID, pop);
                }
            });
            parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
        }
    }
}
