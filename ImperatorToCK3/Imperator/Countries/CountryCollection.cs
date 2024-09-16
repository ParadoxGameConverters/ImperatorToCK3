using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Families;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ImperatorToCK3.Imperator.Countries;

public sealed class CountryCollection : ConcurrentIdObjectCollection<ulong, Country> {
	public void LoadCountriesFromBloc(BufferedReader reader) {
		var blocParser = new Parser();
		blocParser.RegisterKeyword("country_database", LoadCountries);
		blocParser.IgnoreAndLogUnregisteredItems();
		blocParser.ParseStream(reader);

		Logger.Debug($"Ignored CountryCurrencies tokens: {CountryCurrencies.IgnoredTokens}");
		Logger.Debug($"Ignored RulerTerm tokens: {RulerTerm.IgnoredTokens}");
		Logger.Debug($"Ignored Country tokens: {Country.IgnoredTokens}");
	}
	public void LoadCountries(BufferedReader reader) {
		// Load countries using the producer-consumer pattern.
		
		var channel = Channel.CreateUnbounded<KeyValuePair<string, StringOfItem>>();
		var channelWriter = channel.Writer;
		var channelReader = channel.Reader;

		var producerTask = Task.Run(() => {
			var parser = new Parser();
			parser.RegisterRegex(CommonRegexes.Integer, (countryReader, countryIdStr) => {
				var countryData = countryReader.GetStringOfItem();
				
				if (!channelWriter.TryWrite(new(countryIdStr, countryData))) {
					Logger.Warn($"Failed to enqueue country {countryIdStr} for processing.");
				}
			});
			parser.IgnoreAndLogUnregisteredItems();
			parser.ParseStream(reader);
			
			channelWriter.Complete();
		});
		
		var consumerTasks = new List<Task>();
		for (var i = 0; i < 4; ++i) {
			consumerTasks.Add(Task.Run(async () => {
				await foreach (var (countryIdStr, countryData) in channelReader.ReadAllAsync()) {
					var countryReader = new BufferedReader(countryData.ToString());
					var newCountry = Country.Parse(countryReader, ulong.Parse(countryIdStr));
					Add(newCountry);
				}
			}));
		}
		
		Task.WaitAll(producerTask, Task.WhenAll(consumerTasks));

		foreach (var country in this) {
			country.LinkOriginCountry(this);
		}
	}

	public void LinkFamilies(FamilyCollection families) {
		SortedSet<ulong> idsWithoutDefinition = new();
		var counter = this.Sum(country => country.LinkFamilies(families, idsWithoutDefinition));

		if (idsWithoutDefinition.Count > 0) {
			Logger.Debug($"Families without definition: {string.Join(", ", idsWithoutDefinition)}");
		}

		Logger.Info($"{counter} families linked to countries.");
	}
}