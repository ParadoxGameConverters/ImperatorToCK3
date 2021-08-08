using commonItems;

namespace ImperatorToCK3.Imperator.Pops {
	public class PopFactory : commonItems.Parser {
		public Pop Pop { get; private set; }
        public PopFactory() {
			RegisterKeyword("type", (sr) => {
				Pop.Type = new SingleString(sr).String;
			});
			RegisterKeyword("culture", (sr) => {
				Pop.Culture = new SingleString(sr).String;
			});
			RegisterKeyword("religion", (sr) => {
				Pop.Religion = new SingleString(sr).String;
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}

		public Pop GetPop(string idString, BufferedReader reader) {
            Pop = new Pop {
                ID = ulong.Parse(idString)
            };
            ParseStream(reader);
			return Pop;
        }
	}
}
