using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using ImperatorToCK3.Imperator.Geography;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Region;

public sealed class ImperatorRegion : IIdentifiable<string> {
	public IdObjectCollection<string, Area> Areas { get; } = [];
	public string Id { get; }
	public Color? Color { get; private set; }

	public ImperatorRegion(string id, BufferedReader reader, IdObjectCollection<string, Area> areas, ColorFactory colorFactory) {
		Id = id;
		var parser = new Parser();
		RegisterKeys(parser, colorFactory);
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

	private void RegisterKeys(Parser parser, ColorFactory colorFactory) {
		parser.RegisterKeyword("areas", reader => {
			foreach (var name in reader.GetStrings()) {
				parsedAreas.Add(name);
			}
		});
		parser.RegisterKeyword("color", reader => {
			try {
				Color = colorFactory.GetColor(reader);
			} catch (Exception e) {
				Logger.Warn($"Region {Id} has invalid color! {e.Message}");
			}
		});
		parser.IgnoreAndLogUnregisteredItems();
	}

	public bool ContainsProvince(ulong province) {
		return Areas.Any(area => area.ContainsProvince(province));
	}

	private readonly HashSet<string> parsedAreas = new();
}