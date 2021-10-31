using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils {
	public class History {
		public Dictionary<string, HistoryField> Fields { get; } = new(); // fieldName, field

		public History() { }
		public History(Dictionary<string, HistoryField> fields) {
			this.Fields = fields;
		}

		public object? GetFieldValue(string fieldName, Date date) {
			return Fields.TryGetValue(fieldName, out var value) ? value.GetValue(date) : null;
		}

		public void AddFieldValue(string fieldName, object value, Date date) {
			if (Fields.TryGetValue(fieldName, out var field)) {
				field.AddValueToHistory(value, date);
			} else {
				var newField = new HistoryField(null);
				newField.AddValueToHistory(value, date);
				Fields.Add(fieldName, newField);
			}
		}
	}
}
