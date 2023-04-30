using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.Province;

public class ProvinceMapping {
	public List<ulong> CK3Provinces { get; } = new();
	public List<ulong> ImperatorProvinces { get; } = new();

	private static readonly Parser parser = new();
	private static ProvinceMapping tempMapping = new();
	static ProvinceMapping() {
		parser.RegisterKeyword("ck3", reader => tempMapping.CK3Provinces.Add(reader.GetULong()));
		parser.RegisterKeyword("imp", reader => tempMapping.ImperatorProvinces.Add(reader.GetULong()));
		parser.RegisterKeyword("comment", ParserHelpers.IgnoreItem);
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public static ProvinceMapping Parse(BufferedReader reader) {
		tempMapping = new ProvinceMapping();
		parser.ParseStream(reader);
		return tempMapping;
	}
}