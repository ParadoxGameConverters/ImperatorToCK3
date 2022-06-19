using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Trait;

public class TraitMapper {
	protected Dictionary<string, string> ImpToCK3TraitMap = new();
	protected IdObjectCollection<string, CK3.Characters.Trait> CK3Traits = new();

	public TraitMapper() { }
	public TraitMapper(string mappingsPath, Configuration config) {
		var traitsParser = new Parser();
		traitsParser.RegisterRegex(CommonRegexes.String, (reader, traitId) => CK3Traits.AddOrReplace(new(traitId, reader)));
		traitsParser.ParseGameFolder("common/traits", config.CK3Path, "txt", new List<Mod>(), true);

		Logger.Info("Parsing trait mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile(mappingsPath);
		Logger.Info($"Loaded {ImpToCK3TraitMap.Count} trait links.");
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => {
			var mapping = new TraitMapping(reader);
			if (mapping.CK3Trait is null) {
				return;
			}
			foreach (var imperatorTrait in mapping.ImpTraits) {
				var ck3TraitId = mapping.CK3Trait;
				if (!CK3Traits.ContainsKey(ck3TraitId)) {
					Logger.Warn($"Couldn't find definition for CK3 trait {ck3TraitId}!");
				}
				ImpToCK3TraitMap.Add(imperatorTrait, ck3TraitId);
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public string? GetCK3TraitForImperatorTrait(string impTrait) {
		return ImpToCK3TraitMap.TryGetValue(impTrait, out var ck3Trait) ? ck3Trait : null;
	}
	public ISet<string> GetCK3TraitsForImperatorTraits(IEnumerable<string> impTraits) {
		ISet<string> ck3TraitsToReturn = new HashSet<string>();
		foreach (var impTrait in impTraits) {
			var ck3Trait = GetCK3TraitForImperatorTrait(impTrait);
			if (ck3Trait is null) {
				continue;
			}
			ck3TraitsToReturn.Add(ck3Trait);
		}

		// Remove opposite traits to prevent CK3 log errors
		foreach (var ck3TraitId in ck3TraitsToReturn.ToList()) {
			if (!ck3TraitsToReturn.Contains(ck3TraitId)) {
				continue;
			}

			if (CK3Traits.TryGetValue(ck3TraitId, out var ck3Trait)) {
				ck3TraitsToReturn = ck3TraitsToReturn.Except(ck3Trait.Opposites).ToHashSet();
			}
		}
		return ck3TraitsToReturn;
	}
}