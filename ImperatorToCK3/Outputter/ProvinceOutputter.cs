using commonItems.Serialization;
using ImperatorToCK3.CK3.Provinces;
using System.IO;

namespace ImperatorToCK3.Outputter;

public static class ProvinceOutputter {
	public static void OutputProvince(TextWriter writer, Province province) {
		writer.WriteLine($"{province.Id}={{");
		writer.Write(PDXSerializer.Serialize(province.History, indent: "\t"));
		writer.WriteLine("}");
	}
}