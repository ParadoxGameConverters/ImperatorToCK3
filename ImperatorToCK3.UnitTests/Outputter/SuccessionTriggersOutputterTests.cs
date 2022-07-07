using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Outputter;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class SuccessionTriggersOutputterTests {
	[Fact]
	public void PrimogenitureAndSeniorityTriggersAreOutputted() {
		const string outputModName = "outputMod";
		var outputFilePath = Path.Combine(
			"output",
			outputModName,
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

		SuccessionTriggersOutputter.OutputSuccessionTriggers(outputModName, titles, date);

		using var file = File.OpenRead(outputFilePath);
		var reader = new StreamReader(file);
		Assert.Equal("historical_succession_access_single_heir_succession_law_trigger={", reader.ReadLine());
		Assert.Equal("\tOR={", reader.ReadLine());
		Assert.Equal($"\t\thas_title=title:{kingdomPrimogeniture.Id}", reader.ReadLine());
		Assert.Equal("\t}", reader.ReadLine());
		Assert.Equal("}", reader.ReadLine());
		Assert.Equal("historical_succession_access_single_heir_dynasty_house_trigger={", reader.ReadLine());
		Assert.Equal("\tOR={", reader.ReadLine());
		Assert.Equal($"\t\thas_title=title:{kingdomSeniority.Id}", reader.ReadLine());
		Assert.Equal("\t}", reader.ReadLine());
		Assert.Equal("}", reader.ReadLine());
		Assert.True(reader.EndOfStream);
	}
}