using commonItems;
using ImperatorToCK3.Imperator.Armies;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Armies; 

public class SubunitTests {
	[Fact]
	public void SubunitDetailsCanBeLoaded() {
		var reader = new BufferedReader(@"
			subunit_name={
				name=""COHORT_NAME_aryan""
				ordinal=527
				home=""PROV7296""
			}
			category=regular
			home=7296
			type=""archers""
			morale=7.665
			morale_damage=0
			experience=10.9
			strength=0.0375
			strength_damage=0
			target=4294967295
			country=2
		");
		var subunit = new Subunit(420, reader);
		
		Assert.Equal((ulong)420, subunit.Id);
		Assert.Equal("regular", subunit.Category);
		Assert.Equal("archers", subunit.Type);
		Assert.Equal(0.0375, subunit.Strength);
		Assert.Equal((ulong)2, subunit.CountryId);
	}
}