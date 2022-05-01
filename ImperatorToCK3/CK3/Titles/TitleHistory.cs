using commonItems;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.CK3.Titles;

public class TitleHistory : History {
	public TitleHistory() { }
	public TitleHistory(History history) : base(history.Fields) { }
	public void Update(HistoryFactory historyFactory, BufferedReader reader) {
		historyFactory.UpdateHistory(this, reader);
	}

	public string GetHolderId(Date date) {
		var idFromHistory = GetFieldValue("holder", date);
		if (idFromHistory is not null) {
			return idFromHistory.ToString()!;
		}
		return "0";
	}
	public string? GetGovernment(Date date) {
		if (GetFieldValue("government", date) is string govStr) {
			return govStr;
		}
		return null;
	}

	public string? GetLiege(Date date) {
		if (GetFieldValue("liege", date) is string liegeStr) {
			return liegeStr;
		}
		return null;
	}

	public int? GetDevelopmentLevel(Date date) {
		var historyValue = GetFieldValue("development_level", date);
		return historyValue switch {
			string devStr when int.TryParse(devStr, out int dev) => dev,
			int devInt => devInt,
			_ => null
		};
	}
}