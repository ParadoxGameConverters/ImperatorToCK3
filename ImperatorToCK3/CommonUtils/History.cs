using commonItems;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Text;

namespace ImperatorToCK3.CommonUtils {
	public class History : IPDXSerializable {
		[NonSerialized] public Dictionary<string, HistoryField> Fields { get; } = new(); // fieldName, field

		public History() { }
		public History(Dictionary<string, HistoryField> fields) {
			this.Fields = fields;
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
			var sb = new StringBuilder();
			foreach (var (fieldName, field) in Fields) {
				if (field.InitialValue is not null) {
					sb.Append(indent).Append('\t').Append(field.Setter)
						.Append(" = ")
						.AppendLine(PDXSerializer.GetValueRepresentation(field.InitialValue, indent));
				}
			}
			// TODO: SERIALIZE DATE BLOCKS
			return sb.ToString();
		}
	}
}
