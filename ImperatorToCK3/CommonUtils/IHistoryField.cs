﻿using commonItems;
using commonItems.Collections;
using System;
using System.Collections.Generic;
using ZLinq;

namespace ImperatorToCK3.CommonUtils;

internal interface IHistoryField : IIdentifiable<string> {
	public List<KeyValuePair<string, object>> InitialEntries { get; }
	public SortedDictionary<Date, List<KeyValuePair<string, object>>> DateToEntriesDict { get; }

	public object? GetValue(Date date);

	public void RemoveHistoryPastDate(Date date) {
		foreach (var item in DateToEntriesDict.AsValueEnumerable().Where(kv => kv.Key > date).ToArray()) {
			DateToEntriesDict.Remove(item.Key);
		}
	}
	public void AddEntryToHistory(Date? date, string keyword, object value);

	/// <summary>
	/// Removes all entries
	/// </summary>
	public void RemoveAllEntries() {
		RemoveAllEntries(_ => true);
	}

	/// <summary>
	/// Removes all entries with values matching the predicate
	/// </summary>
	/// <param name="predicate"></param>
	public int RemoveAllEntries(Func<object, bool> predicate) {
		int removed = 0;
		removed += InitialEntries.RemoveAll(kvp => predicate(kvp.Value));
		foreach (var datedEntriesBlock in DateToEntriesDict) {
			removed += datedEntriesBlock.Value.RemoveAll(kvp => predicate(kvp.Value));
		}
		
		return removed;
	}

	public void RegisterKeywords(Parser parser, Date date);

	public IEnumerable<KeyValuePair<string, object>> InitialEntriesForSerialization => InitialEntries;

	public IHistoryField Clone();
}
