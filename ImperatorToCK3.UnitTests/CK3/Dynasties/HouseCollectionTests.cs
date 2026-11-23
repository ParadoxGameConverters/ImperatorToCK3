using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Dynasties;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Dynasties;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class HouseCollectionTests {
	private static readonly Date TestDate = new(867, 1, 1);

	private static string CreateTestEnvironment(string houseDefinitions, string preservedHouseIds) {
		var tempRoot = Path.Combine(Path.GetTempPath(), "HouseCollectionTests", Guid.NewGuid().ToString());
		var housesDir = Path.Combine(tempRoot, "ck3", "common", "dynasty_houses");
		Directory.CreateDirectory(housesDir);
		File.WriteAllText(Path.Combine(housesDir, "houses.txt"), houseDefinitions);

		var configurablesDir = Path.Combine(tempRoot, "configurables");
		Directory.CreateDirectory(configurablesDir);
		File.WriteAllText(Path.Combine(configurablesDir, "dynasty_houses_to_preserve.txt"), preservedHouseIds);
		return tempRoot;
	}

	private static HouseCollection LoadHouses(string tempRoot) {
		var originalWorkingDirectory = Directory.GetCurrentDirectory();
		try {
			Directory.SetCurrentDirectory(tempRoot);
			var houses = new HouseCollection();
			houses.LoadCK3Houses(new ModFilesystem(Path.Combine(tempRoot, "ck3"), Array.Empty<Mod>()));
			return houses;
		} finally {
			Directory.SetCurrentDirectory(originalWorkingDirectory);
		}
	}

	private static void DeleteTempEnvironment(string tempRoot) {
		try {
			Directory.Delete(tempRoot, recursive: true);
		} catch {
			// Failure to delete the temp directory can be ignored.
		}
	}

	private static House AddHouse(HouseCollection houses, string id) {
		var house = new House(id, new BufferedReader(string.Empty));
		houses.AddOrReplace(house);
		return house;
	}

	[Fact]
	public void LoadCK3Houses_LoadsHousesFromGameFiles() {
		var tempRoot = CreateTestEnvironment(
			"""
			house_a = { name = First }
			house_a = { name = Second }
			house_b = { prefix = "of" dynasty = dynn_b motto = "Ever Vigilant" forced_coa_religiongroup = christianity_religion }
			""",
			string.Empty
		);
		try {
			var houses = LoadHouses(tempRoot);

			Assert.Equal(2, houses.Count);

			Assert.True(houses.TryGetValue("house_a", out var houseA));
			Assert.Equal("Second", houseA.Name);

			Assert.True(houses.TryGetValue("house_b", out var houseB));
			Assert.Equal("of", houseB.Prefix);
			Assert.Equal("dynn_b", houseB.DynastyId);
			Assert.Equal("Ever Vigilant", houseB.Motto);
			Assert.Equal("christianity_religion", houseB.ForcedCoaReligionGroup);
		} finally {
			DeleteTempEnvironment(tempRoot);
		}
	}

	[Fact]
	public void LoadCK3Houses_LoadsPreservedHouseIds() {
		var tempRoot = CreateTestEnvironment(string.Empty, "house_c");
		try {
			var houses = LoadHouses(tempRoot);
			AddHouse(houses, "house_c");

			houses.RemoveUnlessConfiguredToPreserve("house_c");

			Assert.True(houses.ContainsKey("house_c"));
		} finally {
			DeleteTempEnvironment(tempRoot);
		}
	}

	[Fact]
	public void PurgeUnneededHouses_RemovesOnlyUnreferencedAndUnconfiguredHouses() {
		var tempRoot = CreateTestEnvironment(string.Empty, "house_configured");
		try {
			var houses = LoadHouses(tempRoot);
			AddHouse(houses, "house_referenced");
			AddHouse(houses, "house_configured");
			AddHouse(houses, "house_unneeded");

			var characters = new CharacterCollection();
			var characterWithHouse = new Character("1", "Alice", TestDate, characters);
			characterWithHouse.SetDynastyHouseId("house_referenced", null);
			characters.Add(characterWithHouse);
			characters.Add(new Character("2", "Bob", TestDate, characters));

			houses.PurgeUnneededHouses(characters, TestDate);

			Assert.True(houses.ContainsKey("house_referenced"));
			Assert.True(houses.ContainsKey("house_configured"));
			Assert.False(houses.ContainsKey("house_unneeded"));
			Assert.Equal(2, houses.Count);
		} finally {
			DeleteTempEnvironment(tempRoot);
		}
	}

	[Fact]
	public void PurgeUnneededHouses_PurgesEverythingWhenNoCharactersAndNoConfiguredHouses() {
		var tempRoot = CreateTestEnvironment(string.Empty, string.Empty);
		try {
			var houses = LoadHouses(tempRoot);
			AddHouse(houses, "house_a");
			AddHouse(houses, "house_b");

			var characters = new CharacterCollection();

			houses.PurgeUnneededHouses(characters, TestDate);

			Assert.Empty(houses);
		} finally {
			DeleteTempEnvironment(tempRoot);
		}
	}

	[Fact]
	public void RemoveUnlessConfiguredToPreserve_RemovesHouseNotOnPreserveList() {
		var tempRoot = CreateTestEnvironment(string.Empty, "house_other");
		try {
			var houses = LoadHouses(tempRoot);
			AddHouse(houses, "house_target");

			houses.RemoveUnlessConfiguredToPreserve("house_target");

			Assert.False(houses.ContainsKey("house_target"));
		} finally {
			DeleteTempEnvironment(tempRoot);
		}
	}

	[Fact]
	public void RemoveUnlessConfiguredToPreserve_KeepsHouseOnPreserveList() {
		var tempRoot = CreateTestEnvironment(string.Empty, "house_target");
		try {
			var houses = LoadHouses(tempRoot);
			AddHouse(houses, "house_target");

			houses.RemoveUnlessConfiguredToPreserve("house_target");

			Assert.True(houses.ContainsKey("house_target"));
		} finally {
			DeleteTempEnvironment(tempRoot);
		}
	}
}
