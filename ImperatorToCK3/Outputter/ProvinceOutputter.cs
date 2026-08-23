using commonItems.Serialization;
using ImperatorToCK3.CK3.Provinces;
using System.Text;

namespace ImperatorToCK3.Outputter;

internal static class ProvinceOutputter {
	public static void WriteProvince(StringBuilder sb, Province province, bool isCountyCapital) {
		// If the province is not a county capital, remove the "culture" and "faith" fields.
		if (!isCountyCapital) {
			province.History.Fields.Remove("culture");
			province.History.Fields.Remove("faith");
		}
		
		var serializedHistory = PDXSerializer.Serialize(province.History, indent: "\t");
		if (string.IsNullOrWhiteSpace(serializedHistory.Trim())) {
			return;
		}

		sb.AppendLine($"{province.Id}={{");
		sb.Append(serializedHistory);
		sb.AppendLine("}");
	}
}