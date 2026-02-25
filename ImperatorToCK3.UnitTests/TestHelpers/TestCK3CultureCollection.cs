using commonItems;
using commonItems.Colors;
using DotLiquid;
using ImperatorToCK3.CK3.Cultures;
using System.Collections.Generic;

namespace ImperatorToCK3.UnitTests.TestHelpers; 

internal class TestCK3CultureCollection() : CultureCollection(colorFactory, new PillarCollection(colorFactory, ck3ModFlags), ck3ModFlags) {
	private static readonly ColorFactory colorFactory = new();
	private static readonly OrderedDictionary<string, bool> ck3ModFlags = [];
	private static readonly Hash liquidVariables = new();
	
	public void LoadConverterPillars(string filePath) {
		PillarCollection.LoadConverterPillars(filePath, ck3ModFlags, liquidVariables);
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
			NameLists = {nameList},
			Color = new Color(100, 150, 200)
		};
		var culture = new Culture(id, cultureData);
		Add(culture);
	}
}