using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.CommonUtils.Map;

internal sealed class ProvinceDefinitions : IdObjectCollection<ulong, ProvinceDefinition> {
	internal Dictionary<Rgb24, ulong> ColorToProvinceDict { get; } = [];
	internal Dictionary<ulong, Rgb24> ProvinceToColorDict { get; } = [];

	internal void LoadDefinitions(string definitionsFilename, ModFilesystem modFS) {
		string? definitionsFilePath = GetDefinitionsFilePath(definitionsFilename, modFS);
		if (definitionsFilePath is null) {
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
			if (ShouldSkipLine(line)) {
				continue;
			}

			try {
				ParseDefinitionLine(line);
			} catch (Exception e) {
				throw new FormatException($"Line: |{line}| is unparseable! Breaking. ({e})");
			}
		}
	}

	private static string? GetDefinitionsFilePath(string definitionsFilename, ModFilesystem modFS) {
		var relativePath = Path.Combine("map_data", definitionsFilename);
		string? definitionsFilePath = modFS.GetActualFileLocation(relativePath);
		if (definitionsFilePath is null) {
			Logger.Warn($"Province definitions file {definitionsFilename} not found!");
		}
		return definitionsFilePath;
	}

	private static bool ShouldSkipLine(string line) {
		return line.Length < 4 || line[0] == '#';
	}

	private void ParseDefinitionLine(string line) {
		var span = line.AsSpan();
		int pos = 0;

		var id = ParseProvinceId(span, ref pos);
		AddOrReplace(new ProvinceDefinition(id));

		var color = new Rgb24(
			ParseColorComponent(span, ref pos, "r"),
			ParseColorComponent(span, ref pos, "g"),
			ParseColorComponent(span, ref pos, "b")
		);
		ProvinceToColorDict.Add(id, color);
		ColorToProvinceDict[color] = id;
	}

	private static ulong ParseProvinceId(ReadOnlySpan<char> span, ref int pos) {
		var idSpan = ReadNextField(span, ref pos);
		if (ulong.TryParse(idSpan, out var id)) {
			return id;
		}
		throw new FormatException($"Invalid id: {idSpan}");
	}

	private static byte ParseColorComponent(ReadOnlySpan<char> span, ref int pos, string componentName) {
		var componentSpan = ReadNextField(span, ref pos);
		if (byte.TryParse(componentSpan, out var component)) {
			return component;
		}
		throw new FormatException($"Invalid {componentName}: {componentSpan}");
	}

	private static ReadOnlySpan<char> ReadNextField(ReadOnlySpan<char> span, ref int pos) {
		var fieldEnd = span[pos..].IndexOf(';');
		if (fieldEnd < 0) {
			throw new FormatException("Missing separators");
		}

		var fieldSpan = span[pos..(pos + fieldEnd)];
		pos += fieldEnd + 1;
		return fieldSpan;
	}
}