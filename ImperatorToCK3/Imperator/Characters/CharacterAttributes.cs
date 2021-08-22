using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.Imperator.Characters {
	public class CharacterAttributes {
		public int Martial { get; private set; } = 0;
		public int Finesse { get; private set; } = 0;
		public int Charisma { get; private set; } = 0;
		public int Zeal { get; private set; } = 0;

		public CharacterAttributes() { }

		private static CharacterAttributes parsedAttributes = new();
		private static readonly Parser parser = new();
		static CharacterAttributes() {
			parser.RegisterKeyword("martial", reader => {
				parsedAttributes.Martial = new SingleInt(reader).Int;
			});
			parser.RegisterKeyword("finesse", reader => {
				parsedAttributes.Finesse = new SingleInt(reader).Int;
			});
			parser.RegisterKeyword("charisma", reader => {
				parsedAttributes.Charisma = new SingleInt(reader).Int;
			});
			parser.RegisterKeyword("zeal", reader => {
				parsedAttributes.Zeal = new SingleInt(reader).Int;
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public static CharacterAttributes Parse(BufferedReader reader) {
			parsedAttributes = new CharacterAttributes();
			parser.ParseStream(reader);
			return parsedAttributes;
		}
	}
}
