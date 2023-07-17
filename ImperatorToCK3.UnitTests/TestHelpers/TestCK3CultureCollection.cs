using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using ImperatorToCK3.CK3.Cultures;

namespace ImperatorToCK3.UnitTests.TestHelpers; 

public class TestCK3CultureCollection() : CultureCollection(TestCulturalPillars) {
	private static readonly PillarCollection TestCulturalPillars = new();
	
	static TestCK3CultureCollection() {
		TestCulturalPillars.Add(new Pillar("test_heritage", new BufferedReader("type = heritage")));
	}

	public void GenerateTestCulture(string id) {
		const string nameListId = "name_list_test";
		var nameList = new NameList(nameListId, new BufferedReader());
		var culture = new Culture(
			id,
			new BufferedReader($"heritage=test_heritage name_list={nameListId}"),
			TestCulturalPillars,
			new IdObjectCollection<string, NameList> {nameList},
			new ColorFactory()
		);
		Add(culture);
	}
}