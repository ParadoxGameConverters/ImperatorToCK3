using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.CommonUtils.Map;

internal sealed class ProvinceDefinitions : IdObjectCollection<ulong, ProvinceDefinition> {
	public Dictionary<Rgb24, ulong> ColorToProvinceDict { get; } = [];
	public Dictionary<ulong, Rgb24> ProvinceToColorDict { get; } = [];

	public void LoadDefinitions(string definitionsFilename, ModFilesystem modFS) {
		var relativePath = Path.Combine("map_data", definitionsFilename);
		var definitionsFilePath = modFS.GetActualFileLocation(relativePath);
		if (definitionsFilePath is null) {
			Logger.Warn($"Province definitions file {definitionsFilename} not found!");
			return;
		}
		LoadDefinitionsOptimized(definitionsFilePath, null);
	}

	public void LoadDefinitionsOptimized(string definitionsFilename, ModFilesystem? modFS = null) {
		string? definitionsFilePath;
		if (modFS is null) {
			definitionsFilePath = definitionsFilename;
		} else {
			var relativePath = Path.Combine("map_data", definitionsFilename);
			definitionsFilePath = modFS.GetActualFileLocation(relativePath);
		}
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
				var span = line.AsSpan();
				int pos = 0;

				// id
				var idEnd = span.IndexOf(';');
				if (idEnd < 0) throw new FormatException("Missing separators");
				var idSpan = span[pos..idEnd];
				pos = idEnd + 1;
				if (!ulong.TryParse(idSpan, out var id)) {
					throw new FormatException($"Invalid id: {idSpan.ToString()}");
				}
				AddOrReplace(new ProvinceDefinition(id));

				// r
				var rEnd = span[pos..].IndexOf(';');
				if (rEnd < 0) throw new FormatException("Missing separators");
				var rSpan = span[pos..(pos + rEnd)];
				pos += rEnd + 1;
				if (!byte.TryParse(rSpan, out var r)) {
					throw new FormatException($"Invalid r: {rSpan.ToString()}");
				}

				// g
				var gEnd = span[pos..].IndexOf(';');
				if (gEnd < 0) throw new FormatException("Missing separators");
				var gSpan = span[pos..(pos + gEnd)];
				pos += gEnd + 1;
				if (!byte.TryParse(gSpan, out var g)) {
					throw new FormatException($"Invalid g: {gSpan.ToString()}");
				}

				// b
				var bEnd = span[pos..].IndexOf(';');
				if (bEnd < 0) throw new FormatException("Missing separators");
				var bSpan = span[pos..(pos + bEnd)];
				pos += bEnd + 1;
				if (!byte.TryParse(bSpan, out var b)) {
					throw new FormatException($"Invalid b: {bSpan.ToString()}");
				}

				var color = new Rgb24(r, g, b);
				ProvinceToColorDict.Add(id, color);
				ColorToProvinceDict[color] = id;
			} catch (Exception e) {
				throw new FormatException($"Line: |{line}| is unparseable! Breaking. ({e})");
			}
		}
	}
}