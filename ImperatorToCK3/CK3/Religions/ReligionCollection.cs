using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.CK3.Religions; 

public class ReligionCollection {
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

	public Dictionary<string, OrderedSet<Religion>> ReligionsPerFile { get; } = new();
}