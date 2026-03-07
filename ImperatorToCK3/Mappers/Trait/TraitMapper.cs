using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using JoshuaKearney.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Trait;

internal class TraitMapper {
	protected Dictionary<string, string> ImperatorToCK3TraitMap = [];
	protected IdObjectCollection<string, CK3.Characters.Trait> CK3Traits = [];
	private readonly HashSet<string> droppedImperatorTraits = [];

	public IEnumerable<string> ValidCK3TraitIDs => CK3Traits.Select(t => t.Id);

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
				foreach (var imperatorTrait in mapping.ImperatorTraits) {
					droppedImperatorTraits.Add(imperatorTrait);
				}
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
	public void LogUnmappedImperatorTraits(ModFilesystem irModFS) {
		Logger.Info("Detecting unmapped traits...");

		var unmappedTraits = new ConcurrentSet<string>();
		var traitsParser = new Parser();
		traitsParser.RegisterRegex(CommonRegexes.String, (reader, traitId) => {
			if (!ImperatorToCK3TraitMap.ContainsKey(traitId) && !droppedImperatorTraits.Contains(traitId)) {
				unmappedTraits.Add(traitId);
			}
			ParserHelpers.IgnoreItem(reader);
		});
		traitsParser.IgnoreAndLogUnregisteredItems();
		// We can parse in parallel because we don't care about the trait definitions here, just their IDs.
		traitsParser.ParseGameFolder("common/traits", irModFS, "txt", recursive: true, parallel: true);

		if (unmappedTraits.Count > 0) {
			Logger.Debug($"No mapping for I:R traits found in trait mappings: {string.Join(", ", unmappedTraits.Order())}");
		}
	}
	public string? GetCK3TraitForImperatorTrait(string impTrait) {
		return ImperatorToCK3TraitMap.TryGetValue(impTrait, out var ck3Trait) ? ck3Trait : null;
	}
	public HashSet<string> GetCK3TraitsForImperatorTraits(IEnumerable<string> irTraits) {
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
				ck3TraitsToReturn = [.. ck3TraitsToReturn.Except(ck3Trait.Opposites)];
			}
		}
		return ck3TraitsToReturn;
	}
}