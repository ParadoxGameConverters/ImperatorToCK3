using commonItems;
using commonItems.Localization;
using ImperatorToCK3.Imperator;
using ImperatorToCK3.Imperator.Armies;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Armies; 

public class UnitTests {
	[Fact]
	public void LocalizedNameIsCorrectlyGenerated() {
		var locDB = new LocDB("english", "french");
		var italyLocBlock = locDB.AddLocBlock("central_italy_region");
		italyLocBlock["english"] = "Italia";
		
		var governorshipLegionLocBlock = locDB.AddLocBlock("GOVERNORSHIP_LEGION_NAME_roman");
		governorshipLegionLocBlock["english"] = "Legio $BASE$";

		var latinLegionLocBlock = locDB.AddLocBlock("LEGION_NAME_latin");
		latinLegionLocBlock["english"] = "Cohors $ROMAN$ $BASE$";

		var units = new UnitCollection();
		
		var unitReader = new BufferedReader(@"
			unit_name={
				name=""LEGION_NAME_latin""
				ordinal=5
				base={
					name=""GOVERNORSHIP_LEGION_NAME_roman""
					base={
						name=""central_italy_region""
					}
				}
			}");
		var unit = new Unit(1, unitReader, units, locDB, new Defines());
		
		Assert.NotNull(unit.LocalizedName);
		Assert.Equal("Cohors V Legio Italia", unit.LocalizedName["english"]);
	}
}