using commonItems;
using commonItems.Localization;
using commonItems.Mods;
using System.IO;

namespace ImperatorToCK3.CK3;

public class CK3LocDB {
	public LocDB ModFSLocDB { get; } = new LocDB(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);
	public LocDB ConverterGeneratedLocDB { get; } = new LocDB(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);
	public LocDB OptionalConverterLocDB { get; } = new LocDB(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);

	public CK3LocDB(ModFilesystem ck3ModFS) {
		// Read loc from CK3 and selected CK3 mods.
		ModFSLocDB.ScrapeLocalizations(ck3ModFS);
		
		// Read loc from ImperatorToCK3 congifurables.
		// It will only be outputted for keys localized in neither ModFSLocDB nor ConverterGeneratedLocDB.
		LoadOptionalLoc();
	}

	private void LoadOptionalLoc() {
		const string optionalLocDir = "configurables/localization";
		if (!Directory.Exists(optionalLocDir)) {
			Logger.Warn("Optional loc directory not found, skipping optional loc loading.");
			return;
		}
		
		var optionalLocFilePaths = Directory.GetFiles(optionalLocDir, "*.yml", SearchOption.AllDirectories);
		foreach (var outputtedLocFilePath in optionalLocFilePaths) {
			OptionalConverterLocDB.ScrapeFile(outputtedLocFilePath);
		}
	}
	
	public LocBlock AddLocBlock(string id) => ConverterGeneratedLocDB.AddLocBlock(id);

	public void GetNeededOptionalLocForLanguage(string language) {
		
	}
}