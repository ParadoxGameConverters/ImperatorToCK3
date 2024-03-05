using commonItems;
using commonItems.Colors;
using ImperatorToCK3.CK3.Cultures;
using System.Linq;

namespace ImperatorToCK3.UnitTests.TestHelpers; 

public class TestCK3CultureCollection() : CultureCollection(new ColorFactory(), TestCulturalPillars) {
	private static readonly PillarCollection TestCulturalPillars = new(new ColorFactory());
	
	public void LoadConverterPillars(string filePath) {
		TestCulturalPillars.LoadConverterPillars(filePath);
	}

	public void GenerateTestCulture(string id, string heritageId = "test_heritage") {
		const string nameListId = "name_list_test";
		var nameList = new NameList(nameListId, new BufferedReader());
		
		var heritage = TestCulturalPillars.Heritages.FirstOrDefault(p => p.Id == heritageId);
		if (heritage is null) {
			heritage = new Pillar(heritageId, new PillarData { Type = "heritage" });
			TestCulturalPillars.Add(heritage);
		}
		
		var cultureData = new CultureData {
			Heritage = heritage,
			NameLists = {nameList}
		};
		var culture = new Culture(id, cultureData);
		Add(culture);
	}
}