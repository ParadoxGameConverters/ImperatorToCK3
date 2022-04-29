﻿using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

internal class DiffHistoryField : IHistoryField {
	public string Id { get; }
	public List<KeyValuePair<string, object>> InitialEntries { get; } = new();

	public SortedDictionary<Date, List<KeyValuePair<string, object>>> DateToEntriesDict { get; } = new();

	private readonly OrderedSet<string> insertKeywords;
	private readonly OrderedSet<string> removeKeywords;

	public DiffHistoryField(string fieldName, OrderedSet<string> insertKeywords, OrderedSet<string> removeKeywords) {
		Id = fieldName;
		this.insertKeywords = insertKeywords;
		this.removeKeywords = removeKeywords;
	}

	private void AddOrRemoveToValueSet(OrderedSet<object> valueSet, string keyword, object value) {
		if (insertKeywords.Contains(keyword)) {
			valueSet.Add(value);
		} else if (removeKeywords.Contains(keyword)) {
			valueSet.Remove(value);
		} else {
			Logger.Warn($"Keyword {keyword} is not an insert or remove keyword for field {Id}!");
		}
	}

	public object? GetValue(Date date) {
		var toReturn = new OrderedSet<object>();
		foreach (var (keyword, value) in InitialEntries) {
			AddOrRemoveToValueSet(toReturn, keyword, value);
		}

		foreach (var (entriesDate, entries) in DateToEntriesDict) {
			if (entriesDate > date) {
				break;
			}
			foreach (var (keyword, value) in entries) {
				AddOrRemoveToValueSet(toReturn, keyword, value);
			}
		}

		return toReturn;
	}
	
	public void AddEntryToHistory(Date date, string keyword, object value) {
		if (insertKeywords.Contains(keyword) || removeKeywords.Contains(keyword)) {
			var newEntry = new KeyValuePair<string, object>(keyword, value);
			if (DateToEntriesDict.TryGetValue(date, out var entriesList)) {
				entriesList.Add(newEntry);
			} else {
				DateToEntriesDict.Add(date, new List<KeyValuePair<string, object>> {
					newEntry
				});
			}
		} else {
			Logger.Warn($"Keyword {keyword} is not an insert or remove keyword for field {Id}!");
		}
	}
	
	public void RegisterKeywords(Parser parser, Date date) {
		foreach (var keyword in insertKeywords) {
			parser.RegisterKeyword(keyword, reader => {
				var value = HistoryFactory.GetValue(reader.GetString());
				AddEntryToHistory(date, keyword, value);
			});
		}
		foreach (var keyword in removeKeywords) {
			parser.RegisterKeyword(keyword, reader => {
				var value = HistoryFactory.GetValue(reader.GetString());
				AddEntryToHistory(date, keyword, value);
			});
		}
	}
}