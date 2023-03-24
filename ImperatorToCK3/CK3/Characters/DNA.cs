using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CK3.Characters;

public class DNA {
	public class PaletteCoordinates {
		// hair, skin and eye color palettes are 256x256
		public int X { get; init; } = 128;
		public int Y { get; init; } = 128;
	}

	public string Id { get; }
	
	private readonly Dictionary<string, string> colorAndMorphDNAValues;
	private readonly Dictionary<string, AccessoryGeneValue> accessoryDNAValues;
	public IReadOnlyDictionary<string, AccessoryGeneValue> AccessoryDNAValues => accessoryDNAValues;

	public IEnumerable<string> DNALines {
		get {
			var colorAndMorphGeneLines = colorAndMorphDNAValues
				.Select(kvp => $"{kvp.Key}={{ {kvp.Value} }}");
			var accessoryGeneLines = accessoryDNAValues
				.Select(kvp => $"{kvp.Key}={{ {kvp.Value} }}");
			return colorAndMorphGeneLines.Concat(accessoryGeneLines);
		}
	}

	public DNA(string id, IDictionary<string, string> colorAndMorphDNAValues, IDictionary<string, AccessoryGeneValue> accessoryDNAValues) {
		Id = id;
		this.colorAndMorphDNAValues = new Dictionary<string, string>(colorAndMorphDNAValues);
		this.accessoryDNAValues = new Dictionary<string, AccessoryGeneValue>(accessoryDNAValues);
	}

	public void OutputGenes(StreamWriter output) {
		output.WriteLine("\t\tgenes={");

		foreach (var dnaLine in DNALines) {
			output.WriteLine($"\t\t\t{dnaLine}");
		}

		output.WriteLine("\t\t}");
	}
}