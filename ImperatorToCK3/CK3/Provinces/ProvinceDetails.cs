using System.Collections.Generic;
using ImperatorToCK3.CommonUtils;
using commonItems;

namespace ImperatorToCK3.CK3.Provinces {
	public class ProvinceDetails {
		// These values are open to ease management.
		// This is a storage container for CK3::Province.
		public string Culture { get; set; } = string.Empty;
		public string Religion { get; set; } = string.Empty;
		public string Holding { get; set; } = "none";
		public List<string> Buildings { get; } = new();

		public ProvinceDetails() { }
		public ProvinceDetails(ProvinceDetails otherDetails) {
			this.Culture = otherDetails.Culture;
			Religion = otherDetails.Religion;
			Holding = otherDetails.Holding;
			Buildings = new List<string>(otherDetails.Buildings);
		}
		public ProvinceDetails(BufferedReader reader) {
			var history = historyFactory.GetHistory(reader);
			var date = new Date(867, 1, 1);

			var cultureOpt = history.GetSimpleFieldValue("culture", date);
			if (cultureOpt is not null) {
				Culture = cultureOpt;
			}
			var religionOpt = history.GetSimpleFieldValue("religion", date);
			if (religionOpt is not null) {
				Religion = religionOpt;
			}
			var holdingOpt = history.GetSimpleFieldValue("holding", date);
			if (holdingOpt is null) {
				Logger.Warn("Province's holding can't be null!");
			} else {
				Holding = holdingOpt;
			}
			var buildingsOpt = history.GetContainerFieldValue("buildings", date);
			if (buildingsOpt is null) {
				Logger.Warn("Province's buildings list can't be null!");
			} else {
				Buildings = buildingsOpt;
			}
		}

		private static readonly HistoryFactory historyFactory = new(
			simpleFieldDefs: new() {
				new() { FieldName = "culture", Setter = "culture", InitialValue = null },
				new() { FieldName = "religion", Setter = "religion", InitialValue = null },
				new() { FieldName = "holding", Setter = "holding", InitialValue = "none" }
			},
			containerFieldDefs: new() {
				new() { FieldName = "buildings", Setter = "buildings", InitialValue = new() }
			}
		);
	}
}
