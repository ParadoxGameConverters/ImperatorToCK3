using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public class History {
		public Dictionary<string, SimpleField> SimpleFields { get; } = new(); // fieldName, field
		public Dictionary<string, ContainerField> ContainerFields { get; } = new(); // fieldName, field

		public History() { }
		public History(Dictionary<string, SimpleField> simpleFields, Dictionary<string, ContainerField> containerFields) {
			this.SimpleFields = simpleFields;
			this.ContainerFields = containerFields;
		}

		public string? GetSimpleFieldValue(string fieldName, Date date) {
			if (SimpleFields.TryGetValue(fieldName, out var value)) {
				return value.GetValue(date);
			}
			return null;
		}
		public List<string>? GetContainerFieldValue(string fieldName, Date date) {
			if (ContainerFields.TryGetValue(fieldName, out var value)) {
				return value.GetValue(date);
			}
			return null;
		}
	}
}
