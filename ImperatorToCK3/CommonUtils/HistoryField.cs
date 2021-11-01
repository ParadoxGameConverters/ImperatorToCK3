using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils {
	public class HistoryField {
		public SortedDictionary<Date, object> ValueHistory { get; set; } = new();
		public object? InitialValue { get; set; }
		public string Setter { get; }

		public HistoryField(string setter, object? initialValue) {
			Setter = setter;
			InitialValue = initialValue;
		}
		public object? GetValue(Date date) {
			var pairsWithEarlierOrSameDate = ValueHistory.TakeWhile(d => d.Key <= date);
			return pairsWithEarlierOrSameDate.Any() ? pairsWithEarlierOrSameDate.Last().Value : InitialValue;
		}
		public void AddValueToHistory(object value, Date date) {
			ValueHistory[date] = value;
		}
	}
}
