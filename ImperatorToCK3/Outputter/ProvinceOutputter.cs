using commonItems.Serialization;
using ImperatorToCK3.CK3.Provinces;
using System.Text;

namespace ImperatorToCK3.Outputter;

public static class ProvinceOutputter {
	public static void WriteProvince(StringBuilder sb, Province province) {
		var serializedHistory = PDXSerializer.Serialize(province.History, indent: "\t");
		if (string.IsNullOrWhiteSpace(serializedHistory.Trim())) {
			return;
		}

		sb.AppendLine($"{province.Id}={{");
		sb.Append(serializedHistory);
		sb.AppendLine("}");
	}
}