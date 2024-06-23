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

public class CharacterCollection : ConcurrentIdObjectCollection<ulong, Character> {
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

	public GenesDB? GenesDB { get; set; }
}