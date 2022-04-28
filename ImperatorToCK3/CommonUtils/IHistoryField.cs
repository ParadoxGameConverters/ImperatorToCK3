using commonItems;
using commonItems.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

public interface IHistoryField : IIdentifiable<string> {
	public List<KeyValuePair<string, object>> InitialEntries { get; }
	public SortedDictionary<Date, List<KeyValuePair<string, object>>> DateToEntriesDict { get; }
	public object? GetValue(Date date);
	public void RemoveHistoryPastDate(Date date);
	public void AddEntryToHistory(Date date, string keyword, object value);

	/// <summary>
	/// Removes all entries with values matching the predicate
	/// </summary>
	/// <param name="predicate"></param>
	public void RemoveAll(Func<object, bool> predicate) {
		InitialEntries.RemoveAll(kv => predicate(kv.Value));
		foreach (var datedEntriesBlock in DateToEntriesDict) {
			datedEntriesBlock.Value.RemoveAll(kv => predicate(kv.Value));
		}
	}

	public void RegisterKeywords(Parser parser, Date date);
}
