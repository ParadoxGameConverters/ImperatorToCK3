using commonItems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Mappers.Region {
	public class ImperatorRegion {
		private readonly HashSet<string> parsedAreas = new();
		public Dictionary<string, ImperatorArea> Areas { get; } = new();
		public string Name { get; }

		public ImperatorRegion(string name, BufferedReader reader) {
			Name = name;
			var parser = new Parser();
			RegisterKeys(parser);
			parser.ParseStream(reader);
		}
		private void RegisterKeys(Parser parser) {
			parser.RegisterKeyword("areas", reader => {
				foreach (var name in reader.GetStrings()) {
					parsedAreas.Add(name);
				}
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
		}

		public bool ContainsProvince(ulong province) {
			return Areas.Values.Any(area => area.ContainsProvince(province));
		}

		private void AddArea(string name, ImperatorArea area) {
			Areas[name] = area;
		}
		public void LinkAreas(Dictionary<string, ImperatorArea> areasDict) {
			foreach (var requiredAreaName in parsedAreas) {
				if (areasDict.TryGetValue(requiredAreaName, out var area)) {
					AddArea(requiredAreaName, area);
				} else {
					throw new KeyNotFoundException($"Region's {Name} area {requiredAreaName} does not exist!");
				}
			}
		}
	}
}
