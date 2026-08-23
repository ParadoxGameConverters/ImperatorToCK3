using commonItems.Collections;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;
using ImperatorToCK3.Imperator.Provinces;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ImperatorToCK3.Imperator.States;

internal sealed class State : IIdentifiable<ulong> {
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

	public Province CapitalProvince {
		get {
			if (capitalProvince is not null) {
				return capitalProvince;
			}
			if (Area.TryGetProvince(capitalProvinceId, out var areaProvince)) {
				capitalProvince = areaProvince;
				return areaProvince;
			}
			throw new KeyNotFoundException($"Capital province {capitalProvinceId} was not found in area {Area.Id} for state {Id}.");
		}
	}

	public IEnumerable<Province> Provinces {
		get {
			if (provinces is not null) {
				return provinces;
			}

			var stateProvinces = ImmutableArray.CreateBuilder<Province>();
			foreach (var province in Area.Provinces) {
				if (province.State?.Id == Id) {
					stateProvinces.Add(province);
				}
			}

			provinces = stateProvinces.ToImmutable();
			return provinces;
		}
	}

	private Province? capitalProvince;
	private ImmutableArray<Province>? provinces;
}