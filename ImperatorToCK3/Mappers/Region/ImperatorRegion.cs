using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Region; 

public class ImperatorRegion : IIdentifiable<string> {
	public IdObjectCollection<string, ImperatorArea> Areas { get; } = new();
	public string Id { get; }

	public ImperatorRegion(string id, BufferedReader reader) {
		Id = id;
		var parser = new Parser();
		RegisterKeys(parser);
		parser.ParseStream(reader);
	}

	public bool ContainsProvince(ulong province) {
		return Areas.Any(area => area.ContainsProvince(province));
	}

	public void LinkAreas(IdObjectCollection<string, ImperatorArea> areasDict) {
		foreach (var requiredAreaName in parsedAreas) {
			if (areasDict.TryGetValue(requiredAreaName, out var area)) {
				AddArea(area);
			} else {
				throw new KeyNotFoundException($"Region's {Id} area {requiredAreaName} does not exist!");
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

	private void AddArea(ImperatorArea area) {
		Areas.Add(area);
	}

	private readonly HashSet<string> parsedAreas = new();
}