using commonItems;
using commonItems.Collections;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Imperator.Characters;
using ImperatorToCK3.Imperator.Cultures;
using System.Linq;

namespace ImperatorToCK3.Imperator.Families;

public class Family : IIdentifiable<ulong> {
	public ulong Id { get; } = 0;
	public string Key { get; private set; } = "";
	public string Culture { get; private set; } = "";
	public double Prestige { get; private set; } = 0;
	public double PrestigeRatio { get; private set; } = 0;
	public OrderedSet<ulong> MemberIds { get; } = new();
	public bool Minor { get; private set; } = false;

	public Family(ulong id) {
		Id = id;
	}
	public void AddMember(Character? newMember) {
		if (newMember is null) {
			Logger.Warn($"Family {Id}: cannot link null member!");
			return;
		}
		MemberIds.Add(newMember.Id);
	}
	public void RemoveUnlinkedMembers(CharacterCollection characters) {
		var toRemove = MemberIds.Where(memberId => !characters.ContainsKey(memberId)).ToList();
		foreach (var idToRemove in toRemove) {
			MemberIds.Remove(idToRemove);
		}
	}
	
	public static string GetMaleForm(string familyNameKey, CulturesDB culturesDB) {
		return culturesDB.GetMaleFamilyNameForm(familyNameKey) ?? familyNameKey;
	}

	public string GetMaleForm(CulturesDB culturesDB) {
		return GetMaleForm(Key, culturesDB);
	}

	public static IgnoredKeywordsSet IgnoredTokens { get; } = new();
	private static class FamilyFactory {
		private static readonly Parser parser = new();
		private static Family family = new(0);
		static FamilyFactory() {
			parser.RegisterKeyword("key", reader =>
				family.Key = reader.GetString()
			);
			parser.RegisterKeyword("prestige", reader =>
				family.Prestige = reader.GetDouble()
			);
			parser.RegisterKeyword("prestige_ratio", reader =>
				family.PrestigeRatio = reader.GetDouble()
			);
			parser.RegisterKeyword("culture", reader =>
				family.Culture = reader.GetString()
			);
			parser.RegisterKeyword("minor_family", reader =>
				family.Minor = reader.GetBool()
			);
			parser.RegisterKeyword("member", reader => {
				foreach (var memberId in reader.GetULongs()) {
					family.MemberIds.Add(memberId);
				}
			});
			parser.RegisterKeyword("color", ParserHelpers.IgnoreItem);
			parser.RegisterRegex(CommonRegexes.Catchall, (reader, token) => {
				IgnoredTokens.Add(token);
				ParserHelpers.IgnoreItem(reader);
			});
		}
		public static Family Parse(BufferedReader reader, ulong id) {
			family = new Family(id);
			parser.ParseStream(reader);
			return family;
		}
	}

	public static Family Parse(BufferedReader reader, ulong id) {
		return FamilyFactory.Parse(reader, id);
	}
}