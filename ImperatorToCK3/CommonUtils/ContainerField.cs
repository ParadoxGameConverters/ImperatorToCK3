using System.Collections.Generic;
using System.Linq;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public class ContainerField {
		public ContainerField(List<string> initialValue) {
			InitialValue = initialValue;
		}
		public List<string> GetValue(Date date) {
			var pairsWithEarlierOrSameDate = ValueHistory.TakeWhile(d => d.Key <= date);
			if (pairsWithEarlierOrSameDate.Any()) {
				return pairsWithEarlierOrSameDate.Last().Value;
			}
			return InitialValue;
		}
		public SortedDictionary<Date, List<string>> ValueHistory { get; private set; } = new();
		public List<string> InitialValue { get; set; } = new();
		public void AddValueToHistory(List<string> value, Date date) {
			ValueHistory[date] = value;
		}
	}
}
