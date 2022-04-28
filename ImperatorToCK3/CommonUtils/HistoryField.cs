using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils {
	public class HistoryField {
		public SortedDictionary<Date, object> ValueHistory { get; set; } = new();
		public object? InitialValue { get; set; }
		public ISet<string> Setters { get; }

		public HistoryField(string setter, object? initialValue) : this(new string[] {setter}, initialValue) { }
		public HistoryField(IEnumerable<string> setters, object? initialValue) {
			Setters = setters.ToHashSet();
			InitialValue = initialValue;
		}
		public object? GetValue(Date date) {
			var pairsWithEarlierOrSameDate = ValueHistory.TakeWhile(d => d.Key <= date);
			var withEarlierOrSameDate = pairsWithEarlierOrSameDate.ToList();
			return withEarlierOrSameDate.Count > 0 ? withEarlierOrSameDate.Last().Value : InitialValue;
		}
		public void AddValueToHistory(object value, Date date) {
			ValueHistory[date] = value;
		}
	}
}
