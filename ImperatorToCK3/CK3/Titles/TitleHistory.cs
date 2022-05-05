using commonItems;

namespace ImperatorToCK3.CK3.Titles;

public partial class Title {

	public string GetHolderId(Date date) {
		var idFromHistory = History.GetFieldValue("holder", date);
		if (idFromHistory is not null) {
			return idFromHistory.ToString()!;
		}
		return "0";
	}
	public string? GetGovernment(Date date) {
		if (History.GetFieldValue("government", date) is string govStr) {
			return govStr;
		}
		return null;
	}

	public string? GetLiege(Date date) {
		if (History.GetFieldValue("liege", date) is string liegeStr) {
			return liegeStr;
		}
		return null;
	}

	public int? GetDevelopmentLevel(Date date) {
		var historyValue = History.GetFieldValue("development_level", date);
		return historyValue switch {
			string devStr when int.TryParse(devStr, out int dev) => dev,
			int devInt => devInt,
			_ => null
		};
	}
}