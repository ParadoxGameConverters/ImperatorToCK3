using commonItems;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.CK3.Titles {
	public class TitleHistory {
		public TitleHistory() { }
		public TitleHistory(History history, Date ck3BookmarkDate) {
			this.History = history;
			var holderFromHistory = history.GetSimpleFieldValue("holder", ck3BookmarkDate);
			if (holderFromHistory is null) {
				Logger.Warn("TitleHistory: holder should not be null!");
			} else {
				Holder = holderFromHistory;
			}
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

		// These values are open to ease management.
		// This is a storage container for CK3::Title.
		public string Holder { get; set; } = "0"; // ID of Character holding the Title
		public string? Liege { get; set; }
		public string? Government { get; set; }
		public int? DevelopmentLevel { get; set; }

		public History History { get; } = new();
	}
}
