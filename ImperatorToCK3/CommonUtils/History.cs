using System.Collections.Generic;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public class History {
		public Dictionary<string, SimpleField> simpleFields = new(); // fieldName, field
		public Dictionary<string, ContainerField> containerFields = new(); // fieldName, field

		public History() { }
		public History(Dictionary<string, SimpleField> simpleFields, Dictionary<string, ContainerField> containerFields) {
			this.simpleFields = simpleFields;
			this.containerFields = containerFields;
		}

		public string? GetSimpleFieldValue(string fieldName, Date date) {
			if (simpleFields.TryGetValue(fieldName, out var value)) {
				return value.GetValue(date);
			}
			return null;
		}
		public List<string>? GetContainerFieldValue(string fieldName, Date date) {
			if (containerFields.TryGetValue(fieldName, out var value)) {
				return value.GetValue(date);
			}
			return null;
		}
	}
}
