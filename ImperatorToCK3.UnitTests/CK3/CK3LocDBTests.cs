using ImperatorToCK3.CK3;
using ImperatorToCK3.UnitTests.TestHelpers;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3;

public class CK3LocDBTests {
	[Theory]
	// https://en.wikipedia.org/wiki/MurmurHash
	[InlineData("", 0)]
	[InlineData("test", 0xba6bd213)]
	[InlineData("Hello, world!", 0xc0363e43)]
	[InlineData("The quick brown fox jumps over the lazy dog", 0x2e4ff723)]
	public void MurmurHashIsCorrectlyCalculated(string key, uint expectedHash) {
		var actualHash = CK3LocDB.GetHashForKey(key);
		Assert.Equal(expectedHash, actualHash);
	}

	[Theory]
	[InlineData("Mallobald", "laamp_base_contract_schemes.2541.e.tt.employer_has_trait.paranoid")]
	[InlineData("dynn_Hkeng", "debug_min_popular_opinion_modifier")]
	[InlineData("b_hinggan_adj", "grand_wedding_completed_guest")]
	[InlineData("c_biak_adj", "b_celtzene")]
	[InlineData("b_molungr_adj", "c_somkhiti")]
	[InlineData("BrewPositiveAdjectiveSpectacular", "duchy_theo_cath_andalusian")]
	[InlineData("childhood.2200.desc", "b_dezful_adj")]
	[InlineData("khabzism_devoteeplural", "caballero_flavor")]
	[InlineData("building_nishapur_mines_02", "k_IRTOCK3_ATV_adj")]
	public void HashCollisionsAreDetected(string key1, string key2) {
		 var locDB = new TestCK3LocDB();
		 locDB.AddLocForLanguage(key1, language: "english", string.Empty);
		 Assert.True(locDB.KeyHasConflictingHash(key2));
	}
	
	[Theory]
	[InlineData("a", "b")]
	[InlineData("key1", "key2")]
	[InlineData("Mallobald", "laamp_base_contract_schemes.2541")]
	[InlineData("dynn_Hkeng", "dynn_Heng")]
	[InlineData("b_hinggan_adj", "b_hinggan_adj2")]
	[InlineData("c_biak_adj", "c_biak_adj2")]
	[InlineData("b_molungr_adj", "b_molungr_adj2")]
	[InlineData("BrewPositiveAdjectiveSpectacular", "BrewPositiveAdjectiveSpectacular2")]
	[InlineData("childhood.2200.desc", "childhood.2200.desc2")]
	[InlineData("khabzism_devoteeplural", "khabzism_devoteeplural2")]
	public void FalseHashCollisionsAreNotDetected(string key1, string key2) {
		 var locDB = new TestCK3LocDB();
		 locDB.AddLocForLanguage(key1, language: "english", string.Empty);
		 Assert.False(locDB.KeyHasConflictingHash(key2));
	}
}