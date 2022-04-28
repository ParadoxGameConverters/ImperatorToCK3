using commonItems;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CommonUtils {
	public class History : IPDXSerializable {
		[commonItems.Serialization.NonSerialized] public Dictionary<string, HistoryField> Fields { get; } = new(); // fieldName, field

		public History() { }
		public History(Dictionary<string, HistoryField> fields) {
			Fields = fields;
		}

		public object? GetFieldValue(string fieldName, Date date) {
			return Fields.TryGetValue(fieldName, out var value) ? value.GetValue(date) : null;
		}

		public void AddFieldValue(string fieldName, object? value, Date date, string setter) {
			AddFieldValue(fieldName, value, date, setter, new string[] { setter });
		}
		// The allSetters parameter is used when a new history field needs to be created
		public void AddFieldValue(string fieldName, object? value, Date date, string setter, IEnumerable<string> allSetters) {
			if (Fields.TryGetValue(fieldName, out var field)) {
				field.AddValueToHistory(value, setter, date);
			} else {
				if (allSetters is null) {
					Logger.Error($"Cannot create history field {fieldName} without a setter!");
					return;
				}
				var newField = new HistoryField(allSetters, null);
				newField.AddValueToHistory(value, setter, date);
				Fields.Add(fieldName, newField);
			}
		}

		public string Serialize(string indent, bool withBraces) {
			var entriesByDate = new SortedDictionary<Date, List<FieldValue>>(); // <date, list<setter, value>>
			foreach (var field in Fields.Values) {
				foreach (var (date, fieldValue) in field.ValueHistory) {
					if (entriesByDate.TryGetValue(date, out var listForDate)) {
						listForDate.Add(fieldValue);
					} else {
						var entryList = new List<FieldValue>();
						entriesByDate[date] = entryList;
						entryList.Add(fieldValue);
					}
				}
			}

			var sb = new StringBuilder();
			foreach (HistoryField field in Fields.Values.Where(f => f.InitialValue.Value is not null)) {
				var initialValue = field.InitialValue;

				if (initialValue.Value is IEnumerable<object> enumerable && !enumerable.Any()) {
					// we don't need to output empty lists
					continue;
				}

				sb.Append(indent).Append(initialValue.Setter)
					.Append('=')
					.AppendLine(PDXSerializer.Serialize(initialValue.Value ?? "none", indent));
			}
			foreach (var (date, fieldEntries) in entriesByDate) {
				sb.Append(indent).Append(date).AppendLine("={");
				foreach (var entry in fieldEntries) {
					sb.Append(indent).Append('\t').Append(entry.Setter)
						.Append('=')
						.AppendLine(PDXSerializer.Serialize(entry.Value ?? "none", indent));
				}
				sb.Append(indent).AppendLine("}");
			}

			return sb.ToString();
		}

		public void RemoveHistoryPastDate(Date date) {
			foreach (var field in Fields.Values) {
				field.ValueHistory = new SortedDictionary<Date, FieldValue>(
					field.ValueHistory.Where(entry => entry.Key <= date)
						.ToDictionary(pair => pair.Key, pair => pair.Value)
				);
			}
		}
	}
}
