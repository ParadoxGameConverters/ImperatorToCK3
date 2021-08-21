using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;
using ImperatorToCK3.TempMocks.Countries;

namespace ImperatorToCK3.Imperator.Provinces {
	public enum ProvinceRank { settlement, city, city_metropolis };
	public class Province {
		public ulong ID { get; } = 0;
		public string Name { get; set; } = "";
		public string Culture { get; set; } = "";
		public string Religion { get; set; } = "";
		public KeyValuePair<ulong, Country> OwnerCountry { get; set; } = new(0, null);
		public ulong Controller { get; set; } = 0;
		public Dictionary<ulong, Pops.Pop?> Pops { get; set; } = new();
		public ProvinceRank ProvinceRank { get; set; } = ProvinceRank.settlement;
		public bool Fort { get; set; } = false;
		public bool HolySite { get; set; } = false;
		public uint BuildingCount { get; set; } = 0;
		public double CivilizationValue { get; set; } = 0;

		public Province(ulong ID) {
			this.ID = ID;
		}
		public int GetPopCount() {
			return Pops.Count;
		}
		public void LinkOwnerCountry(Country? country) {
			if (country is null) {
				Logger.Log(LogLevel.Warning, $"Province {ID}: cannot link null country!");
				return;
			}
			if (country.ID != OwnerCountry.Key) {
				Logger.Log(LogLevel.Warning, $"Province {ID}: cannot link country {country.ID}: wrong ID!");
			} else {
				OwnerCountry = new(OwnerCountry.Key, country);
			}
		}
	}
}
