using commonItems;
using ImperatorToCK3.CK3.Provinces;
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
		Assert.Equal("orthodox",province.GetFaithId(ck3BookmarkDate));
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
	public void CultureFaithAndTerrainDetailsCanCopiedFromOtherProvince() {
		var reader = new BufferedReader("""
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
}