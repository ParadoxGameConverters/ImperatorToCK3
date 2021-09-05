using System.Collections.Generic;
using System.Linq;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public class SimpleField {
		public SortedDictionary<Date, string> ValueHistory { get; private set; } = new();
		public string? InitialValue { private get; set; }

		public SimpleField(string? initialValue) {
			InitialValue = initialValue;
		}
		public string? GetValue(Date date) {
			var pairWithLastEarlierOrSameDate = ValueHistory.TakeWhile(d => d.Key <= date);
			if (pairWithLastEarlierOrSameDate.Any()) {
				return pairWithLastEarlierOrSameDate.Last().Value;
			}
			return InitialValue;
		}
		public void AddValueToHistory(string value, Date date) {
			ValueHistory[date] = value;
		}
	}
}
