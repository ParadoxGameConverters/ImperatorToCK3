using commonItems;
using commonItems.Colors;
using ImperatorToCK3.CK3.Cultures;
using System.Linq;

namespace ImperatorToCK3.UnitTests.TestHelpers; 

public class TestCK3CultureCollection() : CultureCollection(new ColorFactory(), TestCulturalPillars) {
	private static readonly PillarCollection TestCulturalPillars = new();
	
	static TestCK3CultureCollection() {
		TestCulturalPillars.Add(new Pillar("test_heritage", new PillarData { Type = "heritage" }));
	}

	public void GenerateTestCulture(string id) {
		const string nameListId = "name_list_test";
		var nameList = new NameList(nameListId, new BufferedReader());
		var cultureData = new CultureData {
			Heritage = TestCulturalPillars.Heritages.First(p => p.Id == "test_heritage"),
			NameLists = {nameList}
		};
		var culture = new Culture(id, cultureData);
		Add(culture);
	}
}