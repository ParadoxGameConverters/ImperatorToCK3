using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Families;
using ImperatorToCK3.Imperator.Jobs;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ImperatorToCK3.Imperator.Characters;

internal sealed class CharacterCollection : ConcurrentIdObjectCollection<ulong, Character> {
	public void LoadCharactersFromBloc(BufferedReader reader) {
		var blocParser = new Parser();
		blocParser.RegisterKeyword("character_database", LoadCharacters);
		blocParser.IgnoreAndLogUnregisteredItems();
		blocParser.ParseStream(reader);
	}
	
	public void LoadCharacters(BufferedReader charactersReader) {
		// Load characters in a producer-consumer pattern.
		var channel = Channel.CreateUnbounded<KeyValuePair<string, StringOfItem>>();
		var channelWriter = channel.Writer;
		var channelReader = channel.Reader;
		
		var producerTask = Task.Run(() => {
			var parser = new Parser();
			parser.RegisterRegex(CommonRegexes.Integer, (reader, charIdStr) => {
				if (!channelWriter.TryWrite(new(charIdStr, reader.GetStringOfItem()))) {
					Logger.Warn($"Failed to enqueue character {charIdStr} for processing.");
				}
			});
			parser.IgnoreAndLogUnregisteredItems();
			parser.ParseStream(charactersReader);
			channelWriter.Complete();
		});
		
		var consumerTasks = new List<Task>();
		for (var i = 0; i < 10; ++i) {
			consumerTasks.Add(Task.Run(async () => {
				await foreach (var (charIdStr, characterStringOfItem) in channelReader.ReadAllAsync()) {
					var newCharacter = Character.Parse(new(characterStringOfItem.ToString()), charIdStr, GenesDB);
					AddOrReplace(newCharacter);
				}
			}));
		}
		
		Task.WaitAll(producerTask, Task.WhenAll(consumerTasks));

		Logger.Info("Linking Characters with Spouses...");
		LinkSpouses();
		Logger.Info("Linking Characters with Mothers and Fathers...");
		LinkMothersAndFathers();
	}

	public void LinkFamilies(FamilyCollection families) {
		var idsWithoutDefinition = new SortedSet<ulong>();
		var counter = this.Count(character => character.LinkFamily(families, idsWithoutDefinition));
		if (idsWithoutDefinition.Count > 0) {
			Logger.Info($"Families without definition: {string.Join(", ", idsWithoutDefinition)}");
		}

		Logger.Info($"{counter} families linked to characters.");
	}
	private void LinkSpouses() {
		var spouseCounter = this.Sum(character => character.LinkSpouses(this));
		Logger.Info($"{spouseCounter} spouses linked.");
	}

	private void LinkMothersAndFathers() {
		var motherCounter = 0;
		var fatherCounter = 0;
		foreach (var character in this) {
			if (character.LinkMother(this)) {
				++motherCounter;
			}
			if (character.LinkFather(this)) {
				++fatherCounter;
			}
		}
		Logger.Info($"{motherCounter} mothers and {fatherCounter} fathers linked.");
	}

	public void LinkCountries(CountryCollection countries) {
		var counter = this.Count(character => character.LinkCountry(countries));
		Logger.Info($"{counter} countries linked to characters.");

		counter = this.Count(character => character.LinkHomeCountry(countries));
		Logger.Info($"{counter} home countries linked to characters.");

		counter = this.Count(character => character.LinkPrisonerHome(countries));
		Logger.Info($"{counter} prisoner homes linked to characters.");
	}

	public void PurgeUnneededCharacters(CountryCollection countries, List<Governorship> governorships, FamilyCollection families) {
		Logger.Info("Purging unneeded Imperator characters...");

		// Alive characters should be kept.
		var charactersToCheck = this
			.Where(character => character.IsDead);

		// All landed characters should be kept.
		var allRulerIds = countries
			.SelectMany(country => country.RulerTerms.Select(term => term.CharacterId))
			.Where(id => id is not null)
			.Cast<ulong>();
		var allGovernorIds = governorships.Select(g => g.CharacterId);
		var landedCharacterIds = allRulerIds.Concat(allGovernorIds).ToFrozenSet();
		charactersToCheck = charactersToCheck
			.Where(character => !landedCharacterIds.Contains(character.Id))
			.ToArray();

		// Members of rulers' families should be kept, unless dead and childless.
		var familyIdsOfLandedCharacters = this
			.Where(character => landedCharacterIds.Contains(character.Id))
			.Select(character => character.Family?.Id)
			.Distinct()
			.Where(id => id is not null)
			.Cast<ulong>()
			.ToFrozenSet();

		var i = 0;
		var charactersToRemove = new List<Character>();
		var parentIdsCache = new HashSet<ulong>();
		do {
			Logger.Debug($"Beginning iteration {i} of characters purge...");
			charactersToRemove.Clear();
			parentIdsCache.Clear();
			++i;

			// Build cache of all parent IDs.
			foreach (var character in this) {
				ulong? motherId = character.Mother?.Id;
				if (motherId is not null) {
					parentIdsCache.Add(motherId.Value);
				}

				ulong? fatherId = character.Father?.Id;
				if (fatherId is not null) {
					parentIdsCache.Add(fatherId.Value);
				}
			}

			// See who can be removed.
			foreach (var character in charactersToCheck) {
				// Does the character belong to a landed family?
				if (character.Family?.Id is ulong familyId && familyIdsOfLandedCharacters.Contains(familyId)) {
					// Is the dead character childless? Purge.
					if (!parentIdsCache.Contains(character.Id)) {
						charactersToRemove.Add(character);
					}

					continue;
				}

				charactersToRemove.Add(character);
			}

			BulkRemove(charactersToRemove.ConvertAll(c => c.Id));

			Logger.Debug($"\tPurged {charactersToRemove.Count} unneeded Imperator characters in iteration {i}.");
			charactersToCheck = charactersToCheck.Except(charactersToRemove).ToArray();
		} while (charactersToRemove.Count > 0);
		
		// At this point we may have families with no characters left.
		// Let's purge them.
		families.PurgeUnneededFamilies(this);
	}
	
	private void BulkRemove(List<ulong> ids) {
		// Remove parent/child/spouse references to the characters to be removed.
		foreach (var character in this) {
			if (character.Mother is not null && ids.Contains(character.Mother.Id)) {
				character.Mother = null;
			}
			if (character.Father is not null && ids.Contains(character.Father.Id)) {
				character.Father = null;
			}
			character.Children.RemoveWhere(child => ids.Contains(child.Key));
			character.Spouses.RemoveWhere(spouse => ids.Contains(spouse.Key));
		}
		
		foreach (var id in ids) {
			Remove(id);
		}
	}

	public GenesDB? GenesDB { get; set; }
}