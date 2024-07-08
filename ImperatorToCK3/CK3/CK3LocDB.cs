using commonItems;
using commonItems.Localization;
using commonItems.Mods;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ImperatorToCK3.CK3;

public class CK3LocDB {
	public LocDB ModFSLocDB { get; } = new LocDB(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);
	public LocDB ConverterGeneratedLocDB { get; } = new LocDB(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);
	public LocDB OptionalConverterLocDB { get; } = new LocDB(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);
	
	protected CK3LocDB() { } // Only for inheritance

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
	
	public bool ContainsKey(string key) {
		if (ModFSLocDB.ContainsKey(key)) {
			return true;
		}
		if (ConverterGeneratedLocDB.ContainsKey(key)) {
			return true;
		}
		if (OptionalConverterLocDB.ContainsKey(key)) {
			return true;
		}
		return false;
	}
	
	public bool TryGetValue(string key, [MaybeNullWhen(false)] out LocBlock locBlock) {
		if (ModFSLocDB.TryGetValue(key, out locBlock)) {
			return true;
		}
		if (ConverterGeneratedLocDB.TryGetValue(key, out locBlock)) {
			return true;
		}
		if (OptionalConverterLocDB.TryGetValue(key, out locBlock)) {
			return true;
		}
		return false;
	}
	
	public LocBlock? GetLocBlockForKey(string key) {
		if (TryGetValue(key, out var locBlock)) {
			return locBlock;
		}
		
		return null;
	}

	public HashSet<string> GetAlreadyOutputtedLocKeysForLanguage(string language) {
		var keysPerLanguage = new HashSet<string>();
	
		foreach (var locBlock in ModFSLocDB) {
			if (locBlock.HasLocForLanguage(language)) {
				keysPerLanguage.Add(locBlock.Id);
			}
		}
	
		return keysPerLanguage;
	}

	public List<string> GetLocLinesToOutputForLanguage(string language) { // TODO: finish this
		// TODO: also get fallback loc for lines not licalized in the language but localized in the primary language
		var locLinesToOutput = new List<string>();
		
		var alreadyWrittenLocForLanguage = GetAlreadyOutputtedLocKeysForLanguage(language);

		foreach (var locBlock in ConverterGeneratedLocDB) {
			if (!locBlock.HasLocForLanguage(language)) {
				continue;
			}
			
			var loc = locBlock[language];
			if (loc is null) {
				continue;
			}
			
			locLinesToOutput.Add(locBlock.GetYmlLocLineForLanguage(language));
			alreadyWrittenLocForLanguage.Add(locBlock.Id);
		}

		foreach (var locBlock in OptionalConverterLocDB) {
			if (alreadyWrittenLocForLanguage.Contains(locBlock.Id)) {
				continue;
			}

			if (!locBlock.HasLocForLanguage(language)) {
				continue;
			}
			
			var loc = locBlock[language];
			if (loc is null) {
				continue;
			}
			
			locLinesToOutput.Add(locBlock.GetYmlLocLineForLanguage(language));
			alreadyWrittenLocForLanguage.Add(locBlock.Id);
		}
		
		return locLinesToOutput;
	}
}