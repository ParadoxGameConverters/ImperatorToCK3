using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Pops;
using ImperatorToCK3.Imperator.States;
using System.Linq;

namespace ImperatorToCK3.Imperator.Provinces;

public class ProvinceCollection : IdObjectCollection<ulong, Province> {
	public ProvinceCollection() { }
	public void LoadProvinces(BufferedReader provincesReader, StateCollection states) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.Integer, (reader, provIdStr) => {
			var newProvince = Province.Parse(reader, ulong.Parse(provIdStr), states);
			Add(newProvince);
		});
		parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		parser.ParseStream(provincesReader);
	}
	public void LinkPops(PopCollection pops) {
		var counter = this.Sum(province => province.LinkPops(pops));
		Logger.Info($"{counter} pops linked to provinces.");
	}
	public void LinkCountries(CountryCollection countries) {
		var counter = this.Count(province => province.TryLinkOwnerCountry(countries));
		Logger.Info($"{counter} provinces linked to countries.");
	}
}