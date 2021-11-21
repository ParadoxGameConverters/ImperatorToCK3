using commonItems;
using ImperatorToCK3.Imperator.Countries;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Provinces {
	public enum ProvinceRank { settlement, city, city_metropolis }
	public partial class Province {
		public ulong Id { get; } = 0;
		public string Name { get; set; } = "";
		public string Culture { get; set; } = "";
		public string Religion { get; set; } = "";
		public KeyValuePair<ulong, Country?> OwnerCountry { get; set; } = new(0, null);
		public ulong Controller { get; set; } = 0;
		public Dictionary<ulong, Pops.Pop> Pops { get; set; } = new();
		public ProvinceRank ProvinceRank { get; set; } = ProvinceRank.settlement;
		public ParadoxBool Fort { get; set; } = new(false);
		public bool HolySite { get; set; } = false;
		public uint BuildingCount { get; set; } = 0;
		public double CivilizationValue { get; set; } = 0;

		public Province(ulong id) {
			Id = id;
		}
		public int GetPopCount() {
			return Pops.Count;
		}
		public void LinkOwnerCountry(Country? country) {
			if (country is null) {
				Logger.Warn($"Province {Id}: cannot link null country!");
				return;
			}
			if (country.Id != OwnerCountry.Key) {
				Logger.Warn($"Province {Id}: cannot link country {country.Id}: wrong ID!");
			} else {
				OwnerCountry = new(OwnerCountry.Key, country);
			}
		}

		// Returns a count of linked pops
		public int LinkPops(Pops.Pops pops) {
			int counter = 0;
			foreach (var popId in parsedPopIds) {
				if (pops.TryGetValue(popId, out var popToLink)) {
					Pops.Add(popId, popToLink);
					++counter;
				} else {
					Logger.Warn($"Pop with ID {popId} has no definition!");
				}
			}
			return counter;
		}

		private readonly HashSet<ulong> parsedPopIds = new();
	}
}
