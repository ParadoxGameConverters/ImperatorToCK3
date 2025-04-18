﻿using commonItems;

namespace ImperatorToCK3.CK3.Titles;

internal partial class Title {
	public string GetHolderId(Date date) {
		var idFromHistory = History.GetFieldValue("holder", date);
		if (idFromHistory is not null) {
			return idFromHistory.ToString()!;
		}
		return "0";
	}

	public string? GetGovernment(Date date) {
		var value = History.GetFieldValue("government", date);
		if (value is string govStr) {
			return govStr.RemQuotes();
		} else if (value is StringOfItem govItem) {
			return govItem.ToString().RemQuotes();
		}
		return null;
	}

	public void SetGovernment(string governmentId, Date date) {
		History.AddFieldValue(date, "government", "government", governmentId);
	}

	public string? GetLiegeId(Date date) {
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