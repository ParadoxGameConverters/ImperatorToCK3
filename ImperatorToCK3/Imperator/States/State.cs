using commonItems.Collections;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Provinces;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.Imperator.States;

public sealed class State : IIdentifiable<ulong> {
	public ulong Id { get; }
	private readonly ulong capitalProvinceId;
	public Area Area { get; }
	public Country Country { get; }

	public State(ulong id, StateData stateData) {
		Id = id;
		
		capitalProvinceId = stateData.CapitalProvinceId;
		Area = stateData.Area!;
		Country = stateData.Country!;
	}

	public Province CapitalProvince => Area.Provinces.First(p => p.Id == capitalProvinceId);
	public IEnumerable<Province> Provinces => Area.Provinces.Where(p => p.State?.Id == Id);
}