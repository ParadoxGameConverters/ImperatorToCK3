using commonItems;
using commonItems.Collections;
using commonItems.Localization;
using commonItems.Mods;
using ImperatorToCK3.CK3.Localization;
using MurmurHash.Net;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using ZLinq;

namespace ImperatorToCK3.CK3;

internal class CK3LocDB : ConcurrentIdObjectCollection<string, CK3LocBlock> {
	public CK3LocDB() { }
	
	public CK3LocDB(ModFilesystem ck3ModFS, IEnumerable<string> activeModFlags) {
		LoadLocFromModFS(ck3ModFS, activeModFlags);
	}
	
	public void LoadLocFromModFS(ModFilesystem ck3ModFS, IEnumerable<string> activeModFlags) {
		// Read loc from CK3 and selected CK3 mods.
		var modFSLocDB = new LocDB(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);
		modFSLocDB.ScrapeLocalizations(ck3ModFS);
		ImportLocFromLocDB(modFSLocDB);
		
		// Read loc from ImperatorToCK3 configurables.
		// It will only be outputted for keys localized in neither ModFSLocDB nor ConverterGeneratedLocDB.
		LoadOptionalLoc(activeModFlags);
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

	private void LoadOptionalLoc(IEnumerable<string> activeModFlags) {
		const string optionalLocDir = "configurables/localization";
		if (!Directory.Exists(optionalLocDir)) {
			Logger.Warn("Optional loc directory not found, skipping optional loc loading.");
			return;
		}
		
		string baseLocDir = Path.Combine(optionalLocDir, "base");
		var optionalLocFilePaths = Directory.GetFiles(baseLocDir, "*.yml", SearchOption.AllDirectories);
		foreach (var modFlag in activeModFlags) {
			string modLocDir = Path.Combine(optionalLocDir, modFlag);
			if (!Directory.Exists(modLocDir)) {
				continue;
			}
			optionalLocFilePaths = optionalLocFilePaths.AsValueEnumerable()
				.Concat(Directory.GetFiles(modLocDir, "*.yml", SearchOption.AllDirectories)).ToArray();
		}
		
		var optionalConverterLocDB = new LocDB(ConverterGlobals.PrimaryLanguage, ConverterGlobals.SecondaryLanguages);
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
	
	private readonly System.Threading.Lock insertionLock = new();
	
	public CK3LocBlock GetOrCreateLocBlock(string id) {
		lock (insertionLock) {
			if (TryGetValue(id, out var locBlock)) {
				return locBlock;
			}

			// Check for hash collision.
			var hash = GetHashStrForKey(id);
			if (hashToKeyDict.TryGetValue(hash, out var existingKey)) {
				Logger.Warn($"Hash collision detected for loc key: {id}. Existing key: {existingKey}");
			} else {
				hashToKeyDict[hash] = id;
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
	
	public bool KeyHasConflictingHash(string key) {
		return hashToKeyDict.ContainsKey(GetHashStrForKey(key));
	}

    private static uint GetHashStrForKey(string key) {
		// Encode key into rented buffer to avoid allocating a dedicated byte[] for every key.
		var enc = System.Text.Encoding.UTF8;
		var pool = ArrayPool<byte>.Shared;
		int maxBytes = enc.GetMaxByteCount(key.Length);
		byte[]? rented = pool.Rent(maxBytes);
		try {
			int bytesWritten = enc.GetBytes(key, 0, key.Length, rented, 0);
			ReadOnlySpan<byte> bytes = rented.AsSpan(0, bytesWritten);
			return MurmurHash3.Hash32(bytes, seed: 0);
		} finally {
			if (rented is not null) pool.Return(rented);
		}
	}

	private readonly Dictionary<uint, string> hashToKeyDict = new(); // stores Murmur32 hash to key mapping
}