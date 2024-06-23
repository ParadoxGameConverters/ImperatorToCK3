using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Outputter;
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
		await using var kingdomHistoryFile = File.OpenRead(kingdomHistoryPath);
		var reader = new StreamReader(kingdomHistoryFile);
		Assert.Equal("k_kingdom={", await reader.ReadLineAsync());
		Assert.Equal("\t20.1.1 = { liege = 0 }", await reader.ReadLineAsync());
		Assert.Equal("}", await reader.ReadLineAsync());
		Assert.True(reader.EndOfStream);

		Assert.True(File.Exists(otherTitlesHistoryPath));
		await using var otherTitlesHistoryFile = File.OpenRead(otherTitlesHistoryPath);
		reader = new StreamReader(otherTitlesHistoryFile);
		Assert.Equal("k_special_title={", await reader.ReadLineAsync());
		Assert.Equal("\t20.1.1 = { holder = bob_42 }", await reader.ReadLineAsync());
		Assert.Equal("}", await reader.ReadLineAsync());
		Assert.True(reader.EndOfStream);

		Assert.True(File.Exists(landedTitlesPath));
		await using var landedTitlesFile = File.OpenRead(landedTitlesPath);
		reader = new StreamReader(landedTitlesFile);
		Assert.Equal("k_kingdom = {", await reader.ReadLineAsync());
		Assert.Equal("\td_duchy = {", await reader.ReadLineAsync());
		Assert.Equal("\t\tc_county = {", await reader.ReadLineAsync());
		Assert.Equal("\t\t\tb_barony = {", await reader.ReadLineAsync());
		Assert.Equal("\t\t\t\tlandless = no", await reader.ReadLineAsync());
		Assert.Equal("\t\t\t\tdefinite_form = no", await reader.ReadLineAsync());
		Assert.Equal("\t\t\t\truler_uses_title_name = no", await reader.ReadLineAsync());
		Assert.Equal("\t\t\t}", await reader.ReadLineAsync());
		Assert.Equal("\t\t\tlandless = no", await reader.ReadLineAsync());
		Assert.Equal("\t\t\tdefinite_form = no", await reader.ReadLineAsync());
		Assert.Equal("\t\t\truler_uses_title_name = no", await reader.ReadLineAsync());
		Assert.Equal("\t\t}", await reader.ReadLineAsync());
		Assert.Equal("\t\tlandless = no", await reader.ReadLineAsync());
		Assert.Equal("\t\tdefinite_form = no", await reader.ReadLineAsync());
		Assert.Equal("\t\truler_uses_title_name = no", await reader.ReadLineAsync());
		Assert.Equal("\t}", await reader.ReadLineAsync());
		Assert.Equal("\tlandless = no", await reader.ReadLineAsync());
		Assert.Equal("\tdefinite_form = no", await reader.ReadLineAsync());
		Assert.Equal("\truler_uses_title_name = no", await reader.ReadLineAsync());
		Assert.Equal("}", await reader.ReadLineAsync());
		Assert.Equal("k_special_title = {", await reader.ReadLineAsync());
		Assert.Equal("\tlandless = no", await reader.ReadLineAsync());
		Assert.Equal("\tdefinite_form = no", await reader.ReadLineAsync());
		Assert.Equal("\truler_uses_title_name = no", await reader.ReadLineAsync());
		Assert.Equal("}", await reader.ReadLineAsync());
		Assert.True(reader.EndOfStream);
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
		await using var landedTitlesFile = File.OpenRead(landedTitlesPath);
		var reader = new StreamReader(landedTitlesFile);
		Assert.Equal("@default_ai_priority=20", await reader.ReadLineAsync());
		Assert.Equal("@default_ai_aggressiveness=40", await reader.ReadLineAsync());
		Assert.True(reader.EndOfStream);
	}
}