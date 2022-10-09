using CommandLine;
using commonItems.Localization;
using commonItems.Mods;
using DocsGenerator;

string gameRoot;
string modPath;

Parser.Default.ParseArguments<Options>(args)
	.WithParsed(o => {
		gameRoot = o.GameRoot;
		modPath = o.ModPath;
		
		
		if (!Directory.Exists(gameRoot)) {
			Console.Error.WriteLine($"\"{gameRoot}\" is not a directory.");
			return;
		}
		if (!Directory.Exists(modPath)) {
			Console.Error.WriteLine($"\"{modPath}\" is not a directory.");
			return;
		}

		Console.WriteLine($"Generating docs for mod located in \"{modPath}\"...");

		var mod = new Mod("analyzed mod", modPath);
		var modFS = new ModFilesystem(gameRoot, new[] {mod});
		var locDB = new LocDB("english");
		locDB.ScrapeLocalizations(modFS);
		CulturesDocGenerator.GenerateCulturesTable(modPath, locDB);
	});
