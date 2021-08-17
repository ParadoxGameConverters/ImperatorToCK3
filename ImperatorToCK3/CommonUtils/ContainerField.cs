using System;
using System.Collections.Generic;
using System.Linq;
using commonItems;

namespace ImperatorToCK3.CommonUtils {
	public struct ContainerFieldStruct {
		public string fieldName;
		public string setter;
		public List<string> initialValue;
	}

	class DescendingComparer<T> : IComparer<T> where T : IComparable<T> {
		public int Compare(T x, T y) {
			return y.CompareTo(x);
		}
	}
	public class ContainerField {
		public ContainerField(List<string> initialValue) {
			InitialValue = initialValue;
		}
		public List<string> GetValue(Date date) {
			try {
				var pairWithLastEarlierOrSameDate = ValueHistory.TakeWhile(d => d.Key <= date).Last();
				return pairWithLastEarlierOrSameDate.Value;
			} catch (Exception) {
				return InitialValue;
			}
		}
		public SortedDictionary<Date, List<string>> ValueHistory { get; private set; } = new();
		public List<string> InitialValue { private get; set; } = new();
		public void AddValueToHistory(List<string> value, Date date) {
			ValueHistory[date] = value;
		}
	}
}
