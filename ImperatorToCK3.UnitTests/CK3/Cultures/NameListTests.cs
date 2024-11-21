using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Cultures;

public class NameListTests {
	[Fact]
	public void NameListIsCorrectlyLoaded() {
		var reader = new BufferedReader("""
		    {
		      male_names = {
		        John
		        "Alexander"
		        2 = { Tom "Mark" }
		      }
		      female_names = {
		        Jane
		        "Alexandra"
		        2 = { Sandra "Maria" }
		      }
		""");
		var nameList = new ImperatorToCK3.CK3.Cultures.NameList("test", reader);

		Assert.Equal("test", nameList.Id);
		Assert.Collection(nameList.MaleNames,
			item => Assert.Equal("John", item),
			item => Assert.Equal("Alexander", item),
			item => Assert.Equal("Tom", item),
			item => Assert.Equal("Mark", item)
		);
		Assert.Collection(nameList.FemaleNames,
			item => Assert.Equal("Jane", item),
			item => Assert.Equal("Alexandra", item),
			item => Assert.Equal("Sandra", item),
			item => Assert.Equal("Maria", item)
		);
	}
}