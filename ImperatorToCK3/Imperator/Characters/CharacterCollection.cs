using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils.Genes;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Families;
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

	public void PurgeUnneededCharacters(Title.LandedTitles titles, DynastyCollection dynasties, HouseCollection houses, Date ck3BookmarkDate) {
		Logger.Info("Purging unneeded Imperator characters...");

		// TODO: modify this to work with the Imperator world.
		// Alive characters should be kept.
		// All rulers should be kept.
		// Families of rulers should be kept.
		
		// Characters from CK3 that hold titles at the bookmark date should be kept.
		var currentTitleHolderIds = titles.GetHolderIdsForAllTitlesExceptNobleFamilyTitles(ck3BookmarkDate);
		var landedCharacters = this
			.Where(character => currentTitleHolderIds.Contains(character.Id))
			.ToArray();
		var charactersToCheck = this.Except(landedCharacters);
		
		// Characters from I:R should be kept.
		var allTitleHolderIds = titles.GetAllHolderIds();
		var imperatorTitleHolders = this
			.Where(character => character.FromImperator && allTitleHolderIds.Contains(character.Id))
			.ToArray();
		charactersToCheck = charactersToCheck.Except(imperatorTitleHolders);

		// Keep alive Imperator characters.
		charactersToCheck = charactersToCheck
			.Where(c => c is not {FromImperator: true, ImperatorCharacter.IsDead: false});

		// Make some exceptions for characters referenced in game's script files.
		charactersToCheck = charactersToCheck
			.Where(character => !character.IsNonRemovable)
			.ToArray();

		// I:R members of landed dynasties will be preserved, unless dead and childless.
		var dynastyIdsOfLandedCharacters = landedCharacters
			.Select(character => character.GetDynastyId(ck3BookmarkDate))
			.Distinct()
			.Where(id => id is not null)
			.ToFrozenSet();

		var i = 0;
		var charactersToRemove = new List<Character>();
		var parentIdsCache = new HashSet<string>();
		do {
			Logger.Debug($"Beginning iteration {i} of characters purge...");
			charactersToRemove.Clear();
			parentIdsCache.Clear();
			++i;

			// Build cache of all parent IDs.
			foreach (var character in this) {
				var motherId = character.MotherId;
				if (motherId is not null) {
					parentIdsCache.Add(motherId);
				}

				var fatherId = character.FatherId;
				if (fatherId is not null) {
					parentIdsCache.Add(fatherId);
				}
			}

			// See who can be removed.
			foreach (var character in charactersToCheck) {
				// Is the character from Imperator and do they belong to a dynasty that holds or held titles?
				if (character.FromImperator && dynastyIdsOfLandedCharacters.Contains(character.GetDynastyId(ck3BookmarkDate))) {
					// Is the character dead and childless? Purge.
					if (!parentIdsCache.Contains(character.Id)) {
						charactersToRemove.Add(character);
					}

					continue;
				}

				charactersToRemove.Add(character);
			}

			BulkRemove(charactersToRemove.ConvertAll(c => c.Id));

			Logger.Debug($"\tPurged {charactersToRemove.Count} unneeded characters in iteration {i}.");
			charactersToCheck = charactersToCheck.Except(charactersToRemove).ToArray();
		} while (charactersToRemove.Count > 0);
		
		// TODO: modify the CK3 world's PurgeUnneededCharacters function to make all preserved I:R characters safe from purging

		// At this point we probably have many dynasties with no characters left.
		// Let's purge them.
		houses.PurgeUnneededHouses(this, ck3BookmarkDate);
		dynasties.PurgeUnneededDynasties(this, houses, ck3BookmarkDate);
		dynasties.FlattenDynastiesWithNoFounders(this, houses, ck3BookmarkDate);
	}

	public GenesDB? GenesDB { get; set; }
}