using commonItems;
using commonItems.Colors;
using commonItems.Serialization;
using AwesomeAssertions;
using ImperatorToCK3.CK3.Religions;
using ImperatorToCK3.CK3.Titles;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Religions;

public class RiteTests {
	private static Religion CreateTestReligion(out ReligionCollection religions) {
		religions = new ReligionCollection(new Title.LandedTitles());
		return new Religion("test_religion", new BufferedReader("{}"), religions);
	}

	[Fact]
	public void RiteIsParsedAndSerialized() {
		CreateTestReligion(out var religions);
		var reader = new BufferedReader("""
			{
				faith = test_faith
				color = rgb { 10 20 30 }
				icon = custom_icon
				tenets = { tenet_a }
				doctrines = { doctrine_a }
				cultures = { roman }
			}
			""");
		var rite = new Rite("test_rite", reader, religions, new ColorFactory());

		Assert.Equal("test_rite", rite.Id);
		Assert.Equal("test_faith", rite.FaithId);
		Assert.Equal(["tenet_a", "doctrine_a"], rite.DoctrineIds);

		var serialized = PDXSerializer.Serialize(rite);
		serialized.Should().ContainAll(
			"faith = test_faith",
			"color = rgb { 10 20 30 }",
			"tenets = {",
			"tenet_a",
			"doctrines = {",
			"doctrine_a",
			"icon = custom_icon",
			"cultures = { roman }"
		);
	}

	[Fact]
	public void MainlineRiteIsCreatedFromFaith() {
		var religion = CreateTestReligion(out _);
		var faithData = new FaithData {Color = new Color(0.25, 0.5, 0.75)};
		faithData.DoctrineIds.Add("tenet_test");
		faithData.DoctrineIds.Add("doctrine_test");
		var faith = new Faith("test_faith", faithData, religion);

		var rite = Rite.CreateMainlineRite(faith);

		Assert.Equal("test_faith", rite.Id);
		Assert.Equal("test_faith", rite.FaithId);
		Assert.Equal(faith.Color, rite.Color);
		Assert.Equal(["tenet_test", "doctrine_test"], rite.DoctrineIds);
	}

	[Fact]
	public void SerializedRiteCanBeParsedBack() {
		CreateTestReligion(out var religions);
		var reader = new BufferedReader("""
			{
				faith = test_faith
				color = rgb { 10 20 30 }
				tenets = { tenet_a }
				doctrines = { doctrine_a }
			}
			""");
		var rite = new Rite("test_rite", reader, religions, new ColorFactory());

		var serialized = rite.Serialize(indent: "", withBraces: true);
		var parsed = new Rite("test_rite", new BufferedReader(serialized), religions, new ColorFactory());

		Assert.Equal("test_faith", parsed.FaithId);
		Assert.Equal(["tenet_a", "doctrine_a"], parsed.DoctrineIds);
	}
}
