using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Imperator.States;
using System;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Provinces;

// Test map layout (provinces.png is a 3x2 image):
//   row 0: 1 2 3
//   row 1: 1 4 3
// Province 4 is a colorable impassable, neighboring provinces 1, 2 and 3.
[Collection("Sequential")]
public class ProvinceCollectionTests {
	private const string GameRoot = "TestFiles/ProvinceCollectionTests/game";
	private readonly ModFilesystem modFS = new(GameRoot, Array.Empty<Mod>());
	private readonly StateCollection states = new();
	private readonly CountryCollection countries = [new Country(69), new Country(70), new Country(71)];

	private static BufferedReader BuildProvincesReader(string owner1, string owner2, string owner3) {
		return new BufferedReader(
			"= {\n" +
			$"\t1 = {{ owner={owner1} }}\n" +
			$"\t2 = {{ owner={owner2} }}\n" +
			$"\t3 = {{ owner={owner3} }}\n" +
			"\t4 = {}\n" +
			"}"
		);
	}

	[Fact]
	public void GetCountryForColorableImpassableReturnsOwnerControllingAtLeastHalfOfNeighbors() {
		var provinces = new ProvinceCollection();
		var reader = BuildProvincesReader("69", "69", "70"); // country 69 controls 2 of 3 neighbors
		var mapData = new MapData(modFS);
		provinces.LoadProvinces(reader, states, countries, mapData);

		var owner = provinces.GetCountryForColorableImpassable(4, mapData);

		Assert.NotNull(owner);
		Assert.Equal((ulong)69, owner.Id);
	}

	[Fact]
	public void GetCountryForColorableImpassableReturnsOwnerWhenAllNeighborsAreOwnedBySameCountry() {
		var provinces = new ProvinceCollection();
		var reader = BuildProvincesReader("69", "69", "69");
		var mapData = new MapData(modFS);
		provinces.LoadProvinces(reader, states, countries, mapData);

		var owner = provinces.GetCountryForColorableImpassable(4, mapData);

		Assert.NotNull(owner);
		Assert.Equal((ulong)69, owner.Id);
	}

	[Fact]
	public void GetCountryForColorableImpassableReturnsNullWhenNoCountryControlsAtLeastHalfOfNeighbors() {
		var provinces = new ProvinceCollection();
		var reader = BuildProvincesReader("69", "70", "71"); // each country controls only 1 of 3 neighbors
		var mapData = new MapData(modFS);
		provinces.LoadProvinces(reader, states, countries, mapData);

		var owner = provinces.GetCountryForColorableImpassable(4, mapData);

		Assert.Null(owner);
	}

	[Fact]
	public void GetCountryForColorableImpassableReturnsNullWhenNoNeighborIsOwned() {
		var provinces = new ProvinceCollection();
		var reader = BuildProvincesReader("0", "0", "0"); // owner 0 is not a loaded country
		var mapData = new MapData(modFS);
		provinces.LoadProvinces(reader, states, countries, mapData);

		var owner = provinces.GetCountryForColorableImpassable(4, mapData);

		Assert.Null(owner);
	}

	[Fact]
	public void GetCountryForColorableImpassableReturnsNullForProvinceWithoutNeighbors() {
		var provinces = new ProvinceCollection();
		var mapData = new MapData(modFS);

		var owner = provinces.GetCountryForColorableImpassable(42, mapData);

		Assert.Null(owner);
	}

	[Fact]
	public void ColorableImpassableGetsOwnerDuringProvinceLoading() {
		var provinces = new ProvinceCollection();
		var reader = BuildProvincesReader("69", "69", "70");
		var mapData = new MapData(modFS);
		provinces.LoadProvinces(reader, states, countries, mapData);

		var impassableProvince = provinces[4];
		Assert.NotNull(impassableProvince.OwnerCountry);
		Assert.Equal((ulong)69, impassableProvince.OwnerCountry.Id);

		// Province 4 needed no explicit owner in the save, so its owner must come from neighbor analysis:
		// country 69 should now own provinces 1, 2 and the impassable province 4.
		Assert.Equal(3, countries[69].TerritoriesCount);
		Assert.Equal(1, countries[70].TerritoriesCount);
	}
}