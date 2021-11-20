using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Families {
	public class Families : Dictionary<ulong, Family> {
		public void LoadFamilies(string path) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseFile(path);
		}
		public void LoadFamilies(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);
		}

		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, familyIdStr) => {
				var familyStr = new StringOfItem(reader).String;
				if (!familyStr.Contains('{')) {
					return;
				}
				var tempReader = new BufferedReader(familyStr);
				var id = ulong.Parse(familyIdStr);
				var newFamily = Family.Parse(tempReader, id);
				var inserted = TryAdd(newFamily.Id, newFamily);
				if (!inserted) {
					Logger.Debug($"Redefinition of family {familyIdStr}.");
					this[newFamily.Id] = newFamily;
				}
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public void RemoveUnlinkedMembers() {
			foreach (var family in Values) {
				family.RemoveUnlinkedMembers();
			}
		}

		public static Families ParseBloc(BufferedReader reader) {
			var blocParser = new Parser();
			var families = new Families();
			blocParser.RegisterKeyword("families", reader =>
				families.LoadFamilies(reader)
			);
			blocParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			blocParser.ParseStream(reader);
			blocParser.ClearRegisteredRules();

			Logger.Debug("Ignored Family tokens: " + string.Join(", ", Family.IgnoredTokens));
			return families;
		}
	}
}
