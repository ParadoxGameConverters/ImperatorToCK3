﻿using commonItems;
using commonItems.Mods;
using System.Collections.Generic;

namespace ImperatorToCK3.Mappers.CoA;

public class CoaMapper {
	public CoaMapper() { }
	public CoaMapper(ModFilesystem imperatorModFS) {
		Logger.Info("Parsing CoAs...");
		var parser = new Parser();
		RegisterKeys(parser);
		const string coasPath = "common/coat_of_arms/coat_of_arms";
		parser.ParseGameFolder(coasPath, imperatorModFS, "txt", true);

		Logger.Info($"Loaded {coasMap.Count} coats of arms.");
		
		Logger.IncrementProgress();
	}
	public CoaMapper(string coaFilePath) {
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile(coaFilePath);
	}
	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("template", ParserHelpers.IgnoreItem); // we don't need templates, we need CoAs!
		parser.RegisterRegex(CommonRegexes.Catchall, (reader, flagName) => coasMap[flagName] = reader.GetStringOfItem().ToString());
	}

	public string? GetCoaForFlagName(string impFlagName) {
		bool contains = coasMap.TryGetValue(impFlagName, out string? value);
		return contains ? value : null;
	}

	private readonly Dictionary<string, string> coasMap = new();
}