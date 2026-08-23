using CommandLine;
using commonItems;
using commonItems.Colors;
using commonItems.Localization;
using commonItems.Mods;
using DocsGenerator;
using System.Collections.Generic;
using Parser = CommandLine.Parser;

Parser.Default.ParseArguments<Options>(args)
	.WithParsed(o => {
		var gameRoot = o.GameRoot;
		var modPath = o.ModPath;
		bool cultureColorUnderName = o.CultureColorUnderName;

		if (!Directory.Exists(gameRoot)) {
			Logger.Error($"\"{gameRoot}\" is not a directory.");
			return;
		}
		if (!Directory.Exists(modPath)) {
			Logger.Error($"\"{modPath}\" is not a directory.");
			return;
		}

		Logger.Info($"Generating docs for mod located in \"{modPath}\"...");
		Directory.CreateDirectory("generated_docs");

		var mod = new Mod("analyzed mod", modPath);
		var modFS = new ModFilesystem(gameRoot, new[] {mod});

		var namedColors = new NamedColorCollection();
		namedColors.LoadNamedColors("common/named_colors", modFS);
		var colorFactory = new ColorFactory();
		colorFactory.AddNamedColorDict(namedColors);

		var locDB = new LocDB("english");
		locDB.ScrapeLocalizations(modFS);

		// The analyzed mod is treated as a standalone mod for vanilla CK3.
		var ck3ModFlags = new OrderedDictionary<string, bool> {
			["tfe"] = false,
			["wtwsms"] = false,
			["roa"] = false,
			["aep"] = false,
			["confed_league"] = false,
			["vanilla_ck3"] = true,
		};

		CulturesDocGenerator.GenerateCulturesTable(modFS, colorFactory, locDB, ck3ModFlags, cultureColorUnderName);

		Logger.Info("Finished generating mod docs.");
	});
