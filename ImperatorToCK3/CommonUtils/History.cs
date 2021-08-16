using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public class History {
		private Dictionary<string, SimpleField> simpleFields; // fieldName, field
		private Dictionary<string, ContainerField> containerFields; // fieldName, field

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
