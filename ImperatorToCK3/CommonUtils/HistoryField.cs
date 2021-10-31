using System.Collections.Generic;
using System.Linq;
using commonItems;
using commonItems.Serialization;

namespace ImperatorToCK3.CommonUtils {
	public class HistoryField : IPDXSerializable {
		public SortedDictionary<Date, object> ValueHistory { get; set; } = new();
		public object? InitialValue { get; set; }

		public HistoryField(object? initialValue) {
			InitialValue = initialValue;
		}
		public object? GetValue(Date date) {
			var pairWithLastEarlierOrSameDate = ValueHistory.TakeWhile(d => d.Key <= date);
			if (pairWithLastEarlierOrSameDate.Any()) {
				return pairWithLastEarlierOrSameDate.Last().Value;
			}
			return InitialValue;
		}
		public void AddValueToHistory(object value, Date date) {
			ValueHistory[date] = value;
		}
	}
}
