using ImperatorToCK3.CK3.Provinces;
using System.IO;

namespace ImperatorToCK3.Outputter;

public static class ProvinceOutputter {
	public static void OutputProvince(TextWriter writer, Province province) {
		writer.WriteLine($"{province.Id}={{");
		if (!string.IsNullOrEmpty(province.Culture)) {
			writer.WriteLine($"\tculture={province.Culture}");
		}
		if (!string.IsNullOrEmpty(province.Religion)) {
			writer.WriteLine($"\treligion={province.Religion}");
		}
		writer.WriteLine($"\tholding={province.Holding}");
		if (province.Buildings.Count > 0) {
			writer.WriteLine("\tbuildings={");
			foreach (var building in province.Buildings) {
				writer.WriteLine($"\t\t{building}");
			}
			writer.WriteLine("\t}");
		}
		writer.WriteLine("}");
	}
}