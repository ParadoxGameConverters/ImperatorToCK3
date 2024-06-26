using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.UnitType;

public sealed class UnitTypeMapper {
	private readonly Dictionary<string, string?> unitTypeMap = new(); // imperator -> ck3

	public UnitTypeMapper(string mappingsFilePath) {
		var parser = new Parser();
		parser.RegisterKeyword("link", mappingReader => {
			var impList = new List<string>();
			string? ck3Type = null;

			var mappingParser = new Parser();
			mappingParser.RegisterKeyword("ir", reader=>impList.Add(reader.GetString()));
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

	public IDictionary<string, int> GetMenPerCK3UnitType(IDictionary<string, int> menPerImperatorUnitType) {
		var toReturn = new Dictionary<string, int>();

		foreach (var (imperatorType, imperatorMen) in menPerImperatorUnitType) {
			var ck3Type = Match(imperatorType);
			if (ck3Type is null) {
				continue;
			}

			if (toReturn.ContainsKey(ck3Type)) {
				toReturn[ck3Type] += imperatorMen;
			} else {
				toReturn[ck3Type] = imperatorMen;
			}
		}

		return toReturn;
	}
}