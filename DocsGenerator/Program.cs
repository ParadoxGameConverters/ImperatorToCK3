using DocsGenerator;

var modPath = args[0];
if (!Directory.Exists(modPath)) {
	Console.Error.WriteLine($"\"{modPath}\" is not a directory.");
	return;
}

Console.WriteLine($"Generating docs for \"{modPath}\"...");

CulturesDocGenerator.GenerateCulturesTable(modPath);