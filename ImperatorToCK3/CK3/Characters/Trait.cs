using commonItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.CK3.Characters {
	internal class Trait {
		public string Id { get; }
		public ISet<string> Opposites = new HashSet<string>();

		public Trait(string id, BufferedReader traitReader) {
			Id = id;

			var parser = new Parser();
			parser.RegisterKeyword("opposites", reader => Opposites = reader.GetStrings().ToHashSet());
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
			parser.ParseStream(traitReader);
		}
	}
}
