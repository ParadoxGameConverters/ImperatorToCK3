using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ImperatorToCK3.CK3.Characters;

internal sealed class DNA {
	[StructLayout(LayoutKind.Auto)]
	internal readonly struct PaletteCoordinates {
		// hair, skin and eye color palettes are 256x256
		public ushort X { get; init; } = 128;
		public ushort Y { get; init; } = 128;

		internal PaletteCoordinates(ushort x, ushort y) {
			X = x;
			Y = y;
		}
	}

	public string Id { get; }

	private readonly Dictionary<string, DNAColorGeneValue> colorDNAValues;
	private readonly Dictionary<string, DNAGeneValue> morphDNAValues;
	private readonly Dictionary<string, DNAAccessoryGeneValue> accessoryDNAValues;
	public IReadOnlyDictionary<string, DNAAccessoryGeneValue> AccessoryDNAValues => accessoryDNAValues;

	public IEnumerable<string> DNALines {
		get {
			foreach (var kvp in colorDNAValues) yield return $"{kvp.Key}={{ {kvp.Value} }}";
			foreach (var kvp in morphDNAValues) yield return $"{kvp.Key}={{ {kvp.Value} }}";
			foreach (var kvp in accessoryDNAValues) yield return $"{kvp.Key}={{ {kvp.Value} }}";
		}
	}

	public DNA(
		string id,
		Dictionary<string, DNAColorGeneValue> colorDNAValues,
		Dictionary<string, DNAGeneValue> morphDNAValues,
		Dictionary<string, DNAAccessoryGeneValue> accessoryDNAValues
	) {
		Id = id;
		this.colorDNAValues = new(colorDNAValues);
		this.morphDNAValues = new(morphDNAValues);
		this.accessoryDNAValues = new(accessoryDNAValues);
	}

	public void WriteGenes(StringBuilder sb) {
		sb.AppendLine("\t\tgenes={");

		foreach (var kvp in colorDNAValues)
			sb.Append("\t\t\t").Append(kvp.Key).Append("={ ").Append(kvp.Value).AppendLine(" }");
		foreach (var kvp in morphDNAValues)
			sb.Append("\t\t\t").Append(kvp.Key).Append("={ ").Append(kvp.Value).AppendLine(" }");
		foreach (var kvp in accessoryDNAValues)
			sb.Append("\t\t\t").Append(kvp.Key).Append("={ ").Append(kvp.Value).AppendLine(" }");

		sb.AppendLine("\t\t}");
	}
}