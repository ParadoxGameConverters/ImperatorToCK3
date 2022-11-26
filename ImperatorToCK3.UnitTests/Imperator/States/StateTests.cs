using commonItems;
using commonItems.Mods;
using FluentAssertions;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Imperator.States;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.States; 

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class StateTests {
	private const string ImperatorRoot = "TestFiles/StateTests";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, new Mod[] { });
	private static readonly ProvinceCollection provinces = new();
	private static readonly AreaCollection areas = new();
	private static readonly CountryCollection countries = new() {new Country(69)};
	
	public StateTests() {
		areas.LoadAreas(irModFS, provinces);
	}
	
	[Fact]
	public void StateCanBeInitialized() {
		var stateReader = new BufferedReader("""
			area="test_area"
			capital=2
			country=69
		""");
		var state = new State(1, stateReader, areas, countries);
		Assert.Equal("test_area", state.Area.Id);
		Assert.Equal((ulong)69, state.Country.Id);
	}

	[Fact]
	public void ProvincesCanBeRetrievedAfterProvincesInitialization() {
		var states = new StateCollection();
		var stateReader = new BufferedReader("""
			area="test_area"
			capital=2
			country=69
		""");
		var state = new State(1, stateReader, areas, countries);
		states.Add(state);

		var provincesReader = new BufferedReader(""" 
			1={ state=1 }
			2={ state=1 }
			3={ state=1 }
			4={}
			5={}
		"""
		);
		provinces.LoadProvinces(provincesReader, states, countries);
		Assert.Equal((ulong)2, state.CapitalProvince.Id);
		state.Provinces.Select(p=>p.Id).Should().Equal(1, 2, 3);
	}
}