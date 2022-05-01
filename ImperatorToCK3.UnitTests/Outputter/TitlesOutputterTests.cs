using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Outputter;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class TitlesOutputterTests {
	[Fact]
	public void TitlesAreOutputted() {
		const string outputModName = "outputMod";

		var titles = new Title.LandedTitles();
		var kingdom = titles.Add("k_kingdom");
		var history = new History();
		history.AddFieldValue(new Date(20, 1, 1), "liege", "liege", 0);
		var kingdomHistory = new TitleHistory(history);
		kingdom.AddHistory(kingdomHistory);

		var duchy = titles.Add("d_duchy");
		duchy.DeJureLiege = kingdom;

		var county = titles.Add("c_county");
		county.DeJureLiege = duchy;

		var barony = titles.Add("b_barony");
		barony.DeJureLiege = county;

		var specialTitle = titles.Add("k_special_title");
		var specialHistory = new History();
		specialHistory.AddFieldValue( new Date(20, 1, 1), "holder", "holder", "bob_42");
		var specialTitleHistory = new TitleHistory(specialHistory);
		specialTitle.AddHistory(specialTitleHistory);

		var titleHistoryPath = Path.Combine("output", outputModName, "history", "titles");
		var kingdomHistoryPath = Path.Combine(titleHistoryPath, "k_kingdom.txt");
		var otherTitlesHistoryPath = Path.Combine(titleHistoryPath, "00_other_titles.txt");
		SystemUtils.TryCreateFolder(titleHistoryPath);

		var landedTitlesPath = Path.Combine("output", outputModName, "common", "landed_titles", "00_landed_titles.txt");
		SystemUtils.TryCreateFolder(CommonFunctions.GetPath(landedTitlesPath));

		TitlesOutputter.OutputTitles(outputModName, titles, IMPERATOR_DE_JURE.NO);

		Assert.True(File.Exists(kingdomHistoryPath));
		using var kingdomHistoryFile = File.OpenRead(kingdomHistoryPath);
		var reader = new StreamReader(kingdomHistoryFile);
		Assert.Equal("k_kingdom={", reader.ReadLine());
		Assert.Equal("\t20.1.1={ liege=0 }", reader.ReadLine());
		Assert.Equal("}", reader.ReadLine());
		Assert.True(reader.EndOfStream);

		Assert.True(File.Exists(otherTitlesHistoryPath));
		using var otherTitlesHistoryFile = File.OpenRead(otherTitlesHistoryPath);
		reader = new StreamReader(otherTitlesHistoryFile);
		Assert.Equal("k_special_title={", reader.ReadLine());
		Assert.Equal("\t20.1.1={ holder=\"bob_42\" }", reader.ReadLine());
		Assert.Equal("}", reader.ReadLine());
		Assert.True(reader.EndOfStream);

		Assert.True(File.Exists(landedTitlesPath));
		using var landedTitlesFile = File.OpenRead(landedTitlesPath);
		reader = new StreamReader(landedTitlesFile);
		Assert.Equal("k_kingdom={", reader.ReadLine());
		Assert.Equal("\td_duchy={", reader.ReadLine());
		Assert.Equal("\t\tc_county={", reader.ReadLine());
		Assert.Equal("\t\t\tb_barony={", reader.ReadLine());
		Assert.Equal("\t\t\t\tlandless=no", reader.ReadLine());
		Assert.Equal("\t\t\t\tdefinite_form=no", reader.ReadLine());
		Assert.Equal("\t\t\t\truler_uses_title_name=no", reader.ReadLine());
		Assert.Equal("\t\t\t}", reader.ReadLine());
		Assert.Equal("\t\t\tlandless=no", reader.ReadLine());
		Assert.Equal("\t\t\tdefinite_form=no", reader.ReadLine());
		Assert.Equal("\t\t\truler_uses_title_name=no", reader.ReadLine());
		Assert.Equal("\t\t}", reader.ReadLine());
		Assert.Equal("\t\tlandless=no", reader.ReadLine());
		Assert.Equal("\t\tdefinite_form=no", reader.ReadLine());
		Assert.Equal("\t\truler_uses_title_name=no", reader.ReadLine());
		Assert.Equal("\t}", reader.ReadLine());
		Assert.Equal("\tlandless=no", reader.ReadLine());
		Assert.Equal("\tdefinite_form=no", reader.ReadLine());
		Assert.Equal("\truler_uses_title_name=no", reader.ReadLine());
		Assert.Equal("}", reader.ReadLine());
		Assert.Equal("k_special_title={", reader.ReadLine());
		Assert.Equal("\tlandless=no", reader.ReadLine());
		Assert.Equal("\tdefinite_form=no", reader.ReadLine());
		Assert.Equal("\truler_uses_title_name=no", reader.ReadLine());
		Assert.Equal("}", reader.ReadLine());
		Assert.True(reader.EndOfStream);
	}

	[Fact]
	public void VariablesAreOutputted() {
		const string outputModName = "outputMod2";
		var titles = new Title.LandedTitles();
		titles.Variables.Add("default_ai_priority", 20);
		titles.Variables.Add("default_ai_aggressiveness", 40);

		var titleHistoryPath = Path.Combine("output", outputModName, "history", "titles");
		SystemUtils.TryCreateFolder(titleHistoryPath);
		var landedTitlesPath = Path.Combine("output", outputModName, "common", "landed_titles", "00_landed_titles.txt");
		SystemUtils.TryCreateFolder(CommonFunctions.GetPath(landedTitlesPath));

		TitlesOutputter.OutputTitles(outputModName, titles, IMPERATOR_DE_JURE.NO);

		Assert.True(File.Exists(landedTitlesPath));
		using var landedTitlesFile = File.OpenRead(landedTitlesPath);
		var reader = new StreamReader(landedTitlesFile);
		Assert.Equal("@default_ai_priority=20", reader.ReadLine());
		Assert.Equal("@default_ai_aggressiveness=40", reader.ReadLine());
		Assert.True(reader.EndOfStream);
	}
}