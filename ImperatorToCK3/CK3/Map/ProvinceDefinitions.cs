using commonItems.Mods;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.CK3.Map;

public class ProvinceDefinitions {
	public Dictionary<Rgb24, ulong> ColorToProvinceDict { get; } = new();
	public SortedDictionary<ulong, Rgb24> ProvinceToColorDict { get; } = new();
	public ProvinceDefinitions(ModFilesystem ck3ModFS) {
		const string relativePath = "map_data/definition.csv";
		var definitionsFilePath = ck3ModFS.GetActualFileLocation(relativePath);
		if (definitionsFilePath is null) {
			throw new FileNotFoundException(message: null, fileName: relativePath);
		}

		using var fileStream = File.OpenRead(definitionsFilePath);
		using var definitionFileReader = new StreamReader(fileStream);

		definitionFileReader.ReadLine(); // discard first line

		while (!definitionFileReader.EndOfStream) {
			var line = definitionFileReader.ReadLine();
			if (line is null || line.Length < 4 || line[0] == '#' || line[1] == '#') {
				continue;
			}

			try {
				var columns = line.Split(';');
				var id = ulong.Parse(columns[0]);
				var r = byte.Parse(columns[1]);
				var g = byte.Parse(columns[2]);
				var b = byte.Parse(columns[3]);
				var color = new Rgb24(r, g, b);
				ProvinceToColorDict.Add(id, color);
				ColorToProvinceDict[color] = id;
			} catch (Exception e) {
				throw new FormatException($"Line: |{line}| is unparseable! Breaking. ({e})");
			}
		}
	}
}