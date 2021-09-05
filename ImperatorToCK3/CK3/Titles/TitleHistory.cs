using commonItems;
using ImperatorToCK3.CommonUtils;

namespace ImperatorToCK3.CK3.Titles {
	public class TitleHistory {
		public TitleHistory() { }
		public TitleHistory(History history) {
			this.history = history;
			var date = new Date(867, 1, 1);
			var holderFromHistory = history.GetSimpleFieldValue("holder", date);
			if (holderFromHistory is null) {
				Logger.Warn("TitleHistory: holder should not be null!");
			} else {
				Holder = holderFromHistory;
			}
			Liege = history.GetSimpleFieldValue("liege", date);
			Government = history.GetSimpleFieldValue("government", date);

			var developmentLevelOpt = history.GetSimpleFieldValue("development_level", date);
			if (developmentLevelOpt is not null) {
				DevelopmentLevel = int.Parse(developmentLevelOpt);
			}
		}
		public void Update(HistoryFactory historyFactory, BufferedReader reader) {
			historyFactory.UpdateHistory(history, reader);
		}

		// These values are open to ease management.
		// This is a storage container for CK3::Title.
		public string Holder { get; set; } = "0"; // ID of Character holding the Title
		public string? Liege { get; set; }
		public string? Government { get; set; }
		public int? DevelopmentLevel { get; set; }

		private readonly History history = new();
	}
}
