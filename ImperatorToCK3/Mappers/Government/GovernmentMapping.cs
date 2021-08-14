using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.Government {
    public class GovernmentMapping {
        public SortedSet<string> ImperatorGovernments = new();
        public string Ck3Government = "";
        public GovernmentMapping(BufferedReader reader) {
            var parser = new Parser();
            parser.RegisterKeyword("ck3", (reader) => {
                Ck3Government = new SingleString(reader).String;
            });
            parser.RegisterKeyword("imp", (reader) => {
                ImperatorGovernments.Add(new SingleString(reader).String);
            });
            parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

            parser.ParseStream(reader);
            parser.ClearRegisteredRules();
        }
    }
}
