using commonItems;
using commonItems.Mods;
using AwesomeAssertions;
using ImperatorToCK3.CommonUtils.Map;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Provinces;
using ImperatorToCK3.Imperator.States;
using System;
using System.Linq;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.States;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class StateTests {
	private const string ImperatorRoot = "TestFiles/StateTests";
	private static readonly ModFilesystem irModFS = new(ImperatorRoot, Array.Empty<Mod>());
	private static readonly ProvinceCollection provinces = new();
	private static readonly AreaCollection areas = new();
	private static readonly CountryCollection countries = new() {new Country(69), new Country(70)};

	public StateTests() {
		areas.LoadAreas(irModFS, provinces);
	}

	[Fact]
	public void StateCanBeInitialized() {
		var stateCollection = new StateCollection();
		var statesReader = new BufferedReader("""
			1 = {
				area="test_area"
				capital=2
				country=69
			}
			2 = {
				area="test_area"
				capital=3
				country=70
			}
		""");
		stateCollection.LoadStates(statesReader, areas, countries);
		var state1 = stateCollection[1];
		Assert.Equal("test_area", state1.Area.Id);
		Assert.Equal((ulong)69, state1.Country.Id);
		Assert.Equal((ulong)2, state1.CapitalProvince.Id);
		var state2 = stateCollection[2];
		Assert.Equal("test_area", state2.Area.Id);
		Assert.Equal((ulong)70, state2.Country.Id);
		Assert.Equal((ulong)3, state2.CapitalProvince.Id);
	}

	[Fact]
	public void ProvincesCanBeRetrievedAfterProvincesInitialization() {
		var states = new StateCollection();
		var stateData = new StateData {Area = areas["test_area"], CapitalProvinceId = 2, Country = countries[69]};
		var state = new State(1, stateData);
		states.Add(state);

		var provincesReader = new BufferedReader("""
			1={ state=1 }
			2={ state=1 }
			3={ state=1 }
			4={}
			5={}
		"""
		);
		provinces.LoadProvinces(provincesReader, states, countries, new MapData(irModFS));
		Assert.Equal((ulong)2, state.CapitalProvince.Id);
		state.Provinces.Select(p=>p.Id).Should().Equal(1, 2, 3);
	}
}