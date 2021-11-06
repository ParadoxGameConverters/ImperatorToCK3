using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Imperator.Families {
	public class Families : Parser {
		public Dictionary<ulong, Family> StoredFamilies { get; } = new();
		public void LoadFamilies(string path) {
			RegisterKeys();
			ParseFile(path);
			ClearRegisteredRules();
		}
		public void LoadFamilies(BufferedReader reader) {
			RegisterKeys();
			ParseStream(reader);
			ClearRegisteredRules();
		}

		private void RegisterKeys() {
			RegisterRegex(CommonRegexes.Integer, (reader, familyIdStr) => {
				var familyStr = new StringOfItem(reader).String;
				if (familyStr.IndexOf('{') == -1) {
					return;
				}

				var tempReader = new BufferedReader(familyStr);
				var id = ulong.Parse(familyIdStr);
				var newFamily = Family.Parse(tempReader, id);
				var inserted = StoredFamilies.TryAdd(newFamily.Id, newFamily);
				if (!inserted) {
					Logger.Debug($"Redefinition of family {familyIdStr}.");
					StoredFamilies[newFamily.Id] = newFamily;
				}
			});
			RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public void RemoveUnlinkedMembers() {
			foreach (var family in StoredFamilies.Values) {
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
