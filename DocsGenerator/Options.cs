using CommandLine;

namespace DocsGenerator; 

public class Options {
	[Option('r', "gameRoot", Required = true,
		HelpText = "CK3 game root path, for example \"C:/SteamLibrary/steamapps/common/Crusader Kings III/game\".")]
	public string GameRoot { get; set; } = Directory.GetCurrentDirectory();
	
	[Option('m', "modPath", Required = true,
		HelpText = "Path to mod directory, for example \"C:/Users/User/Documents/Paradox Interactive/Crusader Kings III/mod/test_mod\"")]
	public string ModPath { get; set; } = Directory.GetCurrentDirectory();
}