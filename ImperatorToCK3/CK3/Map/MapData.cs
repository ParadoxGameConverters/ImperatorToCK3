using commonItems;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.CK3.Map {
	public class MapData {
		private struct Point : IEquatable<Point> {
			public int X { get; set; }
			public int Y { get; set; }
			public Point(int x, int y) {
				X = x;
				Y = y;
			}

			public bool Equals(Point other) {
				return X == other.X && Y == other.Y;
			}
		}
		public SortedDictionary<ulong, HashSet<ulong>> NeighborsDict { get; } = new();
		public HashSet<ulong> ColorableImpassableProvinces { get; } = new();
		public Dictionary<ulong, ProvincePosition> ProvincePositions { get; } = new();
		public ProvinceDefinitions ProvinceDefinitions { get; }
		public MapData(string ck3Path) {
			string provincesMapPath = Path.Combine(ck3Path, "game/map_data/provinces.png");
			Logger.Debug("Loaded provinces map.");

			ProvinceDefinitions = new ProvinceDefinitions(ck3Path);
			Logger.Debug("Loaded province definitions.");
			DetermineProvincePositions(ck3Path);
			Logger.Debug("Loaded province positions.");
			using (Image<Rgb24> provincesMap = Image.Load<Rgb24>(provincesMapPath)) {
				DetermineNeighbors(provincesMap, ProvinceDefinitions);
			}
			Logger.Debug("Determined province neighbors.");
			FindImpassables(ck3Path);
			Logger.Debug("Found impassables.");
		}

		private void DetermineProvincePositions(string ck3Path) {
			var provincePositionsPath = Path.Combine(ck3Path, "game/gfx/map/map_object_data/building_locators.txt");
			var fileParser = new Parser();
			fileParser.RegisterKeyword("game_object_locator", reader => {
				var listParser = new Parser();
				listParser.RegisterKeyword("instances", instancesReader => {
					foreach (var blob in new BlobList(instancesReader).Blobs) {
						var blobReader = new BufferedReader(blob);
						var instance = ProvincePosition.Parse(blobReader);
						ProvincePositions[instance.Id] = instance;
					}
				});
				listParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
				listParser.ParseStream(reader);
			});
			fileParser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreItem);
			fileParser.ParseFile(provincePositionsPath);
		}

		private void DetermineNeighbors(Image<Rgb24> provincesMap, ProvinceDefinitions provinceDefinitions) {
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
		private void FindImpassables(string ck3Path) {
			var filePath = Path.Combine(ck3Path, "game/map_data/default.map");
			var parser = new Parser();
			const string listRegex = "sea_zones|river_provinces|lakes|impassable_mountains|impassable_seas";
			parser.RegisterRegex(listRegex, (reader, keyword) => {
				parser.GetNextTokenWithoutMatching(reader); // equals sign
				var typeOfGroup = parser.GetNextTokenWithoutMatching(reader);
				var provIds = reader.GetULongs();
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
					} else {
						ColorableImpassableProvinces.UnionWith(provIds);
					}
				}
			});
			parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			parser.ParseFile(filePath);
		}
		private static Rgb24 GetCenterColor(Point position, Image<Rgb24> provincesMap) {
			return GetPixelColor(position, provincesMap);
		}
		private static Rgb24 GetAboveColor(Point position, Image<Rgb24> provincesMap) {
			if (position.Y > 0) {
				--position.Y;
			}
			return GetPixelColor(position, provincesMap);
		}
		private static Rgb24 GetBelowColor(Point position, int height, Image<Rgb24> provincesMap) {
			if (position.Y < height - 1) {
				++position.Y;
			}
			return GetPixelColor(position, provincesMap);
		}
		private static Rgb24 GetLeftColor(Point position, Image<Rgb24> provincesMap) {
			if (position.X > 0) {
				--position.X;
			}
			return GetPixelColor(position, provincesMap);
		}
		private static Rgb24 GetRightColor(Point position, int width, Image<Rgb24> provincesMap) {
			if (position.X < width - 1) {
				++position.X;
			}
			return GetPixelColor(position, provincesMap);
		}
		private static Rgb24 GetPixelColor(Point position, Image<Rgb24> provincesMap) {
			return provincesMap[position.X, position.Y];
		}

		private void HandleNeighbor(
			Rgb24 centerColor,
			Rgb24 otherColor,
			ProvinceDefinitions provinceDefinitions
		) {
			if (!provinceDefinitions.ColorToProvinceDict.TryGetValue(centerColor, out ulong centerProvince)) {
				Logger.Warn($"Province not found for color {centerColor}!");
				return;
			}
			if (!provinceDefinitions.ColorToProvinceDict.TryGetValue(otherColor, out ulong otherProvince)) {
				Logger.Warn($"Province not found for color {otherColor}!");
				return;
			}
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
