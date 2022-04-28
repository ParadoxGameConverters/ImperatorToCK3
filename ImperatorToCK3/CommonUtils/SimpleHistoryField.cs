using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

public class SimpleHistoryField : IHistoryField {
	public string Id { get; }
	public List<KeyValuePair<string, object>> InitialEntries { get; } = new(); // every entry is a <setter, value> pair

	public SortedDictionary<Date, List<KeyValuePair<string, object>>> DateToEntriesDict { get; } = new();
	
	private OrderedSet<string> setterKeywords;
	
	public SimpleHistoryField(string fieldName, OrderedSet<string> setterKeywords, object? initialValue) {
		Id = fieldName;
		this.setterKeywords = setterKeywords;
		if (initialValue is not null) {
			InitialEntries.Add(new KeyValuePair<string, object>(setterKeywords.First(), initialValue));
		}
	}
	private KeyValuePair<string, object>? GetLastEntry(Date date) {
		var pairsWithEarlierOrSameDate = DateToEntriesDict.TakeWhile(d => d.Key <= date);
		var pairList = pairsWithEarlierOrSameDate.ToList();
		return pairList.Count > 0 ? pairList.Last().Value.Last() : InitialEntries.LastOrDefault();
	}
	public object? GetValue(Date date) {
		return GetLastEntry(date)?.Value;
	}

	public void AddEntryToHistory(Date date, string setter, object value) {
		if (!setterKeywords.Contains(setter)) {
			Logger.Warn($"Setter {setter} does not belong to history field's setters!");
		}

		var newEntry = new KeyValuePair<string, object>(setter, value);
		if (DateToEntriesDict.TryGetValue(date, out var entriesList)) {
			entriesList.Add(newEntry);
		} else {
			DateToEntriesDict.Add(date, new List<KeyValuePair<string, object>> {
				newEntry
			});
		}
	}

	public void RemoveHistoryPastDate(Date date) {
		foreach (var item in DateToEntriesDict.Where(kv => kv.Key > date)) {
			DateToEntriesDict.Remove(item.Key);
		}
	}

	public void RegisterKeywords(Parser parser, Date date) {
		foreach (var setter in setterKeywords) {
			parser.RegisterKeyword(setter, reader => {
				var value = HistoryFactory.GetValue(reader.GetString());
				AddEntryToHistory(date, setter, value);
			});
		}
	}
}
