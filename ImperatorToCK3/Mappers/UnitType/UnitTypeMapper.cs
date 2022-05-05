using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.UnitType; 

public class UnitTypeMapper {
	private readonly Dictionary<string, string?> unitTypeMap = new(); // imperator -> ck3

	public UnitTypeMapper(string mappingsFilePath) {
		var parser = new Parser();
		parser.RegisterKeyword("link", mappingReader => {
			var impList = new List<string>();
			string? ck3Type = null;
			
			var mappingParser = new Parser();
			mappingParser.RegisterKeyword("imp", reader=>impList.Add(reader.GetString()));
			mappingParser.RegisterKeyword("ck3", reader=>ck3Type=reader.GetString());
			mappingParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			mappingParser.ParseStream(mappingReader);

			foreach (var impType in impList) {
				unitTypeMap[impType] = ck3Type;
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseFile(mappingsFilePath);
	}

	public string? Match(string imperatorUnitType) {
		return unitTypeMap.GetValueOrDefault(imperatorUnitType, null);
	}
}