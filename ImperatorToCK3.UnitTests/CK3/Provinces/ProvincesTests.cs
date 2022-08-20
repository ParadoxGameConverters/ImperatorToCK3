using commonItems;
using commonItems.Mods;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Provinces; 

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class ProvincesTests {
	private const string CK3Root = "TestFiles/CK3ProvincesTests";
	private readonly ModFilesystem ck3ModFs = new(CK3Root, new List<Mod>());
	private Date ck3BookmarkDate = new("867.1.1");
	
	[Fact]
	public void ProvincesDefaultToEmpty() {
		var provinces = new ImperatorToCK3.CK3.Provinces.ProvinceCollection();

		Assert.Empty(provinces);
	}

	[Fact]
	public void ProvincesAreProperlyLoadedFromFilesystem() {
		var provinces = new ImperatorToCK3.CK3.Provinces.ProvinceCollection(ck3ModFs);

		Assert.Collection(provinces.OrderBy(p=>p.Id),
			prov => {
				Assert.Equal((ulong)3080, prov.Id);
				Assert.Equal("slovien", prov.GetCultureId(ck3BookmarkDate));
				Assert.Equal("catholic", prov.GetFaithId(ck3BookmarkDate));
			},
			prov => {
				Assert.Equal((ulong)4125, prov.Id);
				Assert.Equal("czech", prov.GetCultureId(ck3BookmarkDate));
				Assert.Equal("slavic_pagan", prov.GetFaithId(ck3BookmarkDate));
			},
			prov => {
				Assert.Equal((ulong)4161, prov.Id);
				Assert.Equal("czech", prov.GetCultureId(ck3BookmarkDate));
				Assert.Equal("slavic_pagan", prov.GetFaithId(ck3BookmarkDate));
			},
			prov => {
				Assert.Equal((ulong)4165, prov.Id);
				Assert.Equal("slovien", prov.GetCultureId(ck3BookmarkDate));
				Assert.Equal("catholic", prov.GetFaithId(ck3BookmarkDate));
			}
		);
	}
}