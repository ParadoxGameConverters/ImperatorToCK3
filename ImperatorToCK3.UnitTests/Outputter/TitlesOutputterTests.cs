using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class TitlesOutputterTests {
	[Fact]
	public async Task TitlesAreOutputted() {
		const string outputModPath = "output/outputMod";

		var titles = new Title.LandedTitles();
		var kingdom = titles.Add("k_kingdom");
		kingdom.History.AddFieldValue(new Date(20, 1, 1), "liege", "liege", 0);

		var duchy = titles.Add("d_duchy");
		duchy.DeJureLiege = kingdom;

		var county = titles.Add("c_county");
		county.DeJureLiege = duchy;

		var barony = titles.Add("b_barony");
		barony.DeJureLiege = county;

		var specialTitle = titles.Add("k_special_title");
		specialTitle.History.AddFieldValue(new Date(20, 1, 1), "holder", "holder", "bob_42");

		var titleHistoryPath = Path.Combine(outputModPath, "history", "titles");
		var kingdomHistoryPath = Path.Combine(titleHistoryPath, "k_kingdom.txt");
		var otherTitlesHistoryPath = Path.Combine(titleHistoryPath, "00_other_titles.txt");
		SystemUtils.TryCreateFolder(titleHistoryPath);

		var landedTitlesPath = Path.Combine(outputModPath, "common", "landed_titles", "00_landed_titles.txt");
		SystemUtils.TryCreateFolder(CommonFunctions.GetPath(landedTitlesPath));

		await TitlesOutputter.OutputTitles(outputModPath, titles);

		Assert.True(File.Exists(kingdomHistoryPath));
		var kingdomHistoryText = await File.ReadAllTextAsync(kingdomHistoryPath);
		Assert.Equal("k_kingdom={\n\t20.1.1 = { liege = 0 }\n}\n", TextTestUtils.NormalizeNewlines(kingdomHistoryText));

		Assert.True(File.Exists(otherTitlesHistoryPath));
		var otherTitlesHistoryText = await File.ReadAllTextAsync(otherTitlesHistoryPath);
		Assert.Equal("k_special_title={\n\t20.1.1 = { holder = bob_42 }\n}\n", TextTestUtils.NormalizeNewlines(otherTitlesHistoryText));

		Assert.True(File.Exists(landedTitlesPath));
		var landedTitlesText = await File.ReadAllTextAsync(landedTitlesPath);
		var expectedLandedTitles = TextTestUtils.NormalizeNewlines("""
		k_kingdom = {
			d_duchy = {
				c_county = {
					b_barony = {
						landless = no
						definite_form = no
						ruler_uses_title_name = no
					}
					landless = no
					definite_form = no
					ruler_uses_title_name = no
				}
				landless = no
				definite_form = no
				ruler_uses_title_name = no
			}
			landless = no
			definite_form = no
			ruler_uses_title_name = no
		}
		k_special_title = {
			landless = no
			definite_form = no
			ruler_uses_title_name = no
		}
		""");
		Assert.Equal(expectedLandedTitles, TextTestUtils.NormalizeNewlines(landedTitlesText));
	}

	[Fact]
	public async Task VariablesAreOutputted() {
		const string outputModPath = "output/outputMod2";
		var titles = new Title.LandedTitles();
		titles.Variables.Add("default_ai_priority", 20);
		titles.Variables.Add("default_ai_aggressiveness", 40);

		var titleHistoryPath = Path.Combine(outputModPath, "history", "titles");
		SystemUtils.TryCreateFolder(titleHistoryPath);
		var landedTitlesPath = Path.Combine(outputModPath, "common", "landed_titles", "00_landed_titles.txt");
		SystemUtils.TryCreateFolder(CommonFunctions.GetPath(landedTitlesPath));

		await TitlesOutputter.OutputTitles(outputModPath, titles);

		Assert.True(File.Exists(landedTitlesPath));
		var landedTitlesText = await File.ReadAllTextAsync(landedTitlesPath);
		var expectedVariables = TextTestUtils.NormalizeNewlines("""
		@default_ai_priority=20
		@default_ai_aggressiveness=40
		""");
		Assert.Equal(expectedVariables, TextTestUtils.NormalizeNewlines(landedTitlesText).TrimEnd());
	}
}