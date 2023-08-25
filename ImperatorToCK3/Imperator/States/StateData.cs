using ImperatorToCK3.Imperator.Countries;
using ImperatorToCK3.Imperator.Geography;

namespace ImperatorToCK3.Imperator.States;

public record StateData {
	public ulong CapitalProvinceId { get; set; }
	public Area? Area { get; set; }
	public Country? Country { get; set; }
}