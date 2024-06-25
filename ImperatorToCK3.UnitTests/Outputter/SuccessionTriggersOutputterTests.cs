using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Outputter;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class SuccessionTriggersOutputterTests {
	[Fact]
	public async Task PrimogenitureAndSeniorityTriggersAreOutputted() {
		const string outputModPath = "output/outputMod";
		var outputFilePath = Path.Combine(
			outputModPath,
			"common",
			"scripted_triggers",
			"IRToCK3_succession_triggers.txt"
		);

		var date = new Date(476, 1, 1);
		var titles = new Title.LandedTitles();

		var kingdomPrimogeniture = titles.Add("k_kingdom1");
		kingdomPrimogeniture.History.AddFieldValue(date,"succession_laws", "succession_laws", new List<string> { "single_heir_succession_law" });

		var kingdomSeniority = titles.Add("k_kingdom2");
		kingdomSeniority.History.AddFieldValue(date,"succession_laws", "succession_laws", new List<string> { "single_heir_dynasty_house" });

		var vassal = titles.Add("d_vassal");
		vassal.History.AddFieldValue(date,"succession_laws", "succession_laws", new List<string> { "single_heir_succession_law" });
		vassal.SetDeFactoLiege(kingdomPrimogeniture, date); // has de facto liege, will not be added to the trigger

		SystemUtils.TryCreateFolder(CommonFunctions.GetPath(outputFilePath));

		await SuccessionTriggersOutputter.OutputSuccessionTriggers(outputModPath, titles, date);

		await using var file = File.OpenRead(outputFilePath);
		var reader = new StreamReader(file);
		Assert.Equal("historical_succession_access_single_heir_succession_law_trigger={", await reader.ReadLineAsync());
		Assert.Equal("\tOR={", await reader.ReadLineAsync());
		Assert.Equal("\t\thas_title=title:k_kingdom1", await reader.ReadLineAsync());
		Assert.Equal("\t}", await reader.ReadLineAsync());
		Assert.Equal("}", await reader.ReadLineAsync());
		Assert.Equal("historical_succession_access_single_heir_dynasty_house_trigger={", await reader.ReadLineAsync());
		Assert.Equal("\tOR={", await reader.ReadLineAsync());
		Assert.Equal("\t\thas_title=title:k_kingdom2", await reader.ReadLineAsync());
		Assert.Equal("\t}", await reader.ReadLineAsync());
		Assert.Equal("}", await reader.ReadLineAsync());
		Assert.True(reader.EndOfStream);
	}
}