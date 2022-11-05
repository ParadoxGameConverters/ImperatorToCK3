using CommandLine;
using commonItems;
using commonItems.Localization;
using commonItems.Mods;
using DocsGenerator;
using ImperatorToCK3.CK3.Cultures;
using Parser = CommandLine.Parser;

string gameRoot;
string modPath;

Parser.Default.ParseArguments<Options>(args)
	.WithParsed(o => {
		gameRoot = o.GameRoot;
		modPath = o.ModPath;
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
		Culture.ColorFactory.AddNamedColorDict(namedColors);
		
		var locDB = new LocDB("english");
		locDB.ScrapeLocalizations(modFS);
		
		CulturesDocGenerator.GenerateCulturesTable(modPath, locDB, cultureColorUnderName);
		
		Logger.Info("Finished generating mod docs.");
	});
