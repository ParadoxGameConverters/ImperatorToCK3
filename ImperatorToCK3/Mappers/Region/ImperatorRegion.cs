using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Geography;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Region;

public class ImperatorRegion : IIdentifiable<string> {
	public IdObjectCollection<string, Area> Areas { get; } = new();
	public string Id { get; }

	public ImperatorRegion(string id, BufferedReader reader, IdObjectCollection<string, Area> areas) {
		Id = id;
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
		
		LinkAreas(areas);
	}

	private void LinkAreas(IdObjectCollection<string, Area> areas) {
		foreach (var requiredAreaId in parsedAreas) {
			if (areas.TryGetValue(requiredAreaId, out var area)) {
				Areas.Add(area);
			} else {
				throw new KeyNotFoundException($"Region's {Id} area {requiredAreaId} does not exist!");
			}
		}
	}

	private void RegisterKeys(Parser parser) {
		parser.RegisterKeyword("areas", reader => {
			foreach (var name in reader.GetStrings()) {
				parsedAreas.Add(name);
			}
		});
		parser.IgnoreAndLogUnregisteredItems();
	}

	public bool ContainsProvince(ulong province) {
		return Areas.Any(area => area.ContainsProvince(province));
	}

	private readonly HashSet<string> parsedAreas = new();
}