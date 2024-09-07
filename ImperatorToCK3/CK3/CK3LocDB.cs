using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Localization;
using System.Collections.Generic;
using System.IO;

namespace ImperatorToCK3.CK3;

public class CK3LocDB : IdObjectCollection<string, CK3LocBlock> {
	public CK3LocDB() { } // For unit tests.
	
	public CK3LocDB(ModFilesystem ck3ModFS) {
		// Read loc from CK3 and selected CK3 mods.
		var modFSLocDB = new LocDB(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);
		modFSLocDB.ScrapeLocalizations(ck3ModFS);
		ImportLocFromLocDB(modFSLocDB);
		
		// Read loc from ImperatorToCK3 congifurables.
		// It will only be outputted for keys localized in neither ModFSLocDB nor ConverterGeneratedLocDB.
		LoadOptionalLoc();
	}

	private void ImportLocFromLocDB(LocDB locDB) {
		foreach (var locBlock in locDB) {
			var ck3LocBlock = GetOrCreateLocBlock(locBlock.Id);
			foreach (var (language, loc) in locBlock) {
				if (loc is null) {
					continue;
				}
				ck3LocBlock.AddModFSLoc(language, loc);
			}
		}
	}

	private void LoadOptionalLoc() {
		const string optionalLocDir = "configurables/localization";
		if (!Directory.Exists(optionalLocDir)) {
			Logger.Warn("Optional loc directory not found, skipping optional loc loading.");
			return;
		}
		
		var optionalConverterLocDB = new LocDB(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);
		var optionalLocFilePaths = Directory.GetFiles(optionalLocDir, "*.yml", SearchOption.AllDirectories);
		foreach (var outputtedLocFilePath in optionalLocFilePaths) {
			optionalConverterLocDB.ScrapeFile(outputtedLocFilePath);
		}

		foreach (var locBlock in optionalConverterLocDB) {
			// Only add loc for the languages that are not already in the CK3LocDB.
			var ck3LocBlock = GetOrCreateLocBlock(locBlock.Id);
			foreach (var (language, loc) in locBlock) {
				if (loc is null) {
					continue;
				}
				if (!ck3LocBlock.HasLocForLanguage(language)) {
					ck3LocBlock.AddOptionalLoc(language, loc);
				}
			}
		}
	}
	
	private readonly object insertionLock = new();
	
	public CK3LocBlock GetOrCreateLocBlock(string id) {
		lock (insertionLock) {
			if (TryGetValue(id, out var locBlock)) {
				return locBlock;
			}
			
			// Create new loc block.
			locBlock = new CK3LocBlock(id, ConverterGlobals.PrimaryLanguage);
			Add(locBlock);
			return locBlock;
		}
	}
	
	// TODO: add unit test for combining loc from all the sources into one locblock
	
	
	public CK3LocBlock? GetLocBlockForKey(string key) {
		if (TryGetValue(key, out var locBlock)) {
			return locBlock;
		}
		
		return null;
	}

	public bool HasKeyLocForLanguage(string key, string language) {
		if (TryGetValue(key, out var locBlock)) {
			return locBlock.HasLocForLanguage(language);
		}
		
		return false;
	}

	public void AddLocForLanguage(string key, string language, string loc) {
		lock (insertionLock) {
			var locBlock = GetOrCreateLocBlock(key);
			locBlock[language] = loc;
		}
	}

	public string? GetYmlLocLineForLanguage(string key, string language) {
		if (TryGetValue(key, out var locBlock) && locBlock.HasLocForLanguage(language)) {
			return locBlock.GetYmlLocLineForLanguage(language);
		}
		
		return null;
	}

	public List<string> GetLocLinesToOutputForLanguage(string language) {
		var locLinesToOutput = new List<string>();

		foreach (var locBlock in this) {
			if (locBlock.GetLocTypeForLanguage(language) is null or CK3LocType.CK3ModFS) {
				// If there's no loc for the language, the returned loc type is null.
				// CK3ModFS locs are already present in the CK3/mod/blankMod files, we don't need to output them.
				continue;
			}
			
			var loc = locBlock[language];
			if (loc is null) {
				continue;
			}
			
			locLinesToOutput.Add(locBlock.GetYmlLocLineForLanguage(language));
		}
		
		return locLinesToOutput;
	}
}