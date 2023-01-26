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

	[Fact]
	public void UnitStrengthIsCorrectlyCalculated() {
		var defines = new Defines();
		Assert.Equal(500, defines.CohortSize);

		var subunitsReader = new BufferedReader(@"
			1 = { strength = 0.5 type=""archers"" } # 250 men
			2 = { strength = 1 type=""archers"" } # 500 men
		");

		var unitCollection = new UnitCollection();
		unitCollection.LoadSubunits(subunitsReader);

		var unitReader = new BufferedReader("cohort=1 cohort=2");
		var unit = new Unit(1, unitReader, unitCollection, new LocDB("english"), defines);

		Assert.Equal(750, unit.MenPerUnitType["archers"]); // 250 + 500
	}
}