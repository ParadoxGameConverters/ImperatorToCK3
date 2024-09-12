using commonItems.Collections;
using commonItems.Localization;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Localization;

public class CK3LocBlock : IIdentifiable<string> { // TODO: add ILocBlock interface that both this and commonItems' LocBlock would implement.
	private readonly string baseLanguage;
	private readonly ConcurrentDictionary<string, (string, CK3LocType)> localizations = new();
	
	public string Id { get; }
	
	public CK3LocBlock(string locKey, string baseLanguage) {
		Id = locKey;
		this.baseLanguage = baseLanguage;
	}

	public CK3LocBlock(string locKey, string baseLanguage, LocBlock otherBlock) : this(locKey, baseLanguage) {
		foreach (var (language, loc) in otherBlock) {
			if (loc is null) {
				continue;
			}
			localizations[language] = (loc, CK3LocType.ConverterGenerated);
		}
	}

	public void CopyFrom(LocBlock otherBlock) {
		foreach (var (language, loc) in otherBlock) {
			if (loc is null) {
				continue;
			}
			localizations[language] = (loc, CK3LocType.ConverterGenerated);
		}
	}
	
	public void CopyFrom(CK3LocBlock otherBlock) {
		foreach (var (language, loc) in otherBlock.localizations) {
			localizations[language] = loc;
		}
	}
	
	public bool HasLocForLanguage(string language) {
		return localizations.ContainsKey(language);
	}

	public string? this[string language] {
		get {
			if (localizations.TryGetValue(language, out var toReturn)) {
				return toReturn.Item1;
			}
			
			// As fallback, try to use base language loc.
			if (language != baseLanguage) {
				if (localizations.TryGetValue(baseLanguage, out var baseLoc)) {
					return baseLoc.Item1;
				}
			}
			return null;
		}
		set {
			if (value is null) {
				localizations.Remove(language, out _);
			} else {
				localizations[language] = (value, CK3LocType.ConverterGenerated);
			}
		}
	}

	/// <summary>
	/// Helps remove boilerplate by applying modifyingMethod to every language in the struct
	/// For example:
	/// <code>
	/// nameLocBlock["english"] = nameLocBlock["english"].Replace("$ADJ$", baseAdjLocBlock["english"]);
	/// nameLocBlock["french"] = nameLocBlock["french"].Replace("$ADJ$", baseAdjLocBlock["french"]);
	/// nameLocBlock["german"] = nameLocBlock["german"].Replace("$ADJ$", baseAdjLocBlock["german"]);
	/// nameLocBlock["russian"] = nameLocBlock["russian"].Replace("$ADJ$", baseAdjLocBlock["russian"]);
	/// nameLocBlock["simp_chinese"] = nameLocBlock["simp_chinese"].Replace("$ADJ$", baseAdjLocBlock["simp_chinese"]);
	/// nameLocBlock["spanish"] = nameLocBlock["spanish"].Replace("$ADJ$", baseAdjLocBlock["spanish"]);
	/// </code>
	/// 
	/// Can be replaced by:
	/// <code>
	/// nameLocBlock.ModifyForEveryLanguage(baseAdjLocBlock, (string baseLoc, string modifyingLoc) => {
	/// 	return baseLoc.Replace("$ADJ$", modifyingLoc);
	/// });
	/// </code>
	/// </summary>
	/// <param name="otherBlock"></param>
	/// <param name="modifyingFunction"></param>
	public void ModifyForEveryLanguage(LocBlock otherBlock, TwoArgLocDelegate modifyingFunction) {
		var otherBlockAsCK3LocBlock = new CK3LocBlock(otherBlock.Id, baseLanguage, otherBlock);
		ModifyForEveryLanguage(otherBlockAsCK3LocBlock, modifyingFunction);
	}

	public void ModifyForEveryLanguage(CK3LocBlock otherBlock, TwoArgLocDelegate modifyingFunction) {
		foreach (var language in localizations.Keys) {
			var locValue = modifyingFunction(localizations[language].Item1, otherBlock[language], language);
			if (locValue is null) {
				localizations.Remove(language, out _);
				continue;
			}
			localizations[language] = (locValue, CK3LocType.ConverterGenerated);
		}
		if (!localizations.ContainsKey(baseLanguage)) {
			var locValue = modifyingFunction(null, otherBlock[baseLanguage], baseLanguage);
			if (locValue is not null) {
				localizations[baseLanguage] = (locValue, CK3LocType.ConverterGenerated);
			}
		}
	}
	public void ModifyForEveryLanguage(LocDelegate modifyingFunction) {
		foreach (var language in localizations.Keys) {
			var locValue = modifyingFunction(localizations[language].Item1, language);
			if (locValue is null) {
				localizations.Remove(language, out _);
				continue;
			}
			localizations[language] = (locValue, CK3LocType.ConverterGenerated);
		}
		if (!localizations.ContainsKey(baseLanguage)) {
			var locValue = modifyingFunction(null, baseLanguage);
			if (locValue is not null) {
				localizations[baseLanguage] = (locValue, CK3LocType.ConverterGenerated);
			}
		}
	}

	public string GetYmlLocLineForLanguage(string language) {
		return $" {Id}: \"{this[language]}\"";
	}
	
	public CK3LocType? GetLocTypeForLanguage(string language) {
		if (localizations.TryGetValue(language, out var loc)) {
			return loc.Item2;
		}
		return null;
	}
	
	public void AddModFSLoc(string language, string loc) {
		localizations[language] = (loc, CK3LocType.CK3ModFS);
	}
	public void AddOptionalLoc(string language, string loc) {
		localizations[language] = (loc, CK3LocType.Optional);
	}
}