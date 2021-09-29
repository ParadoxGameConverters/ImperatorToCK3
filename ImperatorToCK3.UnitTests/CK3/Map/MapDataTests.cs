using ImperatorToCK3.CK3.Map;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Map {
	[Collection("MapTests")]
	[CollectionDefinition("MapTests", DisableParallelization = true)]
	public class MapDataTests {
		private const string testCK3Path = "TestFiles/CK3";
		[Fact]
		public void NeighborsDictDefaultsToEmpty() {
			var provincesMap = new ImageMagick.MagickImage();
			var definitions = new ProvinceDefinitions(testCK3Path);
			var data = new MapData(provincesMap, definitions, testCK3Path);

			Assert.Empty(data.NeighborsDict);
		}
		[Fact]
		public void NeighborProvincesCanBeDetermined() {
			const string testCK3Path2 = "TestFiles/CK3_all_prov_defs";
			const ulong byzantionId = 496;
			var provincesMap = new ImageMagick.MagickImage(testCK3Path2 + "/game/map_data/provinces.png");
			var definitions = new ProvinceDefinitions(testCK3Path2);
			Assert.True(definitions.ProvinceToColorDict.ContainsKey(496));

			var data = new MapData(provincesMap, definitions, testCK3Path);
			Assert.True(data.NeighborsDict.ContainsKey(496));

			var byzantionNeighborProvs = data.NeighborsDict[byzantionId];
			var expectedByzantionProvs = new HashSet<ulong> {
				3761, // Selymbria
				8668, // sea_bosporus
				947 // sea_marmara
				// There is also a dark grey circle in the middle of Byzantion,
				// but the color is not found in definition.csv, so there's no ID to add to neighbors.
			};
			Assert.True(byzantionNeighborProvs.SetEquals(expectedByzantionProvs));
		}

		[Fact]
		public void ProvinceNotFoundForColorIsLogged() {
			var output = new StringWriter();
			Console.SetOut(output);

			const string testCK3Path2 = "TestFiles/CK3_all_prov_defs";
			var provincesMap = new ImageMagick.MagickImage(testCK3Path2 + "/game/map_data/provinces.png");
			var definitions = new ProvinceDefinitions(testCK3Path2);

			_ = new MapData(provincesMap, definitions, testCK3Path);
			Assert.Contains("Province not found for color #1E1E1E", output.ToString());
		}
	}
}
