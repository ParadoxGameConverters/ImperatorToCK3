using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

public class LiteralHistoryField : IHistoryField {
	public string Id { get; }
	public List<KeyValuePair<string, object>> InitialEntries { get; } = new(); // every entry is a <setter, value> pair

	public SortedDictionary<Date, List<KeyValuePair<string, object>>> DateToEntriesDict { get; } = new();

	private readonly OrderedSet<string> setterKeywords;

	public LiteralHistoryField(string fieldName, OrderedSet<string> setterKeywords, object? initialValue) {
		Id = fieldName;
		this.setterKeywords = setterKeywords;
		if (initialValue is not null) {
			InitialEntries.Add(new KeyValuePair<string, object>(setterKeywords.First(), initialValue));
		}
	}

	private LiteralHistoryField(LiteralHistoryField baseField) {
		Id = baseField.Id;
		setterKeywords = new OrderedSet<string>(baseField.setterKeywords);
		InitialEntries = new List<KeyValuePair<string, object>>(baseField.InitialEntries);
		foreach (var (date, entries) in baseField.DateToEntriesDict) {
			DateToEntriesDict[date] = new List<KeyValuePair<string, object>>(entries);
		}
	}

	private KeyValuePair<string, object>? GetLastEntry(Date date) {
		var pairsWithEarlierOrSameDate = DateToEntriesDict.TakeWhile(d => d.Key <= date);

		foreach (var (_, entries) in pairsWithEarlierOrSameDate.Reverse()) {
			foreach (var entry in Enumerable.Reverse(entries)) {
				return entry;
			}
		}

		return InitialEntries.LastOrDefault();
	}
	public object? GetValue(Date date) {
		return GetLastEntry(date)?.Value;
	}

	public void AddEntryToHistory(Date? date, string setter, object value) {
		if (!setterKeywords.Contains(setter)) {
			Logger.Warn($"Setter {setter} does not belong to history field's setters!");
		}

		if (date is null) {
			InitialEntries.Add(new KeyValuePair<string, object>(setter, value));
		} else {
			if (DateToEntriesDict.TryGetValue(date, out var entriesList)) {
				entriesList.Add(new KeyValuePair<string, object>(setter, value));
			}
			else {
				DateToEntriesDict[date] = new List<KeyValuePair<string, object>> {
					new(setter, value)
				};
			}
		}
	}

	public void RegisterKeywords(Parser parser, Date date) {
		foreach (var setter in setterKeywords) {
			parser.RegisterKeyword(setter, reader => {
				var itemStr = reader.GetStringOfItem().ToString();
				AddEntryToHistory(date, setter, itemStr);
			});
		}
	}

	public IEnumerable<KeyValuePair<string, object>> InitialEntriesForSerialization => InitialEntries;

	public IHistoryField Clone() => new LiteralHistoryField(this);
}
