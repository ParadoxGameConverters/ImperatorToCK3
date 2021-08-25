using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.Mappers.Province {
	public class ProvinceMapping {
		public List<ulong> CK3Provinces { get; } = new();
		public List<ulong> ImperatorProvinces { get; } = new();

		private static readonly Parser parser = new();
		private static ProvinceMapping tempMapping = new();
		static ProvinceMapping() {
			parser.RegisterKeyword("ck3", reader => {
				tempMapping.CK3Provinces.Add(new SingleULong(reader).ULong);
			});
			parser.RegisterKeyword("imp", reader => {
				tempMapping.ImperatorProvinces.Add(new SingleULong(reader).ULong);
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}
		public static ProvinceMapping Parse(BufferedReader reader) {
			tempMapping = new ProvinceMapping();
			parser.ParseStream(reader);
			return tempMapping;
		}
	}
}
