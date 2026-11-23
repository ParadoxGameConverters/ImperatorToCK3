using commonItems.Mods;
using ImperatorToCK3.Imperator.Cultures;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Cultures;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CulturesDBTests {
	private static ModFilesystem GetTestModFilesystem() {
		var imperatorRoot = Path.Combine("TestFiles", "Imperator", "game");
		var mods = new List<Mod> {
			new("cool_mod", Path.Combine(Directory.GetCurrentDirectory(), "TestFiles/documents/Imperator/mod/cool_mod"))
		};
		return new ModFilesystem(imperatorRoot, mods);
	}

	[Fact]
	public void CulturesDBIsInitiallyEmpty() {
		var db = new CulturesDB();

		Assert.Empty(db);
		Assert.Null(db.GetMaleFamilyNameForm("any_family"));
	}

	[Fact]
	public void CulturesDBCanBeLoadedFromGameAndMods() {
		var db = new CulturesDB();
		db.Load(GetTestModFilesystem());

		Assert.Equal(4, db.Count); // roman, greek, duplicate (replaced), and mod groups
		Assert.NotNull(db["roman_culture_group"]);
		Assert.Equal(2, db["roman_culture_group"].Count);
		// The second definition of duplicate_id_group should have replaced the first one.
		Assert.Single(db["duplicate_id_group"]);
		Assert.NotNull(db["duplicate_id_group"]["new_culture"]);
		// The group from the mod should also be loaded.
		Assert.NotNull(db["mod_culture_group"]);
	}

	[Fact]
	public void GetMaleFamilyNameFormReturnsFormsFromGroupsAndCultures() {
		var db = new CulturesDB();
		db.Load(GetTestModFilesystem());

		Assert.Equal("Julius", db.GetMaleFamilyNameForm("Julius")); // group-level simple form
		Assert.Equal("Claudius", db.GetMaleFamilyNameForm("Claudii")); // group-level complex form
		Assert.Equal("Iulii", db.GetMaleFamilyNameForm("Iulii")); // culture-level form
		Assert.Equal("ModFamily", db.GetMaleFamilyNameForm("ModFamily")); // modded group
	}

	[Fact]
	public void GetMaleFamilyNameFormSearchesSubsequentGroupsWhenEarlierOnesDoNotMatch() {
		var db = new CulturesDB();
		db.Load(GetTestModFilesystem());

		// The family is defined in the greek group that comes after the roman one.
		Assert.Equal("Alexandros", db.GetMaleFamilyNameForm("Alexandridai"));
	}

	[Fact]
	public void GetMaleFamilyNameFormReturnsNullForUnknownFamily() {
		var db = new CulturesDB();
		db.Load(GetTestModFilesystem());

		Assert.Null(db.GetMaleFamilyNameForm("nonexistent_family"));
	}
}
