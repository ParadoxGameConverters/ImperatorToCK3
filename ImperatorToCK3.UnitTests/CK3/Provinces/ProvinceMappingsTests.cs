using commonItems.Mods;
using ImperatorToCK3.CK3.Provinces;
using System;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Provinces;

public class ProvinceMappingsTests {
	[Fact]
	public void MappingsDefaultToEmpty() {
		var emptyCK3ModFs = new ModFilesystem("TestFiles/missing_folder", new List<Mod>());

		Assert.Empty(new ProvinceMappings(emptyCK3ModFs));
	}

	[Fact]
	public void MappingsCanBeLoadedFromFilesystem() {
		const string ck3Root = "TestFiles/CK3ProvinceMappingTests/normal";
		var ck3ModFs = new ModFilesystem(ck3Root, new List<Mod>());
		// ReSharper disable once CollectionNeverUpdated.Local
		var provinceMappings = new ProvinceMappings(ck3ModFs);

		Assert.Collection(provinceMappings,
			mapping1 => {
				Assert.Equal((ulong)3, mapping1.Key);
				Assert.Equal((ulong)1, mapping1.Value);
			},
			mapping2 => {
				Assert.Equal((ulong)4, mapping2.Key);
				Assert.Equal((ulong)1, mapping2.Value);
			},
			mapping3 => {
				Assert.Equal((ulong)5, mapping3.Key);
				Assert.Equal((ulong)4, mapping3.Value);
			}
		);
	}

	[Fact]
	public void MappingIsIgnoredIfRightSideIsSameAsLeftSide() {
		const string ck3Root = "TestFiles/CK3ProvinceMappingTests/leftEqualsRight";
		var ck3ModFs = new ModFilesystem(ck3Root, new List<Mod>());
		// ReSharper disable once CollectionNeverUpdated.Local
		var provinceMappings = new ProvinceMappings(ck3ModFs);

		Assert.Collection(provinceMappings,
			mapping1 => {
				Assert.Equal((ulong)104, mapping1.Key);
				Assert.Equal((ulong)101, mapping1.Value);
			}
		);
	}

	[Fact]
	public void MappingsWithSameTargetProvAreOverwritten() {
		const string ck3Root = "TestFiles/CK3ProvinceMappingTests/overwriting";
		var ck3ModFs = new ModFilesystem(ck3Root, new List<Mod>());
		// ReSharper disable once CollectionNeverUpdated.Local
		var provinceMappings = new ProvinceMappings(ck3ModFs);

		Assert.Collection(provinceMappings,
			mapping1 => {
				Assert.Equal((ulong)4, mapping1.Key);
				Assert.Equal((ulong)2, mapping1.Value);
			}
		);
	}
}