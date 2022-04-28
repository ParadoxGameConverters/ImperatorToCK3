using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

public class HistoryField {
	public SortedDictionary<Date, FieldValue> ValueHistory { get; set; } = new();
	public FieldValue InitialValue { get; set; }
	public ISet<string> Setters { get; }

	public HistoryField(string setter, object? initialValue) : this(new string[] { setter }, initialValue) { }
	public HistoryField(IEnumerable<string> setters, object? initialValue) {
		Setters = setters.ToHashSet();
		InitialValue = new FieldValue(initialValue, setters.First());
	}
	public object? GetValue(Date date) {
		var pairsWithEarlierOrSameDate = ValueHistory.TakeWhile(d => d.Key <= date);
		var withEarlierOrSameDate = pairsWithEarlierOrSameDate.ToList();
		return withEarlierOrSameDate.Count > 0 ? withEarlierOrSameDate.Last().Value.Value : InitialValue.Value;
	}
	public void AddValueToHistory(object? value, string setter, Date date) {
		if (!Setters.Contains(setter)) {
			Logger.Warn($"Setter {setter} does not belong to history field's setters!");
		}
		ValueHistory[date] = new FieldValue(value, setter);
	}
}
