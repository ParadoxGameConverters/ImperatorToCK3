using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;
using ImperatorToCK3.Imperator.Families;

namespace ImperatorToCK3.Imperator.Characters {
	public class Character {
		public ulong ID { get; } = 0;
		private string culture = string.Empty;
		public string Culture {
			get {
				if (!string.IsNullOrEmpty(culture)) {
					return culture;
				}
				if (family.Key != 0 && family.Value is not null && !string.IsNullOrEmpty(family.Value.Culture)) {
					return family.Value.Culture;
				}
				return culture;
			}
			private set {
				culture = value;
			}
		}
		public string Religion { get; private set; } = string.Empty;
		public string Name { get; private set; } = string.Empty;
		public string Nickname { get; private set; } = string.Empty;
		public ulong ProvinceID { get; private set; } = 0;
		public Date BirthDate { get; private set; } = new Date(1, 1, 1);
		public Date? DeathDate { get; private set; }
		public bool IsDead => DeathDate is not null;
		public string? DeathReason { get; private set; }
		public Dictionary<ulong, Character?> Spouses { get; set; } = new();
		public Dictionary<ulong, Character?> Children { get; set; } = new();
		public KeyValuePair<ulong, Character?> Mother { get; set; } = new();
		public KeyValuePair<ulong, Character?> Father { get; set; } = new();
		private KeyValuePair<ulong, Family?> family = new(0, null);
		public KeyValuePair<ulong, Family?> Family {
			get {
				return family;
			}
			set {
				if (value.Value is null) {
					Logger.Warn($"Setting null family {value.Key} to character {ID}!");
				} else if (value.Value.ID != value.Key) {
					Logger.Warn($"Setting family with ID mismatch: {value.Key} v. {value.Value.ID}!");
				}
				family = value;
			} 
		}
		public List<string> Traits { get; set; } = new();
		public CharacterAttributes Attributes { get; private set; } = new();
		public uint Age { get; private set; } = new();
		public string? DNA { get; private set; }
		public PortraitData? PortraitData { get; private set; }
		public string AgeSex {
			get {
				if (Age >= 16) {
					if (Female) {
						return "female";
					}
					return "male";
				}
				if (Female) {
					return "girl";
				}
				return "boy";
			}
		}
		public bool Female { get; private set; } = false;
		public double Wealth { get; private set; } = 0;

		//public ImperatorToCK3.CK3.Characters.Character? CK3Character { get; set; } // TODO: ENABLE

		public void AddYears(int years) {
			BirthDate.SubtractYears(years);
		}
	}
}
