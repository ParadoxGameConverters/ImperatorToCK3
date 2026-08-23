using commonItems.Mods;
using ImperatorToCK3.CommonUtils.Map;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Map;

[Collection("MapTests")]
[CollectionDefinition("MapTests", DisableParallelization = true)]
public class ProvinceDefinitionsTests {
	[Fact]
	public void DictionariesDefaultToEmpty() {
		const string testCK3Root = "TestFiles/CK3/game";
		var ck3ModFs = new ModFilesystem(testCK3Root, new List<Mod>());
		
		var provDefs = new ProvinceDefinitions();
		provDefs.LoadDefinitions("definition.csv", ck3ModFs);
		
		Assert.Collection(provDefs.ColorToProvinceDict,
			pair1 => {
				(Rgb24 key, ulong value) = pair1;
				Assert.Equal(new Rgb24(42, 3, 128), key);
				Assert.Equal((ulong)1, value);
			},
			pair2 => {
				(Rgb24 key, ulong value) = pair2;
				Assert.Equal(new Rgb24(84, 6, 1), key);
				Assert.Equal((ulong)2, value);
			},
			pair3 => {
				(Rgb24 key, ulong value) = pair3;
				Assert.Equal(new Rgb24(126, 9, 129), key);
				Assert.Equal((ulong)3, value);
			}
		);
		Assert.Collection(provDefs.ProvinceToColorDict,
			pair1 => {
				(ulong key, Rgb24 value) = pair1;
				Assert.Equal((ulong)1, key);
				Assert.Equal(new Rgb24(42, 3, 128), value);
			},
			pair2 => {
				(ulong key, Rgb24 value) = pair2;
				Assert.Equal((ulong)2, key);
				Assert.Equal(new Rgb24(84, 6, 1), value);
			},
			pair3 => {
				(ulong key, Rgb24 value) = pair3;
				Assert.Equal((ulong)3, key);
				Assert.Equal(new Rgb24(126, 9, 129), value);
			}
		);
	}

	[Fact]
	public void ExceptionIsThrownOnUnparseableLine() {
		const string ck3Root = "TestFiles/corruptedCK3/game";
		// this definition.csv has a line with quoted province id
		var ck3ModFs = new ModFilesystem(ck3Root, new List<Mod>());
		Assert.Throws<FormatException>(() => new ProvinceDefinitions().LoadDefinitions("definition.csv", ck3ModFs));
	}
}