using ImperatorToCK3.Mappers.HolySiteEffect;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.HolySiteEffect;

public class HolySiteEffectMapperTests {
	private const string MappingsFilePath = "TestFiles/HolySiteEffectMapperTests/mappings.txt";

	[Fact]
	public void UnknownEffectHasNoMatch() {
		var mapper = new HolySiteEffectMapper(MappingsFilePath);
		Assert.Null(mapper.Match("wrong_effect", 1));
	}

	[Fact]
	public void BlockMappingsProduceModifiersInAllFourBlocks() {
		var mapper = new HolySiteEffectMapper(MappingsFilePath);

		var matches = mapper.Match("block_effect", 1);

		Assert.NotNull(matches);
		Assert.Equal(5, matches.Count);

		Assert.Contains(matches, m =>
			m.BlockKeyword == MappedHolySiteModifier.FaithCharacterBlock &&
			m.Modifier == "faith_mod" &&
			m.Value == 10 &&
			m.Scale is null);
		Assert.Contains(matches, m =>
			m.BlockKeyword == MappedHolySiteModifier.FaithCharacterScaledBlock &&
			m.Modifier == "faith_scaled_mod" &&
			m.Value == 0.2 &&
			m.Scale == "holy_site_total_artifact_rarity");
		Assert.Contains(matches, m =>
			m.BlockKeyword == MappedHolySiteModifier.FaithCharacterScaledBlock &&
			m.Modifier == "faith_scaled_mod2" &&
			m.Value == 0.4 &&
			m.Scale == "holy_site_total_artifact_rarity");
		Assert.Contains(matches, m =>
			m.BlockKeyword == MappedHolySiteModifier.CountyHolderCharacterBlock &&
			m.Modifier == "county_holder_mod" &&
			m.Value == 5 &&
			m.Scale is null);
		Assert.Contains(matches, m =>
			m.BlockKeyword == MappedHolySiteModifier.CountyScaledBlock &&
			m.Modifier == "county_scaled_mod" &&
			m.Value == 0.2 &&
			m.Scale == "holy_site_total_artifact_rarity");
	}
}
