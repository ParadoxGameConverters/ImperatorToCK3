using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CommonUtils;

public sealed class History : IPDXSerializable {
	[NonSerialized] public IdObjectCollection<string, IHistoryField> Fields { get; } = new(); // fieldName, field
	[NonSerialized] public IgnoredKeywordsSet IgnoredKeywords { get; } = new();

	public History() { }

	public History(History baseHistory) {
		foreach (var field in baseHistory.Fields) {
			Fields.AddOrReplace(field.Clone());
		}
	}
	public History(IdObjectCollection<string, IHistoryField> fields) {
		Fields = fields;
	}

	public object? GetFieldValue(string fieldName, Date date) {
		return Fields.TryGetValue(fieldName, out var value) ? value.GetValue(date) : null;
	}

	public OrderedSet<object>? GetFieldValueAsCollection(string fieldName, Date date) {
		return GetFieldValue(fieldName, date) as OrderedSet<object>;
	}

	public void AddFieldValue(Date? date, string fieldName, string setter, object value) {
		if (Fields.TryGetValue(fieldName, out var field)) {
			field.AddEntryToHistory(date, setter, value);
		} else {
			var newField = new SimpleHistoryField(fieldName, new OrderedSet<string>{setter}, null);
			newField.AddEntryToHistory(date, setter, value);
			Fields.Add(newField);
		}
	}

	public string Serialize(string indent, bool withBraces) {
		var sb = new StringBuilder();
		foreach (IHistoryField field in Fields) {
			var serializableEntries = field.InitialEntriesForSerialization;
			foreach (var entry in serializableEntries) {
				if (entry.Value is IEnumerable<object> enumerable && !enumerable.Any()) {
					// don't serialize empty lists
					continue;
				}
				sb.Append(indent).AppendLine(PDXSerializer.Serialize(entry));
			}
		}

		var entriesByDate = new SortedDictionary<Date, List<KeyValuePair<string, object>>>(); // <date, list<effect name, value>>
		foreach (var field in Fields) {
			foreach (var (date, entries) in field.DateToEntriesDict) {
				if (entries.Count == 0) {
					continue;
				}

				if (entriesByDate.TryGetValue(date, out var listForDate)) {
					listForDate.AddRange(entries);
				} else {
					var entryList = new List<KeyValuePair<string, object>>();
					entryList.AddRange(entries);
					entriesByDate[date] = entryList;
				}
			}
		}
		if (entriesByDate.Any()) {
			sb.Append(indent).AppendLine(PDXSerializer.Serialize(entriesByDate, indent, false));
		}

		return sb.ToString();
	}

	public void RemoveHistoryPastDate(Date date) {
		foreach (var field in Fields) {
			field.RemoveHistoryPastDate(date);
		}
	}
}
