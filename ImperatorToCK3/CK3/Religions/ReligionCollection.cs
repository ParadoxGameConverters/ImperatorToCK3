using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CK3.Religions; 

public class ReligionCollection {
	public Dictionary<string, OrderedSet<Religion>> ReligionsPerFile { get; } = new();
	public Dictionary<string, OrderedSet<string>> HolySitesByFaith = new();
	
	public void LoadReligions(string religionsFolderPath) {
		var files = SystemUtils.GetAllFilesInFolderRecursive(religionsFolderPath);
		foreach (var file in files) {
			var religionsInFile = new OrderedSet<Religion>();
			
			var parser = new Parser();
			parser.RegisterRegex(CommonRegexes.String, (religionReader, religionId) => {
				var religion = new Religion(religionId, religionReader);
				religionsInFile.Add(religion);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);

			var filePath = Path.Combine(religionsFolderPath, file);
			parser.ParseFile(filePath);

			ReligionsPerFile[file] = religionsInFile;
		}
	}

	public void LoadReplaceableHolySites(string filePath) {
		Logger.Info("Loading replaceable holy sites...");
		
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, faithId) => {
			var faith = GetFaith(faithId);
			if (faith is null) {
				Logger.Warn($"Faith \"{faithId}\" not found!");
				return;
			}

			var value = reader.GetStringOfItem();
			// TODO: USE value.IsArrayOrObject
			var valueStr = value.ToString();
			var indexOfBracket = valueStr.IndexOf('{');
			if (indexOfBracket != -1 && (!valueStr.Contains('"') || valueStr.IndexOf('"') > indexOfBracket)) {
				// is array
				HolySitesByFaith[faithId] = new OrderedSet<string>(new BufferedReader(valueStr).GetStrings());
			} else if (valueStr == "all") {
				HolySitesByFaith[faithId] = new OrderedSet<string>(faith.HolySites);
			} else Logger.Warn($"Unexpected value: {valueStr}");
		});
	}

	public Faith? GetFaith(string id) {
		foreach (Religion religion in ReligionsPerFile.Values.SelectMany(religionSet => religionSet)) {
			if (religion.Faiths.TryGetValue(id, out var faith)) {
				return faith;
			}
		}

		return null;
	}

	public void DetermineHolySites() {
		foreach (var religionsSet in ReligionsPerFile.Values) {
			foreach (var religion in religionsSet) {
				foreach (var faith in religion.Faiths) {
					
				}
			}
		}
	}
}