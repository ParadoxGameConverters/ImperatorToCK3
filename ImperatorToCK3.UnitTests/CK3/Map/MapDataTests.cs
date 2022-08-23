using commonItems.Mods;
using ImperatorToCK3.CK3.Map;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Map;

[Collection("MapTests")]
[CollectionDefinition("MapTests", DisableParallelization = true)]
public class MapDataTests {
	[Fact]
	public void NeighborsDictDefaultsToEmpty() {
		const string ck3Root = "TestFiles/MapData/CK3_1_province_map/game";
		var ck3ModFS = new ModFilesystem(ck3Root, new List<Mod>());
		var data = new MapData(ck3ModFS);

		Assert.Empty(data.NeighborsDict);
	}

	[Fact]
	public void NeighborProvincesCanBeDetermined() {
		const string ck3Root = "TestFiles/MapData/CK3_all_prov_defs/game";
		const ulong byzantionId = 496;

		var ck3ModFS = new ModFilesystem(ck3Root, new List<Mod>());
		var data = new MapData(ck3ModFS);
		Assert.True(data.ProvinceDefinitions.ProvinceToColorDict.ContainsKey(byzantionId));
		Assert.True(data.NeighborsDict.ContainsKey(byzantionId));

		var byzantionNeighborProvs = data.NeighborsDict[byzantionId];
		var expectedByzantionNeighborProvs = new HashSet<ulong> {
			3761, // Selymbria
			8668, // sea_bosporus
			947 // sea_marmara
			// There is also a dark grey circle in the middle of Byzantion,
			// but the color is not found in definition.csv, so there's no ID to add to neighbors.
		};
		Assert.True(byzantionNeighborProvs.SetEquals(expectedByzantionNeighborProvs));
	}

	[Fact]
	public void ProvinceNotFoundForColorIsLogged() {
		var output = new StringWriter();
		Console.SetOut(output);

		const string ck3Root = "TestFiles/MapData/CK3_all_prov_defs/game";
		var ck3ModFS = new ModFilesystem(ck3Root, new List<Mod>());
		_ = new MapData(ck3ModFS);
		Assert.Contains("Province not found for color Rgb24(30, 30, 30)", output.ToString());
	}
}