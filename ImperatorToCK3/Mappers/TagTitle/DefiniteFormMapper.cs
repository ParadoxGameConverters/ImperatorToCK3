using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.TagTitle {
	public class DefiniteFormMapper {
		public DefiniteFormMapper(string configurablePath) {
			var parser = new Parser();
			parser.RegisterKeyword("names", reader => {
				ImpCountryNamesWithDefiniteForm = new HashSet<string>(ParserHelpers.GetStrings(reader));
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseFile(configurablePath);
		}
		public bool IsDefiniteForm(string imperatorCountryName) {
			return ImpCountryNamesWithDefiniteForm.Contains(imperatorCountryName);
		}
		private HashSet<string> ImpCountryNamesWithDefiniteForm = new();
	}
}
