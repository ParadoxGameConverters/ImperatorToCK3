using commonItems;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.CK3.Titles {
	public class TitleHistory {
		public TitleHistory() { }
		public TitleHistory(History history, Date ck3BookmarkDate) {
			History = history;
			Liege = history.GetSimpleFieldValue("liege", ck3BookmarkDate);

			var developmentLevelOpt = history.GetSimpleFieldValue("development_level", ck3BookmarkDate);
			if (developmentLevelOpt is not null) {
				DevelopmentLevel = int.Parse(developmentLevelOpt);
			}
		}
		public void Update(HistoryFactory historyFactory, BufferedReader reader) {
			historyFactory.UpdateHistory(History, reader);
		}

		public string GetHolderId(Date date) {
			var idFromHistory = History.GetSimpleFieldValue("holder", date);
			if (idFromHistory is not null) {
				return idFromHistory;
			}
			return "0";
		}
		public string? GetGovernment(Date date) {
			return History.GetSimpleFieldValue("government", date);
		}
		public string? Liege { get; set; }
		public int? DevelopmentLevel { get; set; }

		public History History { get; } = new();
	}
}
