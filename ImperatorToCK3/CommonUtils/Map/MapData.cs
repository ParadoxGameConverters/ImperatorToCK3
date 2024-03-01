using commonItems;
using commonItems.Mods;
using CsvHelper;
using CsvHelper.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ImperatorToCK3.CommonUtils.Map;

public class MapData {
	[StructLayout(LayoutKind.Auto)]
	private struct Point(int x, int y) : IEquatable<Point> {
		public int X { get; set; } = x;
		public int Y { get; set; } = y;

		public readonly bool Equals(Point other) {
			return X == other.X && Y == other.Y;
		}

		public override readonly bool Equals(object? obj) {
			return obj is Point point && Equals(point);
		}

		public override readonly int GetHashCode() {
			return HashCode.Combine(X, Y);
		}
	}

	private SortedDictionary<ulong, HashSet<ulong>> NeighborsDict { get; } = [];
	public ISet<ulong> ColorableImpassableProvinceIds { get; } = new HashSet<ulong>();
	private readonly Dictionary<ulong, ProvincePosition> provincePositions = [];
	public IReadOnlyDictionary<ulong, ProvincePosition> ProvincePositions => provincePositions;
	public ProvinceDefinitions ProvinceDefinitions { get; }

	private readonly Dictionary<ulong, string> provinceToTypeDict = [];

	private readonly Dictionary<ulong, HashSet<ulong>> provinceAdjacencies = new();

	private readonly HashSet<string> impassableLandProvinceTypes = ["impassable_mountains"];
	private readonly HashSet<string> staticWaterProvinceTypes = ["sea_zones", "lakes", "impassable_seas"];
	private readonly HashSet<string> riverProvinceTypes = ["river_provinces"];
	private IEnumerable<string> ProvinceTypes => impassableLandProvinceTypes
		.Union(staticWaterProvinceTypes)
		.Union(riverProvinceTypes);

	private string provincesMapFilename = "provinces.png";

	public MapData(ModFilesystem modFS) {
		string definitionsFilename = "definition.csv";
		string adjacenciesFilename = "adjacencies.csv";

		Logger.Info("Loading default map data...");
		const string defaultMapPath = "map_data/default.map";
		var defaultMapParser = new Parser();
		defaultMapParser.RegisterKeyword("definitions", reader => definitionsFilename = reader.GetString());
		defaultMapParser.RegisterKeyword("provinces", reader => provincesMapFilename = reader.GetString());
		defaultMapParser.RegisterKeyword("rivers", ParserHelpers.IgnoreItem);
		defaultMapParser.RegisterKeyword("topology", ParserHelpers.IgnoreItem);
		defaultMapParser.RegisterKeyword("terrain", ParserHelpers.IgnoreItem);
		defaultMapParser.RegisterKeyword("adjacencies", reader => adjacenciesFilename = reader.GetString());
		defaultMapParser.RegisterKeyword("island_region", ParserHelpers.IgnoreItem);
		defaultMapParser.RegisterKeyword("seasons", ParserHelpers.IgnoreItem);
		foreach (var provType in ProvinceTypes) {
			defaultMapParser.RegisterKeyword(provType, reader => {
				Parser.GetNextTokenWithoutMatching(reader); // equals sign
				DetermineProvinceTypes(provType, reader);
			});
		}
		
		defaultMapParser.IgnoreAndLogUnregisteredItems(); // TODO: check which types need to be added
		defaultMapParser.ParseGameFile(defaultMapPath, modFS);
		Logger.IncrementProgress();

		Logger.Info("Loading province definitions...");
		ProvinceDefinitions = new ProvinceDefinitions(definitionsFilename, modFS);
		Logger.IncrementProgress();

		Logger.Info("Loading province positions...");
		DetermineProvincePositions(modFS);
		Logger.IncrementProgress();
		
		Logger.Info("Loading province adjacencies...");
		LoadAdjacencies(adjacenciesFilename, modFS);

		DetermineColorableImpassableProvinces();
		Logger.Debug("Excluding impassable provinces that border the map edge from the colorable set...");
		ExcludeMapEdgeProvincesFromColorableImpassables(modFS);

		Logger.Info("Determining province neighbors...");
		var provincesMapPath = GetProvincesMapPath(modFS);
		if (provincesMapPath is not null) {
			using Image<Rgb24> provincesMap = Image.Load<Rgb24>(provincesMapPath);
			DetermineNeighbors(provincesMap, ProvinceDefinitions);
		}

		Logger.IncrementProgress();
	}

	private string? GetProvincesMapPath(ModFilesystem modFS) {
		var relativeMapPath = Path.Join("map_data", provincesMapFilename);
		var provincesMapPath = modFS.GetActualFileLocation(relativeMapPath);
		if (provincesMapPath is not null) {
			return provincesMapPath;
		}

		Logger.Warn($"{nameof(provincesMapPath)} not found!");
		return null;
	}

	public double GetDistanceBetweenProvinces(ulong province1, ulong province2) {
		if (!ProvincePositions.TryGetValue(province1, out var province1Position)) {
			Logger.Warn($"Province {province1} has no position defined!");
			return 0;
		}

		if (!ProvincePositions.TryGetValue(province2, out var province2Position)) {
			Logger.Warn($"Province {province2} has no position defined!");
			return 0;
		}

		var xDiff = province1Position.X - province2Position.X;
		var yDiff = province1Position.Y - province2Position.Y;
		return Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
	}

	public IReadOnlySet<ulong> GetNeighborProvinceIds(ulong provinceId) {
		return NeighborsDict.TryGetValue(provinceId, out var neighbors) ? neighbors : [];
	}

	public bool IsImpassable(ulong provinceId) {
		return provinceToTypeDict.TryGetValue(provinceId, out var type) && impassableLandProvinceTypes.Contains(type);
	}

	public bool IsLand(ulong provinceId) {
		// Regular land provinces are not included in provinceToTypeDict.
		return !provinceToTypeDict.ContainsKey(provinceId) || IsImpassable(provinceId);
	}

	private void DetermineProvincePositions(ModFilesystem modFS) {
		const string provincePositionsPath = "gfx/map/map_object_data/building_locators.txt";
		var fileParser = new Parser();
		fileParser.RegisterKeyword("game_object_locator", reader => {
			var listParser = new Parser();
			listParser.RegisterKeyword("instances", instancesReader => {
				foreach (var blob in new BlobList(instancesReader).Blobs) {
					var blobReader = new BufferedReader(blob);
					var instance = ProvincePosition.Parse(blobReader);
					provincePositions[instance.Id] = instance;
				}
			});
			listParser.IgnoreUnregisteredItems();
			listParser.ParseStream(reader);
		});
		fileParser.IgnoreUnregisteredItems();
		fileParser.ParseGameFile(provincePositionsPath, modFS);
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

	private void DetermineProvinceTypes(string provincesType, BufferedReader provincesGroupReader) {
		var typeOfGroup = Parser.GetNextTokenWithoutMatching(provincesGroupReader);
		var provIds = provincesGroupReader.GetULongs();

		if (typeOfGroup == "RANGE") {
			if (provIds.Count is < 1 or > 2) {
				throw new FormatException("A range of provinces should have 1 or 2 elements!");
			}

			var beginning = provIds[0];
			var end = provIds[^1];
			for (var id = beginning; id <= end; ++id) {
				provinceToTypeDict[id] = provincesType;
			}
		} else {
			foreach (var id in provIds) {
				provinceToTypeDict[id] = provincesType;
			}
		}
	}

	private void DetermineColorableImpassableProvinces() {
		var provincesPerType = provinceToTypeDict
			.GroupBy(d => d.Value)
			.ToDictionary(g => g.Key, g => g.Select(d => d.Key).ToList());

		foreach (var grouping in provincesPerType) {
			if (impassableLandProvinceTypes.Contains(grouping.Key)) {
				ColorableImpassableProvinceIds.UnionWith(grouping.Value);
			}
		}
	}

	private void ExcludeMapEdgeProvincesFromColorableImpassables(ModFilesystem modFS) {
		var mapPath = GetProvincesMapPath(modFS);
		if (mapPath is null) {
			return;
		}
		
		using var mapPng = Image.Load<Rgb24>(mapPath);
		var height = mapPng.Height;
		var width = mapPng.Width;
		var edgeProvinceIds = new HashSet<ulong>();

		for (var y = 0; y < height; ++y) {
			// Get left edge color.
			var color = GetPixelColor(new Point(0, y), mapPng);
			edgeProvinceIds.Add(ProvinceDefinitions.ColorToProvinceDict[color]);

			// Get right edge color.
			color = GetPixelColor(new Point(width - 1, y), mapPng);
			edgeProvinceIds.Add(ProvinceDefinitions.ColorToProvinceDict[color]);
		}

		for (var x = 0; x < width; ++x) {
			// Get top edge color.
			var color = GetPixelColor(new Point(x, 0), mapPng);
			edgeProvinceIds.Add(ProvinceDefinitions.ColorToProvinceDict[color]);

			// Get bottom edge color.
			color = GetPixelColor(new Point(x, height - 1), mapPng);
			edgeProvinceIds.Add(ProvinceDefinitions.ColorToProvinceDict[color]);
		}

		ColorableImpassableProvinceIds.ExceptWith(edgeProvinceIds);
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
			NeighborsDict[mainProvince] = [neighborProvince];
		}
	}

	/// Function for checking if two provinces are directly neighboring or are connected by a maximum number of water tiles.
	public bool AreProvincesAdjacent(ulong province1, ulong province2, int maxWaterTilesDistance) { // TODO: cache the results or save to a dictionary
		if (NeighborsDict.TryGetValue(province1, out var neighbors)) {
			if (neighbors.Contains(province2)) {
				return true;
			}
		}

		if (NeighborsDict.TryGetValue(province2, out var otherNeighbors)) {
			if (otherNeighbors.Contains(province1)) {
				return true;
			}
		}
		
		if (provinceAdjacencies.TryGetValue(province1, out var adjacencies) && adjacencies.Contains(province2)) {
			return true; // TODO: COVER THIS WITH A TEST
		}

		// If the provinces are not directly neighboring, check if they are connected by a maximum number of water tiles.
		return AreProvincesConnectedByWater(province1, province2, maxWaterTilesDistance);
	}

	private bool AreProvincesConnectedByWater(ulong prov1Id, ulong prov2Id, int maxWaterTilesDistance) {
		if (maxWaterTilesDistance < 1) {
			return false;
		}
		
		// Get all water provinces in range.
		int currentDistance = 1;
		var provincesToCheckForWaterNeighbors = new HashSet<ulong> { prov1Id };
		var provincesCheckedForWaterNeighbors = new HashSet<ulong>();
		var waterProvincesInRange = new HashSet<ulong>();
		while (currentDistance <= maxWaterTilesDistance) {
			foreach (var provinceIdToCheck in provincesToCheckForWaterNeighbors.ToList()) {
				if (!provincesCheckedForWaterNeighbors.Add(provinceIdToCheck)) {
					continue;
				}

				if (provinceToTypeDict.TryGetValue(provinceIdToCheck, out var provinceType)) {
					// Only consider static water types, so exclude rivers.
					if (staticWaterProvinceTypes.Contains(provinceType)) {
						waterProvincesInRange.Add(provinceIdToCheck);
					}
				}

				if (NeighborsDict.TryGetValue(provinceIdToCheck, out var neighbors)) {
					provincesToCheckForWaterNeighbors.UnionWith(neighbors);
				}
			}

			++currentDistance;
		}

		// For every static water province in range, get its land neighbors.
		foreach (var waterProvince in waterProvincesInRange) {
			if (!NeighborsDict.TryGetValue(waterProvince, out var neighbors)) {
				continue;
			}

			if (neighbors.Any(neighborId => neighborId == prov2Id && IsLand(neighborId))) {
				return true;
			}
		}

		return false;
	}

	private void LoadAdjacencies(string adjacenciesFilename, ModFilesystem modFS) {
		var adjacenciesPath = modFS.GetActualFileLocation(Path.Join("map_data", adjacenciesFilename));
		if (adjacenciesPath is null) {
			Logger.Warn($"Adjacencies file {adjacenciesFilename} not found!");
			return;
		}
		
		var reader = new StreamReader(adjacenciesPath);
		
		var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) {
			Delimiter = ";",
			HasHeaderRecord = true,
			AllowComments = true,
			TrimOptions = TrimOptions.Trim,
			IgnoreBlankLines = true,
			ShouldSkipRecord = (args => {
				string? cell = args.Row[0];
				if (cell is null) {
					return true;
				}

				cell = cell.Trim();
				return cell.Length == 0 || cell[0] == '#';
			}),
		};
		using CsvReader csv = new(reader, csvConfig);
		var adjacency = new {
			From = default(long),
			To = default(long),
		};
		var records = csv.GetRecords(adjacency);

		int count = 0;
		foreach (var record in records) {
			if (record.From == -1) {
				continue;
			}
			if (record.To == -1) {
				continue;
			}
			AddAdjacency((ulong)record.From, (ulong)record.To);
			++count;
		}
		Logger.Debug($"Loaded {count} province adjacencies.");
	}

	private void AddAdjacency(ulong province1, ulong province2) {
		if (!provinceAdjacencies.TryGetValue(province1, out var adjacencies)) {
			adjacencies = new HashSet<ulong>();
			provinceAdjacencies[province1] = adjacencies;
		}

		adjacencies.Add(province2);

		// Since adjacency is bidirectional, add the reverse adjacency as well
		if (!provinceAdjacencies.TryGetValue(province2, out adjacencies)) {
			adjacencies = new HashSet<ulong>();
			provinceAdjacencies[province2] = adjacencies;
		}

		adjacencies.Add(province1);
	}
}