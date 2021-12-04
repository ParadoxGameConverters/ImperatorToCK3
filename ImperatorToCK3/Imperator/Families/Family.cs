using commonItems;
using commonItems.Collections;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace ImperatorToCK3.Imperator.Families {
	public class Family : IIdentifiable<ulong> {
		public ulong Id { get; } = 0;
		public string Key { get; private set; } = "";
		public string Culture { get; private set; } = "";
		public double Prestige { get; private set; } = 0;
		public double PrestigeRatio { get; private set; } = 0;
		public OrderedDictionary Members { get; private set; } = new();
		public PDXBool Minor { get; private set; } = new(false);

		public Family(ulong id) {
			Id = id;
		}
		public void LinkMember(Characters.Character? newMember) {
			if (newMember is null) {
				Logger.Warn($"Family {Id}: cannot link null member!");
				return;
			}
			foreach (DictionaryEntry memberPair in Members) {
				if ((ulong)memberPair.Key == newMember.Id) {
					Members[memberPair.Key] = newMember;
					return;
				}
			}
			if (newMember.DeathDate is not null) { // if character is dead, his ID needs to be added to the dict
				Members.Add(newMember.Id, newMember);
				return;
			}
			// matching ID was not found
			Logger.Warn($"Family {Id}: cannot link {newMember.Id} (not found in members)!");
		}
		public void RemoveUnlinkedMembers() {
			var toRemove = new List<ulong>();
			foreach (DictionaryEntry entry in Members) {
				if (entry.Value is null) {
					toRemove.Add((ulong)entry.Key);
				}
			}
			foreach (var idToRemove in toRemove) {
				Members.Remove(idToRemove);
			}
		}

		public static HashSet<string> IgnoredTokens { get; private set; } = new();
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
					family.Minor = reader.GetPDXBool()
				);
				parser.RegisterKeyword("member", reader => {
					foreach (var memberId in reader.GetULongs()) {
						family.Members.Add(memberId, null);
					}
				});
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
}
