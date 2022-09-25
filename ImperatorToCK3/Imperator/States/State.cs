using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Provinces;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.States; 

public class State : IIdentifiable<ulong> {
	public ulong Id { get; }
	private ulong capitalProvinceId;
	public Area Area { get; private set; } = null!;
	public Country Country { get; private set; } = null!;

	public State(ulong id, BufferedReader stateReader, IdObjectCollection<string, Area> areas, CountryCollection countries) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("capital", reader => capitalProvinceId = reader.GetULong());
		parser.RegisterKeyword("area", reader => Area = areas[reader.GetString()]);
		parser.RegisterKeyword("country", reader => Country = countries[reader.GetULong()]);
		parser.IgnoreAndStoreUnregisteredItems(IgnoredKeywords);
		parser.ParseStream(stateReader);
	}

	public Province CapitalProvince => Area.Provinces.First(p => p.Id == capitalProvinceId);
	public IEnumerable<Province> Provinces => Area.Provinces.Where(p => p.State?.Id == Id);

	public static IgnoredKeywordsSet IgnoredKeywords { get; } = new();
}