using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Provinces;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Region;

public class ImperatorArea : IIdentifiable<string> {
	public SortedSet<Imperator.Provinces.Province> Provinces { get; } = new();
	public string Id { get; }

	public ImperatorArea(string id, BufferedReader areaReader, ProvinceCollection provinces) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("provinces", reader => {
			var provinceIds = reader.GetULongs();
			var provincesToAdd = provinces.Where(p => provinceIds.Contains(p.Id));
			Provinces.UnionWith(provincesToAdd);
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(areaReader);
	}
	
	public bool ContainsProvince(ulong provinceId) {
		return Provinces.Any(p=>p.Id == provinceId);
	}
}