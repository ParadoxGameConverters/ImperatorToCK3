using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.CommonUtils.Map;

public sealed class ProvinceDefinitions : IdObjectCollection<ulong, ProvinceDefinition> {
	public IDictionary<Rgb24, ulong> ColorToProvinceDict { get; } = new Dictionary<Rgb24, ulong>();
	public Dictionary<ulong, Rgb24> ProvinceToColorDict { get; } = [];
	
	public void LoadDefinitions(string definitionsFilename, ModFilesystem modFS) {
		var relativePath = Path.Combine("map_data", definitionsFilename);
		var definitionsFilePath = modFS.GetActualFileLocation(relativePath);
		if (definitionsFilePath is null) {
			Logger.Warn($"Province definitions file {definitionsFilename} not found!");
			return;
		}

		using var fileStream = File.OpenRead(definitionsFilePath);
		using var definitionFileReader = new StreamReader(fileStream);

		definitionFileReader.ReadLine(); // discard first line

		while (!definitionFileReader.EndOfStream) {
			var line = definitionFileReader.ReadLine();
			if (line is null) {
				continue;
			}
			line = line.TrimStart();
			if (line.Length < 4 || line[0] == '#') {
				continue;
			}

			try {
				var columns = line.Split(';');
				
				var id = ulong.Parse(columns[0]);
				AddOrReplace(new ProvinceDefinition(id));
				
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