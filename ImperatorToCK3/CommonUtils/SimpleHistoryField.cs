using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

internal sealed class SimpleHistoryField : IHistoryField {
	public string Id { get; }

	private List<KeyValuePair<string, object>>? initialEntries; // every entry is a <setter, value> pair
	// Lazily allocated: most fields never get entries, and one history object
	// is created per character/province/title during conversion.
	public List<KeyValuePair<string, object>> InitialEntries => initialEntries ??= [];

	private SortedDictionary<Date, List<KeyValuePair<string, object>>>? dateToEntriesDict;
	public SortedDictionary<Date, List<KeyValuePair<string, object>>> DateToEntriesDict => dateToEntriesDict ??= [];

	private readonly OrderedSet<string> setterKeywords;

	public SimpleHistoryField(string fieldName, OrderedSet<string> setterKeywords, object? initialValue) {
		Id = fieldName;
		this.setterKeywords = setterKeywords;
		if (initialValue is not null) {
			InitialEntries.Add(new KeyValuePair<string, object>(setterKeywords.First(), initialValue));
		}
	}

	private SimpleHistoryField(SimpleHistoryField baseField) {
		Id = baseField.Id;
		setterKeywords = new OrderedSet<string>(baseField.setterKeywords);
		// Copy from the backing fields (not the lazy properties) so cloning
		// an empty field allocates nothing on either instance.
		if (baseField.initialEntries is { Count: > 0 } sourceInitialEntries) {
			initialEntries = new List<KeyValuePair<string, object>>(sourceInitialEntries);
		}
		if (baseField.dateToEntriesDict is { Count: > 0 } sourceDatedEntries) {
			dateToEntriesDict = new SortedDictionary<Date, List<KeyValuePair<string, object>>>();
			foreach (var (date, entries) in sourceDatedEntries) {
				dateToEntriesDict[date] = new List<KeyValuePair<string, object>>(entries);
			}
		}
	}

	private KeyValuePair<string, object>? GetLastEntry(Date? date) {
		if (date is not null) {
			List<KeyValuePair<string, object>>? latestEntries = null;
			foreach (var datedEntries in DateToEntriesDict) {
				if (datedEntries.Key > date.Value) {
					break;
				}

				latestEntries = datedEntries.Value;
			}

			if (latestEntries is { Count: > 0 }) {
				return latestEntries[^1];
			}
		}

		return InitialEntries.Count > 0 ? InitialEntries[^1] : null;
	}
	public object? GetValue(Date? date) {
		return GetLastEntry(date)?.Value;
	}

	public void AddEntryToHistory(Date? date, string setter, object value) {
		if (!setterKeywords.Contains(setter)) {
			Logger.Warn($"Setter {setter} does not belong to history field's setters!");
		}

		if (date is null) {
			InitialEntries.Add(new KeyValuePair<string, object>(setter, value));
		} else {
			DateToEntriesDict[date.Value] = [
				new(setter, value),
			];
		}
	}

	public void RegisterKeywords(Parser parser, Date date) {
		foreach (var setter in setterKeywords) {
			parser.RegisterKeyword(setter, reader => {
				var itemStr = reader.GetStringOfItem().ToString();
				// If itemStr is the question sign from the "?=" operator, get another string.
				if (itemStr == "?") {
					itemStr = reader.GetStringOfItem().ToString();
				}
				var value = HistoryFactory.GetValue(itemStr);
				AddEntryToHistory(date, setter, value);
			});
		}
	}

	public IEnumerable<KeyValuePair<string, object>> InitialEntriesForSerialization => InitialEntries.TakeLast(1);

	public IHistoryField Clone() => new SimpleHistoryField(this);
}
