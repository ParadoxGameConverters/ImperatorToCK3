using commonItems;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.CK3.Titles {
	public class TitleHistory {
		public TitleHistory() { }
		public TitleHistory(History history, Date ck3BookmarkDate) {
			InternalHistory = history;
			Liege = history.GetSimpleFieldValue("liege", ck3BookmarkDate);

			var developmentLevelOpt = history.GetSimpleFieldValue("development_level", ck3BookmarkDate);
			if (developmentLevelOpt is not null) {
				DevelopmentLevel = int.Parse(developmentLevelOpt);
			}
		}
		public void Update(HistoryFactory historyFactory, BufferedReader reader) {
			historyFactory.UpdateHistory(InternalHistory, reader);
		}

		public string GetHolderId(Date date) {
			var idFromHistory = InternalHistory.GetSimpleFieldValue("holder", date);
			if (idFromHistory is not null) {
				return idFromHistory;
			}
			return "0";
		}
		public string? GetGovernment(Date date) {
			return InternalHistory.GetSimpleFieldValue("government", date);
		}
		public string? Liege { get; set; }
		public int? DevelopmentLevel { get; set; }

		public History InternalHistory { get; } = new();
	}
}
