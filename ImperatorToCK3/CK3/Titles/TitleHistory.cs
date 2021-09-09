using commonItems;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.CK3.Titles {
	public class TitleHistory {
		public TitleHistory() { }
		public TitleHistory(History history, Date ck3BookmarkDate) {
			History = history;
			Liege = history.GetSimpleFieldValue("liege", ck3BookmarkDate);
			Government = history.GetSimpleFieldValue("government", ck3BookmarkDate);

			var developmentLevelOpt = history.GetSimpleFieldValue("development_level", ck3BookmarkDate);
			if (developmentLevelOpt is not null) {
				DevelopmentLevel = int.Parse(developmentLevelOpt);
			}
		}
		public void Update(HistoryFactory historyFactory, BufferedReader reader) {
			historyFactory.UpdateHistory(History, reader);
		}

		// This is a storage container for CK3::Title.
		public string GetHolderId(Date date) {
			var idFromHistory = History.GetSimpleFieldValue("holder", date);
			if (idFromHistory is not null) {
				return idFromHistory;
			}
			return "0";
		}
		public string? Liege { get; set; }
		public string? Government { get; set; }
		public int? DevelopmentLevel { get; set; }

		public History History { get; } = new();
	}
}
