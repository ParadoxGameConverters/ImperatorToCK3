using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Imperator.Countries;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Diplomacy;

internal class DiplomacyDB {
	public List<List<Title>> Leagues { get; } = [];

	public void ImportImperatorLeagues(IReadOnlyCollection<List<ulong>> irLeagues, CountryCollection countries) {
		Logger.Info("Importing Imperator defensive leagues...");

		foreach (var irLeague in irLeagues) {
			List<Title> ck3LeagueMembers = [];
			foreach (var irMemberId in irLeague) {
				if (!countries.TryGetValue(irMemberId, out var country)) {
					Logger.Warn($"Member {irMemberId} of defensive league not found in countries!");
					continue;
				}

				var ck3Title = country.CK3Title;
				if (ck3Title is not null) {
					ck3LeagueMembers.Add(ck3Title);
				}
			}

			if (ck3LeagueMembers.Count < 2) {
				Logger.Notice("Not enough members in league to import it, skipping: " +
				             $"{string.Join(", ", ck3LeagueMembers.Select(t => t.Id))}");
				continue;
			}
			Leagues.Add(ck3LeagueMembers);
		}
	}
}