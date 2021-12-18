using commonItems;
using commonItems.Collections;

namespace ImperatorToCK3.Imperator.Families {
	public class FamilyCollection : IdObjectCollection<ulong, Family> {
		public void LoadFamilies(BufferedReader reader) {
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);
		}

		private void RegisterKeys(Parser parser) {
			parser.RegisterRegex(CommonRegexes.Integer, (reader, familyIdStr) => {
				var familyStr = new StringOfItem(reader).ToString();
				if (!familyStr.Contains('{')) {
					return;
				}
				var tempReader = new BufferedReader(familyStr);
				var id = ulong.Parse(familyIdStr);
				var newFamily = Family.Parse(tempReader, id);
				var inserted = TryAdd(newFamily);
				if (!inserted) {
					Logger.Debug($"Redefinition of family {id}.");
					dict[newFamily.Id] = newFamily;
				}
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public void RemoveUnlinkedMembers() {
			foreach (var family in this) {
				family.RemoveUnlinkedMembers();
			}
		}

		public static FamilyCollection ParseBloc(BufferedReader reader) {
			var blocParser = new Parser();
			var families = new FamilyCollection();
			blocParser.RegisterKeyword("families", reader =>
				families.LoadFamilies(reader)
			);
			blocParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			blocParser.ParseStream(reader);
			blocParser.ClearRegisteredRules();

			Logger.Debug($"Ignored Family tokens: {string.Join(", ", Family.IgnoredTokens)}");
			return families;
		}
	}
}
