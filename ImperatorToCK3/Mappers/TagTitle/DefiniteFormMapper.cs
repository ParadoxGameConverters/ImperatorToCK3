using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.TagTitle;

public sealed class DefiniteFormMapper {
	public DefiniteFormMapper() { }
	public DefiniteFormMapper(string configurablePath) {
		var parser = new Parser();
		parser.RegisterKeyword("names", reader => impCountryNamesWithDefiniteForm = new HashSet<string>(reader.GetStrings()));
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseFile(configurablePath);
	}
	public bool IsDefiniteForm(string imperatorCountryName) {
		return impCountryNamesWithDefiniteForm.Contains(imperatorCountryName);
	}
	private HashSet<string> impCountryNamesWithDefiniteForm = new();
}