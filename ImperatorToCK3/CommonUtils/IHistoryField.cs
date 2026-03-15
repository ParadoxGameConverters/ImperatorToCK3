using commonItems;
using commonItems.Collections;
using System;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils;

internal interface IHistoryField : IIdentifiable<string> {
	internal List<KeyValuePair<string, object>> InitialEntries { get; }
	internal SortedDictionary<Date, List<KeyValuePair<string, object>>> DateToEntriesDict { get; }

	internal object? GetValue(Date? date);
	internal KeyValuePair<Date?, object?> GetLastEntryWithDate(Date? date) {
		if (date is not null) {
			Date? lastDate = null;
			List<KeyValuePair<string, object>>? latestEntries = null;
			foreach (var datedEntries in DateToEntriesDict) {
				if (datedEntries.Key > date.Value) {
					break;
				}

				lastDate = datedEntries.Key;
				latestEntries = datedEntries.Value;
			}

			if (latestEntries is { Count: > 0 }) {
				return new(lastDate, latestEntries[^1].Value);
			}
		}

		return InitialEntries.Count > 0
			? new(key: null, InitialEntries[^1])
			: new KeyValuePair<Date?, object?>(key: null, value: null);
	}

	internal void RemoveHistoryPastDate(Date date) {
		var keysToRemove = new List<Date>();
		foreach (var key in DateToEntriesDict.Keys) {
			if (key > date) {
				keysToRemove.Add(key);
			}
		}

		foreach (var key in keysToRemove) {
			DateToEntriesDict.Remove(key);
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
