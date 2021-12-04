using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.CK3.Titles;

public class TitleHistory {
	public TitleHistory() { }
	public TitleHistory(History history, Date ck3BookmarkDate) {
		InternalHistory = history;
		if (history.GetFieldValue("liege", ck3BookmarkDate) is string liegeStr) {
			Liege = liegeStr;
		}

		var developmentLevelOpt = history.GetFieldValue("development_level", ck3BookmarkDate);
		if (developmentLevelOpt is string devStr) {
			DevelopmentLevel = int.Parse(devStr);
		}
	}
	public void Update(HistoryFactory historyFactory, BufferedReader reader) {
		historyFactory.UpdateHistory(InternalHistory, reader);
	}

	public string GetHolderId(Date date) {
		var idFromHistory = InternalHistory.GetFieldValue("holder", date);
		if (idFromHistory is string idStr) {
			return idStr;
		}
		return "0";
	}
	public string? GetGovernment(Date date) {
		if (InternalHistory.GetFieldValue("government", date) is string govStr) {
			return govStr;
		}
		return null;
	}

	public string? GetLiege(Date date) {
		if (InternalHistory.GetFieldValue("liege", date) is string liegeStr) {
			return liegeStr;
		}
		return null;
	}

	public int? GetDevelopmentLevel(Date date) {
		if (InternalHistory.GetFieldValue("development_level", date) is not string devStr) {
			return null;
		}

		if (int.TryParse(devStr, out int dev)) {
			return dev;
		}
		return null;
	}

	public void RemoveHistoryPastBookmarkDate(Date ck3BookmarkDate) {
		InternalHistory.RemoveHistoryPastDate(ck3BookmarkDate);

	}

	[SerializeOnlyValue] public History InternalHistory { get; } = new();
}