using commonItems;
using commonItems.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using ZLinq;

namespace ImperatorToCK3.CommonUtils;

internal interface IHistoryField : IIdentifiable<string> {
	internal List<KeyValuePair<string, object>> InitialEntries { get; }
	internal SortedDictionary<Date, List<KeyValuePair<string, object>>> DateToEntriesDict { get; }

	internal object? GetValue(Date? date);
	internal KeyValuePair<Date?, object?> GetLastEntryWithDate(Date? date) { // TODO: add tests for this
		if (date is not null) {
			var pairsWithEarlierOrSameDate = DateToEntriesDict.TakeWhile(d => d.Key <= date);

			foreach (var (d, entries) in pairsWithEarlierOrSameDate.Reverse()) {
				foreach (var entry in Enumerable.Reverse(entries)) {
					return new(d, entry.Value);
				}
			}
		}

		var lastInitialEntry = InitialEntries.LastOrNull();
		return lastInitialEntry is not null
			? new(key: null, lastInitialEntry.Value)
			: new KeyValuePair<Date?, object?>(key: null, value: null);
	}

	internal void RemoveHistoryPastDate(Date date) {
		foreach (var item in DateToEntriesDict.AsValueEnumerable().Where(kv => kv.Key > date).ToArray()) {
			DateToEntriesDict.Remove(item.Key);
		}
	}
	internal void AddEntryToHistory(Date? date, string keyword, object value);

	/// <summary>
	/// Removes all entries
	/// </summary>
	internal void RemoveAllEntries() {
		RemoveAllEntries(_ => true);
	}

	/// <summary>
	/// Removes all entries with values matching the predicate
	/// </summary>
	/// <param name="predicate"></param>
	internal int RemoveAllEntries(Func<object, bool> predicate) {
		int removed = 0;
		removed += InitialEntries.RemoveAll(kvp => predicate(kvp.Value));
		foreach (var datedEntriesBlock in DateToEntriesDict) {
			removed += datedEntriesBlock.Value.RemoveAll(kvp => predicate(kvp.Value));
		}

		return removed;
	}

	internal void RegisterKeywords(Parser parser, Date date);

	internal IEnumerable<KeyValuePair<string, object>> InitialEntriesForSerialization => InitialEntries;

	internal IHistoryField Clone();
}
