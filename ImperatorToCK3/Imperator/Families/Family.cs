using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;

namespace ImperatorToCK3.Imperator.Families {
	public class Family {
		public ulong ID { get; } = 0;
		public string Key { get; private set; } = "";
		public string Culture { get; private set; } = "";
		public double Prestige { get; private set; } = 0;
		public double PrestigeRatio { get; private set; } = 0;
		public OrderedDictionary Members { get; private set; } = new();
		public bool Minor { get; private set; } = false;

		public Family(ulong ID) {
			this.ID = ID;
		}
		public void LinkMember(Character? newMember) {
			if (newMember is null) {
				Logger.Warn($"Family {ID}: cannot link null member!");
				return;
			}
			foreach(DictionaryEntry memberPair in Members) {
				if (memberPair.Key == newMember.ID) {
					Members[memberPair.Key] = newMember;
					return;
				}
			}
			if (newMember.DeathDate is not null) { // if character is dead, his ID needs to be added to the dict
				Members.Add(newMember.ID, newMember);
				return;
			}
			// matching ID was not found
			Logger.Warn($"Family {ID}: cannot link {newMember.ID} (not found in members)!");
		}
		public void RemoveUnlinkedMembers() {
			var toRemove = new List<ulong>();
			foreach(DictionaryEntry entry in Members) {
				if (entry.Value is null) {
					toRemove.Add((ulong)entry.Key);
				}
			}
			foreach (var idToRemove in toRemove) {
				Members.Remove(idToRemove);
			}
		}

		private static class FamilyFactory {
			private static readonly Parser parser = new();
			private static Family family = new(0);
			static FamilyFactory() {
				parser.RegisterKeyword("key", reader =>
					family.Key = new SingleString(reader).String
				);
				parser.RegisterKeyword("prestige", reader =>
					family.Prestige = new SingleDouble(reader).Double
				);
				parser.RegisterKeyword("prestige_ratio", reader =>
					family.PrestigeRatio = new SingleDouble(reader).Double
				);
				parser.RegisterKeyword("culture", reader =>
					family.Culture = new SingleString(reader).String
				);
				parser.RegisterKeyword("minor_family", reader =>
					family.Minor = new SingleString(reader).String == "yes"
				);
				parser.RegisterKeyword("member", reader => {
					foreach (var memberID in new ULongList(reader).ULongs) {
						family.Members.Add(memberID, null);
					}
				});
				parser.RegisterRegex(CommonRegexes.Catchall, ParserHelpers.IgnoreAndLogItem);
			}
			public static Family Parse(BufferedReader reader, ulong ID) {
				family = new Family(ID);
				parser.ParseStream(reader);
				return family;
			}
		}

		public static Family Parse(BufferedReader reader, ulong ID) {
			return FamilyFactory.Parse(reader, ID);
		}
	}
}
