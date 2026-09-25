using commonItems;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.UnitTests.TestHelpers;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Provinces;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ProvinceHistoryTests {
	private readonly Date ck3BookmarkDate = new(867, 1, 1);
	[Fact]
	public void FieldsDefaultToCorrectValues() {
		var province = new Province(1);
		Assert.Null(province.GetCultureId(ck3BookmarkDate));
		Assert.Null(province.GetFaithId(ck3BookmarkDate));
		Assert.Equal("none", province.GetHoldingType(ck3BookmarkDate));
		Assert.Empty(province.GetBuildings(ck3BookmarkDate));
	}

	[Fact]
	public void DetailsCanBeLoadedFromStream() {
		var reader = new BufferedReader("""
			= {
				religion = orthodox
				random_param = random_stuff
				culture = roman
			}
		""");
		var province = new Province(1, reader);

		Assert.Equal("roman", province.GetCultureId(ck3BookmarkDate));
		Assert.Equal("orthodox", province.GetFaithId(ck3BookmarkDate));
	}

	[Fact]
	public void DetailsAreLoadedFromDatedBlocks() {
		var reader = new BufferedReader("""
			= {
				religion = catholic
				random_param = random_stuff
				culture = roman
				850.1.1 = { religion=orthodox holding=castle_holding }
			}
		""");
		var province = new Province(1, reader);

		Assert.Equal("castle_holding", province.GetHoldingType(ck3BookmarkDate));
		Assert.Equal("orthodox", province.GetFaithId(ck3BookmarkDate));
	}

	[Fact]
	public void CultureFaithAndTerrainDetailsCanCopiedFromOtherProvince() {		var reader = new BufferedReader("""
			= {
				religion = catholic
				culture = roman
				terrain = arctic
				buildings = { orchard tavern }
				850.1.1 = { religion=orthodox holding=castle_holding }
			}
		""");
		var province1 = new Province(1, reader);
		var province2 = new Province(2);
		province2.CopyEntriesFromProvince(province1);

		// Only culture, faith and terrain should be copied from source province.
		Assert.Equal("orthodox", province2.GetFaithId(ck3BookmarkDate));
		Assert.Equal("roman", province2.GetCultureId(ck3BookmarkDate));
		Assert.Equal("arctic", province2.History.GetFieldValue("terrain", ck3BookmarkDate));
		Assert.Equal("none", province2.GetHoldingType(ck3BookmarkDate));
		Assert.Empty(province2.GetBuildings(ck3BookmarkDate));
	}

	[Fact]
	public void QuotedFaithCultureAndHoldingAreLoadedFromStream() {
		var reader = new BufferedReader("""
			= {
				religion = "orthodox"
				culture = "roman"
				holding = "castle_holding"
			}
		""");
		var province = new Province(1, reader);

		// Quoted values are loaded as StringOfItem; quotes are preserved except for culture.
		Assert.Equal("\"orthodox\"", province.GetFaithId(ck3BookmarkDate));
		Assert.Equal("roman", province.GetCultureId(ck3BookmarkDate));
		Assert.Equal("\"castle_holding\"", province.GetHoldingType(ck3BookmarkDate));
	}

	[Fact]
	public void FaithIdCanBeSetAndOverrideExistingEntries() {
		var province = new Province(1);
		province.SetFaithId("old_faith", null);
		province.SetFaithId("older_faith", ck3BookmarkDate);

		province.SetFaithIdAndOverrideExistingEntries("new_faith");

		Assert.Equal("new_faith", province.GetFaithId(ck3BookmarkDate));
	}

	[Fact]
	public void BuildingsCanBeSetAndRetrieved() {
		var province = new Province(1);

		province.SetBuildings(new List<string> { "temple", "aqueduct" }, ck3BookmarkDate);

		Assert.Equal(new List<string> { "temple", "aqueduct" }, province.GetBuildings(ck3BookmarkDate));
	}

	[Fact]
	public void BuildingsLoadedFromStreamAreRetrieved() {
		var reader = new BufferedReader("""
			= {
				buildings = { orchard tavern }
			}
		""");
		var province = new Province(1, reader);

		Assert.Equal(new List<string> { "orchard", "tavern" }, province.GetBuildings(ck3BookmarkDate));
	}

	[Fact]
	public void QuotedBuildingsLoadedFromStreamAreRetrieved() {
		var reader = new BufferedReader("""
			= {
				buildings = { "orchard" "tavern" }
			}
		""");
		var province = new Province(1, reader);

		Assert.Equal(new List<string> { "orchard", "tavern" }, province.GetBuildings(ck3BookmarkDate));
	}

	[Fact]
	public void InvalidBuildingsValueReturnsEmpty() {
		var reader = new BufferedReader("""
			= {
				buildings = orchard
			}
		""");
		var province = new Province(1, reader);

		Assert.Empty(province.GetBuildings(ck3BookmarkDate));
	}

	[Fact]
	public void GetCulture_ReturnsNullWhenProvinceHasNoCulture() {
		var province = new Province(1);
		var cultures = new TestCK3CultureCollection();

		Assert.Null(province.GetCulture(ck3BookmarkDate, cultures));
	}

	[Fact]
	public void GetCulture_ReturnsCultureWhenItExists() {
		var province = new Province(1);
		province.SetCultureId("greek", null);
		var cultures = new TestCK3CultureCollection();
		cultures.GenerateTestCulture("greek");

		Assert.Equal("greek", province.GetCulture(ck3BookmarkDate, cultures)?.Id);
	}

	[Fact]
	public void GetCulture_ReturnsNullAndWarnsWhenCultureIsMissing() {
		var province = new Province(1);
		province.SetCultureId("missing_culture", null);
		var cultures = new TestCK3CultureCollection();

		var output = new System.IO.StringWriter();
		System.Console.SetOut(output);
		Assert.Null(province.GetCulture(ck3BookmarkDate, cultures));
		Assert.Contains("Culture with ID missing_culture not found!", output.ToString());
	}

	[Fact]
	public void CopyEntriesFromProvince_KeepsExistingDatedEntries() {
		var source = new Province(1, new BufferedReader("""
			= {
				culture = source_culture
				religion = source_faith
			}
		"""));
		var target = new Province(2);
		target.SetCultureId("target_culture", ck3BookmarkDate);

		target.CopyEntriesFromProvince(source);

		// Target had a dated culture entry, so it keeps its own culture but copies faith.
		Assert.Equal("target_culture", target.GetCultureId(ck3BookmarkDate));
		Assert.Equal("source_faith", target.GetFaithId(ck3BookmarkDate));
		Assert.Equal((ulong)1, target.BaseProvinceId);
	}

	[Fact]
	public void CopyEntriesFromProvince_KeepsExistingInitialEntries() {
		var source = new Province(1, new BufferedReader("""
			= {
				culture = source_culture
				religion = source_faith
			}
		"""));
		var target = new Province(2);
		target.SetFaithId("target_faith", date: null);

		target.CopyEntriesFromProvince(source);

		// Target had an initial faith entry, so it keeps its own faith but copies culture.
		Assert.Equal("source_culture", target.GetCultureId(ck3BookmarkDate));
		Assert.Equal("target_faith", target.GetFaithId(ck3BookmarkDate));
	}

	[Fact]
	public void StringOfItemValuesAreReturned() {
		var province = new Province(1);
		province.History.AddFieldValue(ck3BookmarkDate, "faith", "faith", new StringOfItem("orthodox"));
		province.History.AddFieldValue(ck3BookmarkDate, "culture", "culture", new StringOfItem("roman"));
		province.History.AddFieldValue(ck3BookmarkDate, "holding", "holding", new StringOfItem("castle_holding"));

		Assert.Equal("orthodox", province.GetFaithId(ck3BookmarkDate));
		Assert.Equal("roman", province.GetCultureId(ck3BookmarkDate));
		Assert.Equal("castle_holding", province.GetHoldingType(ck3BookmarkDate));
	}

	[Fact]
	public void ObjectListBuildingsAreRetrieved() {
		var province = new Province(1);
		province.History.AddFieldValue(ck3BookmarkDate, "buildings", "buildings",
			new List<object> { "temple", "aqueduct" });

		Assert.Equal(new List<string> { "temple", "aqueduct" }, province.GetBuildings(ck3BookmarkDate));
	}

	[Fact]
	public void StringOfItemListBuildingsAreRetrieved() {
		var province = new Province(1);
		province.History.AddFieldValue(ck3BookmarkDate, "buildings", "buildings",
			new List<StringOfItem> { new("temple"), new("aqueduct") });

		Assert.Equal(new List<string> { "temple", "aqueduct" }, province.GetBuildings(ck3BookmarkDate));
	}

	[Fact]
	public void UnexpectedHoldingTypeReturnsNull() {
		var province = new Province(1);
		province.History.AddFieldValue(ck3BookmarkDate, "holding", "holding", 5);

		Assert.Null(province.GetHoldingType(ck3BookmarkDate));
	}
}