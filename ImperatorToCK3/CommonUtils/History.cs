using commonItems;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CommonUtils {
	public class History : IPDXSerializable {
		[NonSerialized] public Dictionary<string, HistoryField> Fields { get; } = new(); // fieldName, field

		public History() { }
		public History(Dictionary<string, HistoryField> fields) {
			Fields = fields;
		}

		public object? GetFieldValue(string fieldName, Date date) {
			return Fields.TryGetValue(fieldName, out var value) ? value.GetValue(date) : null;
		}

		// The setter parameter is used when a new history field needs to be created
		public void AddFieldValue(string fieldName, object value, Date date, string? setter) {
			if (Fields.TryGetValue(fieldName, out var field)) {
				field.AddValueToHistory(value, date);
			} else {
				if (setter is null) {
					Logger.Error($"Cannot create history field {fieldName} without a setter!");
					return;
				}
				var newField = new HistoryField(setter, null);
				newField.AddValueToHistory(value, date);
				Fields.Add(fieldName, newField);
			}
		}

		public string Serialize(string indent) {
			var entriesByDate = new SortedDictionary<Date, List<KeyValuePair<string, object>>>(); // <date, list<setter, value>>
			foreach (var field in Fields.Values) {
				foreach (var (date, value) in field.ValueHistory) {
					var setterValuePair = new KeyValuePair<string, object>(field.Setter, value);
					if (entriesByDate.TryGetValue(date, out var listForDate)) {
						listForDate.Add(setterValuePair);
					} else {
						var entryList = new List<KeyValuePair<string, object>>();
						entriesByDate[date] = entryList;
						entryList.Add(setterValuePair);
					}
				}
			}

			var sb = new StringBuilder();
			foreach (HistoryField field in Fields.Values.Where(f => f.InitialValue is not null)) {
				sb.Append(indent).Append(field.Setter)
					.Append(" = ")
					.AppendLine(PDXSerializer.GetValueRepresentation(field.InitialValue, indent));
			}
			foreach (var (date, entries) in entriesByDate) {
				sb.Append(indent).Append(date).AppendLine(" = {");
				foreach (var (setter, value) in entries) {
					sb.Append('\t').Append(setter)
						.Append(" = ")
						.AppendLine(PDXSerializer.GetValueRepresentation(value, indent));
				}
				sb.Append(indent).AppendLine("}");
			}

			return sb.ToString();
		}
	}
}
