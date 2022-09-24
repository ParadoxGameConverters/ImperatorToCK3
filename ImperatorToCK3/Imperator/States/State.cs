using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Mappers.Region;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.States; 

public class State : IIdentifiable<ulong> {
	public ulong Id { get; }
	public Province CapitalProvince { get; private set; } = null!;
	public ImperatorArea Area { get; private set; } = null!;
	public Country Country { get; private set; } = null!;

	public State(ulong id, BufferedReader stateReader, ProvinceCollection provinces, IdObjectCollection<string, ImperatorArea> areas, CountryCollection countries) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("capital", reader => CapitalProvince = provinces[reader.GetULong()]);
		parser.RegisterKeyword("area", reader => Area = areas[reader.GetString()]);
		parser.RegisterKeyword("country", reader => Country = countries[reader.GetULong()]);
		parser.IgnoreAndStoreUnregisteredItems(IgnoredKeywords);
		parser.ParseStream(stateReader);
	}

	public IEnumerable<Province> GetProvinces() {
		return Area.Provinces.Where(p => p.State.Id == this.Id);
	}

	public static OrderedSet<string> IgnoredKeywords { get; } = new();
}