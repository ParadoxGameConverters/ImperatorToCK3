using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.CK3.Map {
	public class ProvinceDefinitions {
		public Dictionary<MagickColor, ulong> ColorToProvinceDict { get; } = new();
		public SortedDictionary<ulong, MagickColor> ProvinceToColorDict { get; } = new();
		public ProvinceDefinitions(string ck3Path) {
			var definitionsFilePath = Path.Combine(ck3Path, "game/map_data/definition.csv");
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
					var color = MagickColor.FromRgb(r, g, b);
					ProvinceToColorDict.Add(id, color);
					ColorToProvinceDict[color] = id;
				} catch (Exception e) {
					throw new FormatException($"Line: |{line}| is unparseable! Breaking. ({e})");
				}
			}
		}
	}
}
