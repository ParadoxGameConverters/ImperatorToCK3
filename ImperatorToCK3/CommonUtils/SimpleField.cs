using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public struct SimpleFieldStruct {
		string fieldName;
		string setter;
		string? initialValue;
	}
	public class SimpleField {
		public SortedDictionary<Date, string> ValueHistory { get; private set; } = new();
		public string? InitialValue { private get; set; }

		public SimpleField(string? initialValue) {
			InitialValue = initialValue;
		}
		public string? GetValue(Date date) {
			try {
				var pairWithLastEarlierOrSameDate = ValueHistory.TakeWhile(d => d.Key <= date).Last();
				return pairWithLastEarlierOrSameDate.Value;
			} catch (Exception) {
				return InitialValue;
			}
		}
		public void AddValueToHistory(string value, Date date) {
			ValueHistory[date] = value;
		}
	}
}
