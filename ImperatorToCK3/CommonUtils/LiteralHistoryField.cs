using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImperatorToCK3.CommonUtils;

internal sealed class LiteralHistoryField : IHistoryField {
	public string Id { get; }
	public List<KeyValuePair<string, object>> InitialEntries { get; } = []; // every entry is a <setter, value> pair

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
			if (DateToEntriesDict.TryGetValue(date.Value, out var entriesList)) {
				entriesList.Add(new KeyValuePair<string, object>(setter, value));
			}
			else {
				DateToEntriesDict[date.Value] = [new(setter, value)];
			}
		}
	}

	public int EntriesCount => InitialEntries.Count + DateToEntriesDict.Sum(pair => pair.Value.Count);

	public void RegexReplaceAllEntries(Regex regex, string replacement) {
		for (var i = 0; i < InitialEntries.Count; ++i) {
			var entry = InitialEntries[i];
			if (entry.Value is string str) {
				InitialEntries[i] = new(entry.Key, regex.Replace(str, replacement));
			}
		}

		foreach (var (_, entries) in DateToEntriesDict) {
			for (var i = 0; i < entries.Count; ++i) {
				var entry = entries[i];
				if (entry.Value is string str) {
					entries[i] = new(entry.Key, regex.Replace(str, string.Empty));
				}
			}
		}
	}

	public void RegisterKeywords(Parser parser, Date date) {
		foreach (var setter in setterKeywords) {
			parser.RegisterKeyword(setter, reader => {
				var itemStr = reader.GetStringOfItem();
				// If itemStr is the question sign from the "?=" operator, get another string.
				if (itemStr.ToString() == "?") {
					itemStr = reader.GetStringOfItem();
				}
				AddEntryToHistory(date, setter, itemStr);
			});
		}
	}

	public IEnumerable<KeyValuePair<string, object>> InitialEntriesForSerialization => InitialEntries;

	public IHistoryField Clone() => new LiteralHistoryField(this);
}
