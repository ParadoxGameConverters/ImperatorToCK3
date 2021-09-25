using commonItems;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CK3.Map {
	public class MapData {
		private struct Point {
			public int X { get; set; }
			public int Y { get; set; }
			public Point(int x, int y) {
				X = x;
				Y = y;
			}
		}
		public SortedDictionary<ulong, HashSet<ulong>> NeighborsDict { get; } = new();
		public HashSet<ulong> ColorableImpassableProvinces { get; } = new();
		public MapData(MagickImage provincesMap, ProvinceDefinitions provinceDefinitions, Configuration config) {
			DetermineNeighbors(provincesMap, provinceDefinitions);
			FindImpassables(config);
		}
		private void DetermineNeighbors(MagickImage provincesMap, ProvinceDefinitions provinceDefinitions) {
			var height = provincesMap.Height;
			var width = provincesMap.Width;
			for (var y = 0; y < height; ++y) {
				for (var x = 0; x < width; ++x) {
					var position = new Point(x, y);

					var centerColor = GetCenterColor(position, provincesMap);
					var aboveColor = GetAboveColor(position, provincesMap);
					var belowColor = GetBelowColor(position, height, provincesMap);
					var leftColor = GetLeftColor(position, provincesMap);
					var rightColor = GetRightColor(position, width, provincesMap);

					if (!centerColor.Equals(aboveColor)) {
						HandleNeighbor(centerColor, aboveColor, provinceDefinitions);
					}
					if (!centerColor.Equals(rightColor)) {
						HandleNeighbor(centerColor, rightColor, provinceDefinitions);
					}
					if (!centerColor.Equals(belowColor)) {
						HandleNeighbor(centerColor, belowColor, provinceDefinitions);
					}
					if (!centerColor.Equals(leftColor)) {
						HandleNeighbor(centerColor, leftColor, provinceDefinitions);
					}
				}
			}
		}
		private void FindImpassables(Configuration config) {
			var filePath = Path.Combine(config.Ck3Path, "game/map_data/default.map");
			var parser = new Parser();
			const string listRegex = "sea_zones|river_provinces|lakes|impassable_mountains|impassable_seas";
			parser.RegisterRegex(listRegex, (reader, keyword) => {
				Parser.GetNextTokenWithoutMatching(reader); // equals sign
				var typeOfGroup = Parser.GetNextTokenWithoutMatching(reader);
				var provIds = ParserHelpers.GetULongs(reader);
				if (keyword == "impassable_mountains") {
					if (typeOfGroup == "RANGE") {
						if (provIds.Count is < 1 or > 2) {
							throw new FormatException("A range of provinces should have 1 or 2 elements!");
						}
						var beginning = provIds[0];
						var end = provIds.Last();
						for (var id = beginning; id <= end; ++id) {
							ColorableImpassableProvinces.Add(id);
						}
					} else { // type is "LIST"
						ColorableImpassableProvinces.UnionWith(provIds);
					}
				}
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseFile(filePath);
		}
		private static MagickColor GetCenterColor(Point position, MagickImage provincesMap) {
			return GetPixelColor(position, provincesMap);
		}
		private static MagickColor GetAboveColor(Point position, MagickImage provincesMap) {
			if (position.Y > 0) {
				--position.Y;
			}
			return GetPixelColor(position, provincesMap);
		}
		private static MagickColor GetBelowColor(Point position, int height, MagickImage provincesMap) {
			if (position.Y < height - 1) {
				++position.Y;
			}
			return GetPixelColor(position, provincesMap);
		}
		private static MagickColor GetLeftColor(Point position, MagickImage provincesMap) {
			if (position.X > 0) {
				--position.X;
			}
			return GetPixelColor(position, provincesMap);
		}
		private static MagickColor GetRightColor(Point position, int width, MagickImage provincesMap) {
			if (position.X < width - 1) {
				++position.X;
			}
			return GetPixelColor(position, provincesMap);
		}
		private static MagickColor GetPixelColor(Point position, MagickImage provincesMap) {
			var pixels = provincesMap.GetPixels();
			var pixel = pixels.GetPixel(position.X, position.Y);
			var color = pixel.ToColor();
			if (color is null) {
				throw new IndexOutOfRangeException($"Cannot get color for position {position.X}, {position.Y}");
			}
			return new MagickColor(color);
		}

		private void HandleNeighbor(
			MagickColor centerColor,
			MagickColor otherColor,
			ProvinceDefinitions provinceDefinitions
		) {
			var centerProvince = provinceDefinitions.ColorToProvinceDict[centerColor];
			var otherProvince = provinceDefinitions.ColorToProvinceDict[otherColor];
			AddNeighbor(centerProvince, otherProvince);
		}

		private void AddNeighbor(ulong mainProvince, ulong neighborProvince) {
			if (NeighborsDict.TryGetValue(mainProvince, out var neighbors)) {
				neighbors.Add(neighborProvince);
			} else {
				NeighborsDict[mainProvince] = new HashSet<ulong> { neighborProvince };
			}
		}
	}
}
