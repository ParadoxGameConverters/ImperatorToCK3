using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Pops;
using ImperatorToCK3.Imperator.Religions;
using ImperatorToCK3.Imperator.States;
using System.Collections.Generic;

namespace ImperatorToCK3.Imperator.Provinces;

public enum ProvinceRank { settlement, city, city_metropolis }
public sealed partial class Province : IIdentifiable<ulong> {
	public ulong Id { get; } = 0;
	public string Name { get; set; } = "";
	public string Culture { get; set; } = "";
	public string ReligionId { get; set; } = "";
	public State? State { get; private set; } = null;
	public Country? OwnerCountry { get; set; }
	public ulong Controller { get; set; } = 0;
	public IDictionary<ulong, Pop> Pops { get; } = new Dictionary<ulong, Pop>();
	public ProvinceRank ProvinceRank { get; set; } = ProvinceRank.settlement;
	public bool Fort { get; set; } = false;
	public bool IsHolySite => HolySiteId is not null;
	public ulong? HolySiteId { get; set; } = null;
	public ulong? HoldingOwnerId { get; set; } = null;
	public uint BuildingCount { get; set; } = 0;
	public double CivilizationValue { get; set; } = 0;

	public Province(ulong id) {
		Id = id;
	}

	public int GetPopCount() {
		return Pops.Count;
	}

	public Religion? GetReligion(ReligionCollection religions) {
		return religions.TryGetValue(ReligionId, out var religion) ? religion : null;
	}

	public Deity? GetHolySiteDeity(ReligionCollection religions) {
		return HolySiteId is null ? null : religions.GetDeityForHolySiteId((ulong)HolySiteId);
	}

	// Returns a count of linked pops
	public int LinkPops(PopCollection popCollection) {
		int counter = 0;
		foreach (var popId in parsedPopIds) {
			if (popCollection.TryGetValue(popId, out var popToLink)) {
				Pops.Add(popId, popToLink);
				++counter;
			} else {
				Logger.Warn($"Pop with ID {popId} has no definition!");
			}
		}
		return counter;
	}

	private readonly HashSet<ulong> parsedPopIds = new();
}