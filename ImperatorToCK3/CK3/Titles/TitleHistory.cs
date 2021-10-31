using commonItems;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.CK3.Titles {
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
		public string? Liege { get; set; }
		public int? DevelopmentLevel { get; set; }

		public History InternalHistory { get; } = new();
	}
}
