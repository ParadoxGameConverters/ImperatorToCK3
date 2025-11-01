using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ImperatorToCK3.CK3.Cultures;

internal sealed partial class NameList : IIdentifiable<string> {
	public string Id { get; }
	private readonly OrderedSet<string> maleNames = [];
	private readonly OrderedSet<string> femaleNames = [];
	public IReadOnlyCollection<string> MaleNames => maleNames;
	public IReadOnlyCollection<string> FemaleNames => femaleNames;

	public NameList(string id, BufferedReader nameListReader) {
		Id = id;

		var parser = new Parser();
		parser.RegisterRegex(MaleNamesRegex(), maleNamesReader => {
			var maleNamesBlockParser = new Parser();
			maleNamesBlockParser.RegisterRegex(CommonRegexes.Integer, (weightedBlockReader, _) => {
				maleNames.UnionWith(weightedBlockReader.GetStrings());
			});
			maleNamesBlockParser.RegisterRegex(CommonRegexes.String, (_, nameStr) => {
				maleNames.Add(nameStr);
			});
			maleNamesBlockParser.RegisterRegex(CommonRegexes.QuotedString, (_, quotedNameStr) => {
				maleNames.Add(quotedNameStr.RemQuotes());
			});
			maleNamesBlockParser.IgnoreAndLogUnregisteredItems();
			maleNamesBlockParser.ParseStream(maleNamesReader);
		});
		parser.RegisterRegex(FemaleNamesRegex(), reader => {
			var femaleNamesBlockParser = new Parser();
			femaleNamesBlockParser.RegisterRegex(CommonRegexes.Integer, (weightedBlockReader, _) => {
				femaleNames.UnionWith(weightedBlockReader.GetStrings());
			});
			femaleNamesBlockParser.RegisterRegex(CommonRegexes.String, (_, nameStr) => {
				femaleNames.Add(nameStr);
			});
			femaleNamesBlockParser.RegisterRegex(CommonRegexes.QuotedString, (_, quotedNameStr) => {
				femaleNames.Add(quotedNameStr.RemQuotes());
			});
			femaleNamesBlockParser.IgnoreAndLogUnregisteredItems();
			femaleNamesBlockParser.ParseStream(reader);
		});
		parser.IgnoreUnregisteredItems();
		parser.ParseStream(nameListReader);
	}

	[GeneratedRegex("male_names", RegexOptions.IgnoreCase, "en-US")]
	private static partial Regex MaleNamesRegex();
	[GeneratedRegex("female_names", RegexOptions.IgnoreCase, "en-US")]
	private static partial Regex FemaleNamesRegex();
}