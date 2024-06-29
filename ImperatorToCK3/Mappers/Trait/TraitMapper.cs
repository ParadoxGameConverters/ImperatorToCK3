using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Trait;

public class TraitMapper {
	protected IDictionary<string, string> ImperatorToCK3TraitMap = new Dictionary<string, string>();
	protected IdObjectCollection<string, CK3.Characters.Trait> CK3Traits = [];

	public TraitMapper() { }
	public TraitMapper(string mappingsPath, ModFilesystem ck3ModFS) {
		var traitsParser = new Parser();
		traitsParser.RegisterRegex(CommonRegexes.String, (reader, traitId) => CK3Traits.AddOrReplace(new(traitId, reader)));
		traitsParser.ParseGameFolder("common/traits", ck3ModFS, "txt", recursive: true);

		Logger.Info("Parsing trait mappings...");
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseFile(mappingsPath);
		Logger.Info($"Loaded {ImperatorToCK3TraitMap.Count} trait links.");

		Logger.IncrementProgress();
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("link", reader => {
			var mapping = new TraitMapping(reader);
			if (mapping.CK3Trait is null) {
				return;
			}
			foreach (var imperatorTrait in mapping.ImperatorTraits) {
				var ck3TraitId = mapping.CK3Trait;
				if (!CK3Traits.ContainsKey(ck3TraitId)) {
					Logger.Warn($"Couldn't find definition for CK3 trait {ck3TraitId}!");
				}
				ImperatorToCK3TraitMap.Add(imperatorTrait, ck3TraitId);
			}
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
	}
	public string? GetCK3TraitForImperatorTrait(string impTrait) {
		return ImperatorToCK3TraitMap.TryGetValue(impTrait, out var ck3Trait) ? ck3Trait : null;
	}
	public ISet<string> GetCK3TraitsForImperatorTraits(IEnumerable<string> irTraits) {
		HashSet<string> ck3TraitsToReturn = [];
		foreach (var irTrait in irTraits) {
			var ck3Trait = GetCK3TraitForImperatorTrait(irTrait);
			if (ck3Trait is null) {
				continue;
			}
			ck3TraitsToReturn.Add(ck3Trait);
		}

		// Remove opposite traits to prevent CK3 log errors
		foreach (var ck3TraitId in ck3TraitsToReturn.ToArray()) {
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