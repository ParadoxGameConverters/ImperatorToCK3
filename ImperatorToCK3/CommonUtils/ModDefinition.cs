using commonItems;
using commonItems.Mods;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImperatorToCK3.CommonUtils;

/// <summary>Represents a mod detection definition loaded from a configurable file.</summary>
internal sealed class ModDefinition {
	public string Flag { get; }
	private readonly IReadOnlyList<Regex> nameRegexes;
	private readonly IReadOnlyList<string> ids;

	public ModDefinition(string flag, IReadOnlyList<Regex> nameRegexes, IReadOnlyList<string> ids) {
		Flag = flag;
		this.nameRegexes = nameRegexes;
		this.ids = ids;
	}

	/// <summary>Returns true if the given mod matches any of the name regexes or IDs in this definition.</summary>
	public bool IsMatch(Mod mod) {
		return nameRegexes.Any(r => r.IsMatch(mod.Name)) ||
		       ids.Any(id => mod.Path.EndsWith(id, System.StringComparison.OrdinalIgnoreCase));
	}
}

/// <summary>Reads mod definitions from a configurable file.</summary>
internal static class ModDefinitionsReader {
	/// <summary>
	/// Loads mod definitions from the given file. Returns an empty list if the file does not exist.
	/// Each entry in the file has the format:
	/// <code>
	/// flag_name = {
	///     name_regex = { "^Mod Name" }
	///     id = { ugc_1234567890.mod }
	/// }
	/// </code>
	/// </summary>
	public static IReadOnlyList<ModDefinition> LoadFromFile(string filePath) {
		if (!File.Exists(filePath)) {
			Logger.Warn($"Mod definitions file not found: {filePath}");
			return [];
		}

		var definitions = new List<ModDefinition>();

		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, flag) => {
			var nameRegexes = new List<Regex>();
			var ids = new List<string>();

			var modParser = new Parser();
			modParser.RegisterKeyword("name_regex", regexReader => {
				foreach (var pattern in regexReader.GetStrings()) {
					// Use a timeout to protect against ReDoS attacks from malicious configurable content.
					nameRegexes.Add(new Regex(pattern, RegexOptions.Compiled, matchTimeout: System.TimeSpan.FromSeconds(1)));
				}
			});
			modParser.RegisterKeyword("id", idReader => {
				ids.AddRange(idReader.GetStrings());
			});
			modParser.IgnoreAndLogUnregisteredItems();
			modParser.ParseStream(reader);

			definitions.Add(new ModDefinition(flag, nameRegexes, ids));
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(filePath);

		return definitions;
	}
}
