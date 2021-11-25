using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Religion;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Provinces {
	public class Province {
		public Province() { }
		public Province(ulong id, BufferedReader reader, Date ck3BookmarkDate) {
			// Load from a country file, if one exists. Otherwise rely on defaults.
			Id = id;
			details = new ProvinceDetails(reader, ck3BookmarkDate);
		}
		public Province(ulong id, Province otherProvince) {
			Id = id;
			BaseProvinceId = otherProvince.Id;
			details = new ProvinceDetails(otherProvince.details);
		}

		public void InitializeFromImperator(
			Imperator.Provinces.Province impProvince,
			LandedTitles landedTitles,
			CultureMapper cultureMapper,
			ReligionMapper religionMapper
		) {
			ImperatorProvince = impProvince;

			// If we're initializing this from Imperator provinces, then having an owner or being a wasteland/sea is not a given -
			// there are uncolonized provinces in Imperator, also uninhabitables have culture and religion.

			var impOwnerCountry = ImperatorProvince.OwnerCountry;
			if (impOwnerCountry is not null) {
				ownerTitle = impOwnerCountry.CK3Title; // linking to our holder's title
			}

			// Religion first
			SetReligionFromImperator(religionMapper);

			// Then culture
			SetCultureFromImperator(cultureMapper);

			// Holding type
			SetHoldingFromImperator(landedTitles);

			details.Buildings.Clear();
		}

		public ulong Id { get; } = 0;
		public ulong? BaseProvinceId { get; }
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
		private Title? ownerTitle;

		private void SetReligionFromImperator(ReligionMapper religionMapper) {
			var religionSet = false;
			if (ImperatorProvince is null) {
				Logger.Warn($"CK3 Province {Id}: can't set religion from null Imperator Province!");
				return;
			}

			if (!string.IsNullOrEmpty(ImperatorProvince.Religion)) {
				var religionMatch = religionMapper.Match(ImperatorProvince.Religion, Id, ImperatorProvince.Id);
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
				Logger.Debug($"Couldn't determine religion for province {Id} with source religion {ImperatorProvince.Religion}, using vanilla religion");
			}
		}
		private void SetCultureFromImperator(CultureMapper cultureMapper) {
			var cultureSet = false;
			if (ImperatorProvince is null) {
				Logger.Warn($"CK3 Province {Id}: can't set culture from null Imperator Province!");
				return;
			}

			// do we even have a base culture?
			if (!string.IsNullOrEmpty(ImperatorProvince.Culture)) {
				string ownerTitleName = string.Empty;
				if (ownerTitle is not null) {
					ownerTitleName = ownerTitle.Name;
				}
				var cultureMatch = cultureMapper.Match(ImperatorProvince.Culture, details.Religion, Id, ImperatorProvince.Id, ownerTitleName);
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
				Logger.Debug($"Couldn't determine culture for province {Id} with source culture {ImperatorProvince.Culture}, using vanilla culture");
			}
		}
		private void SetHoldingFromImperator(LandedTitles landedTitles) {
			if (ImperatorProvince is null) {
				Logger.Warn($"CK3 Province {Id}: can't set holding from null Imperator Province!");
				return;
			}

			if (ImperatorProvince.OwnerCountry is null) {
				Logger.Warn($"CK3 Province {Id}: Imperator Province Owner Country is null!");
				return;
			}

			if (IsCountyCapital(landedTitles)) {
				// CK3 Holdings that are Provincial Capitals always match the Government Type
				switch (ImperatorProvince.OwnerCountry.GovernmentType) {
					case Imperator.Countries.GovernmentType.tribal:
						details.Holding = "tribal_holding";
						break;
					case Imperator.Countries.GovernmentType.republic:
						details.Holding = "city_holding";
						break;
					case Imperator.Countries.GovernmentType.monarchy:
						details.Holding = "castle_holding";
						break;
					default:
						details.Holding = "none";
						break;
				}
			} else {
				switch (ImperatorProvince.ProvinceRank) {
					case Imperator.Provinces.ProvinceRank.city_metropolis:
					case Imperator.Provinces.ProvinceRank.city:
						switch (ImperatorProvince.OwnerCountry.GovernmentType) {
							case Imperator.Countries.GovernmentType.tribal:
								if (ImperatorProvince.HolySite) {
									details.Holding = "church_holding";
								} else if (ImperatorProvince.Fort) {
									details.Holding = "castle_holding";
								} else {
									details.Holding = "city_holding";
								}

								break;
							case Imperator.Countries.GovernmentType.republic:
								if (ImperatorProvince.HolySite) {
									details.Holding = "church_holding";
								} else {
									details.Holding = "city_holding";
								}
								break;
							case Imperator.Countries.GovernmentType.monarchy:
								if (ImperatorProvince.HolySite) {
									details.Holding = "church_holding";
								} else if (ImperatorProvince.Fort) {
									details.Holding = "castle_holding";
								} else {
									details.Holding = "city_holding";
								}

								break;
							default:
								details.Holding = "city_holding";
								break;
						}
						break;
					case Imperator.Provinces.ProvinceRank.settlement:
						switch (ImperatorProvince.OwnerCountry.GovernmentType) {
							case Imperator.Countries.GovernmentType.tribal:
								details.Holding = "none";
								break;
							case Imperator.Countries.GovernmentType.republic:
								if (ImperatorProvince.HolySite) {
									details.Holding = "church_holding";
								} else if (ImperatorProvince.Fort) {
									details.Holding = "city_holding";
								} else {
									details.Holding = "none";
								}

								break;
							case Imperator.Countries.GovernmentType.monarchy:
								if (ImperatorProvince.HolySite) {
									details.Holding = "church_holding";
								} else if (ImperatorProvince.Fort) {
									details.Holding = "castle_holding";
								} else {
									details.Holding = "none";
								}

								break;
							default:
								details.Holding = "tribal_holding";
								break;
						}
						break;
					default:
						details.Holding = "none";
						break;
				}
			}
		}

		public bool IsCountyCapital(LandedTitles landedTitles) {
			var capitalProvIds = landedTitles.Values
				.Where(t => t.CapitalBaronyProvince is not null)
				.Select(t => (ulong)t.CapitalBaronyProvince!);
			return capitalProvIds.Contains(Id);
		}
	}
}
