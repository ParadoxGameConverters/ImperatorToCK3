using commonItems;
using commonItems.Colors;
using ImperatorToCK3.CK3.Cultures;

namespace ImperatorToCK3.UnitTests.TestHelpers; 

public class TestCK3CultureCollection() : CultureCollection(colorFactory, new PillarCollection(colorFactory)) {
	private static readonly ColorFactory colorFactory = new();
	
	public void LoadConverterPillars(string filePath) {
		PillarCollection.LoadConverterPillars(filePath);
	}

	public void AddNameList(NameList nameList) {
		NameListCollection.Add(nameList);
	}

	public void AddPillar(Pillar pillar) {
		PillarCollection.Add(pillar);
	}

	public void GenerateTestCulture(string id, string heritageId = "test_heritage") {
		const string nameListId = "name_list_test";
		var nameList = new NameList(nameListId, new BufferedReader());

		var heritage = PillarCollection.GetHeritageForId(heritageId);
		if (heritage is null) {
			heritage = new Pillar(heritageId, new PillarData { Type = "heritage" });
			PillarCollection.Add(heritage);
		}
		
		var cultureData = new CultureData {
			Heritage = heritage,
			NameLists = {nameList}
		};
		var culture = new Culture(id, cultureData);
		Add(culture);
	}
}