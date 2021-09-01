using System.Collections.Generic;
using commonItems;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Religion;

namespace ImperatorToCK3.CK3.Provinces {
	public class Province {
		public Province() { }
		public Province(ulong id, BufferedReader reader) {
			// Load from a country file, if one exists. Otherwise rely on defaults.
			ID = id;
			details = new ProvinceDetails(reader);
		}
		public Province(ulong id, Province otherProvince) {
			ID = id;
			BaseProvinceID = otherProvince.ID;
			details = new ProvinceDetails(otherProvince.details);
		}

		public void InitializeFromImperator(
			Imperator.Provinces.Province impProvince,
			CultureMapper cultureMapper,
			ReligionMapper religionMapper
		) {
			ImperatorProvince = impProvince;

			// If we're initializing this from Imperator provinces, then having an owner or being a wasteland/sea is not a given -
			// there are uncolonized provinces in Imperator, also uninhabitables have culture and religion.

			var impOwnerCountry = ImperatorProvince.OwnerCountry.Value;
			if (impOwnerCountry is not null) {
				ownerTitle = impOwnerCountry.CK3Title; // linking to our holder's title
			}

			// Religion first
			SetReligionFromImperator(religionMapper);

			// Then culture
			SetCultureFromImperator(cultureMapper);

			// Holding type
			SetHoldingFromImperator();

			details.Buildings.Clear();
		}

		public ulong ID { get; } = 0;
		public ulong? BaseProvinceID { get; }
		public string Religion {
			get {
				return details.Religion;
			}
			set {
				details.Religion = value;
			}
		}
		public string Culture {
			get {
				return details.Culture;
			}
			set {
				details.Culture = value;
			}
		}
		public string Holding {
			get {
				return details.Holding;
			}
		}

		public List<string> Buildings => details.Buildings;
		public Imperator.Provinces.Province? ImperatorProvince { get; set; }

		private ProvinceDetails details = new();
		private Titles.Title? ownerTitle;

		private void SetReligionFromImperator(ReligionMapper religionMapper) {
			var religionSet = false;
			if (ImperatorProvince is null) {
				Logger.Warn($"CK3 Province {ID}: can't set religion from null Imperator Province!");
				return;
			}

			if (!string.IsNullOrEmpty(ImperatorProvince.Religion)) {
				var religionMatch = religionMapper.Match(ImperatorProvince.Religion, ID, ImperatorProvince.ID);
				if (religionMatch is not null) {
					details.Religion = religionMatch;
					religionSet = true;
				}
			}
			/*
			// Attempt to use religion of country. #TODO(#34): use country religion as fallback
			if (!religionSet && titleCountry.Value.Religion.Count>0) {
				details.Religion = titleCountry.Value.Religion;
				religionSet = true;
			}*/
			if (!religionSet) {
				//Use default CK3 religion.
				Logger.Debug($"Couldn't determine religion for province {ID} with source religion {ImperatorProvince.Religion}, using vanilla religion");
			}
		}
		private void SetCultureFromImperator(CultureMapper cultureMapper) {
			var cultureSet = false;
			if (ImperatorProvince is null) {
				Logger.Warn($"CK3 Province {ID}: can't set culture from null Imperator Province!");
				return;
			}

			// do we even have a base culture?
			if (!string.IsNullOrEmpty(ImperatorProvince.Culture)) {
				string ownerTitleName = string.Empty;
				if (ownerTitle is not null)
					ownerTitleName = ownerTitle.Name;
				var cultureMatch = cultureMapper.Match(ImperatorProvince.Culture, details.Religion, ID, ImperatorProvince.ID, ownerTitleName);
				if (cultureMatch is not null) {
					details.Culture = cultureMatch;
					cultureSet = true;
				}
			}
			/*
			// Attempt to use primary culture of country. #TODO(#34): use country primary culture as fallback
			if (!cultureSet && titleCountry.Value.Culture.Count > 0) {
				details.Culture = titleCountry.Value.PrimaryCulture;
				cultureSet = true;
			}*/
			if (!cultureSet) {
				//Use default CK3 culture.
				Logger.Debug($"Couldn't determine culture for province {ID} with source culture {ImperatorProvince.Culture}, using vanilla culture");
			}
		}
		private void SetHoldingFromImperator() {
			if (ImperatorProvince is null) {
				Logger.Warn($"CK3 Province {ID}: can't set holding from null Imperator Province!");
				return;
			}

			switch (ImperatorProvince.ProvinceRank) {
				case Imperator.Provinces.ProvinceRank.city_metropolis:
					details.Holding = "city_holding";
					break;
				case Imperator.Provinces.ProvinceRank.city:
					if (ImperatorProvince.Fort) {
						details.Holding = "castle_holding";
					} else {
						details.Holding = "city_holding";
					}
					break;
				case Imperator.Provinces.ProvinceRank.settlement:
					if (ImperatorProvince.HolySite) {
						details.Holding = "church_holding";
					} else if (ImperatorProvince.Fort) {
						details.Holding = "castle_holding";
					} else {
						details.Holding = "tribal_holding";
					}
					break;
			}
		}

	}
}
