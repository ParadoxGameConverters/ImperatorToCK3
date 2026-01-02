using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
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

		var actualText = TextTestUtils.NormalizeNewlines(await File.ReadAllTextAsync(outputFilePath));
		var expectedText = TextTestUtils.NormalizeNewlines(
			"""
			historical_succession_access_single_heir_succession_law_trigger={
				OR={
					has_title=title:k_kingdom1
				}
			}
			historical_succession_access_single_heir_dynasty_house_trigger={
				OR={
					has_title=title:k_kingdom2
				}
			}
			
			"""
		);

		Assert.Equal(expectedText, actualText);
	}
}