using commonItems;
using commonItems.Mods;
using ImageMagick;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.Exceptions;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Mappers.Gene;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
	
	private readonly Dictionary<string, string> dnaValues;

	public IEnumerable<string> DNALines => dnaValues.Select(kvp => $"{kvp.Key}={{{kvp.Value}}}");


	public DNA(string id, IDictionary<string, string> dnaValues) {
		Id = id;
		this.dnaValues = new Dictionary<string, string>(dnaValues);
	}

	public void OutputGenes(StreamWriter output) {
		output.WriteLine("\t\tgenes={");

		foreach (var (geneName, geneValue) in dnaValues) {
			output.WriteLine($"\t\t\t{geneName}={{ {geneValue} }}");
		}

		output.WriteLine("\t\t}");
	}
}