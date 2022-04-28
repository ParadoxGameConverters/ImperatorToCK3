using commonItems;
using commonItems.Collections;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CommonUtils;

public class History : IPDXSerializable {
	[NonSerialized] public Dictionary<string, IHistoryField> Fields { get; } = new(); // fieldName, field
	[NonSerialized] public OrderedSet<string> IgnoredKeywords { get; } = new();

	public History() { }
	public History(Dictionary<string, IHistoryField> fields) {
		Fields = fields;
	}

	public object? GetFieldValue(string fieldName, Date date) {
		return Fields.TryGetValue(fieldName, out var value) ? value.GetValue(date) : null;
	}
	
	public void AddFieldValue(Date date, string fieldName, string setter, object value) {
		if (Fields.TryGetValue(fieldName, out var field)) {
			field.AddEntryToHistory(date, setter, value);
		} else {
			var newField = new SimpleHistoryField(fieldName, new OrderedSet<string>(){setter}, null);
			newField.AddEntryToHistory(date, setter, value);
			Fields.Add(fieldName, newField);
		}
	}

	public string Serialize(string indent, bool withBraces) {
		var sb = new StringBuilder();
		foreach (IHistoryField field in Fields.Values.Where(f => f.InitialEntries.Count > 0)) {
			foreach (var entry in field.InitialEntries) {
				sb.AppendLine(PDXSerializer.Serialize(entry, indent));
			}
		}

		var entriesByDate = new SortedDictionary<Date, List<KeyValuePair<string, object>>>(); // <date, list<effect name, value>>
		foreach (var field in Fields.Values) {
			foreach (var (date, entries) in field.DateToEntriesDict) {
				if (entriesByDate.TryGetValue(date, out var listForDate)) {
					listForDate.AddRange(entries);
				} else {
					var entryList = new List<KeyValuePair<string, object>>();
					entryList.AddRange(entries);
					entriesByDate[date] = entryList;
				}
			}
		}

		foreach (var (date, fieldEntries) in entriesByDate) {
			sb.Append(indent).Append(date).AppendLine("={");
			foreach (var entry in fieldEntries) {
				sb.Append(indent).Append('\t').Append(entry.Key)
					.Append('=')
					.AppendLine(PDXSerializer.Serialize(entry.Value ?? "none", indent));
			}
			sb.Append(indent).AppendLine("}");
		}

		return sb.ToString();
	}

	public void RemoveHistoryPastDate(Date date) {
		foreach (var field in Fields.Values) {
			field.RemoveHistoryPastDate(date);
		}
	}
}
