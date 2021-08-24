using System.Collections.Generic;
using commonItems;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Characters {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class CharacterTests {
		private readonly ImperatorToCK3.Imperator.Genes.GenesDB genesDB = new();
		[Fact]
		public void FieldsCanBeSet() {
			var reader = new BufferedReader(
				"= {" +
				"\tculture=\"paradoxian\"" +
				"\treligion=\"orthodox\"" +
				"\tfemale=yes" +
				"\ttraits = { \"lustful\" \"submissive\" \"greedy\" }" +
				"\tbirth_date=408.6.28" + // will be converted to AD date on loading
				"\tdeath_date=408.6.28" + // will be converted to AD date on loading
				"\tdeath = killed_in_battle" +
				"\tspouse= { 69 420 } " +
				"\tchildren = { 69 420 } " +
				"\tmother=123" +
				"\tfather=124" +
				"\tfamily=125" +
				"\twealth=\"420.5\"" +
				"\tfirst_name_loc = {\n" +
				"\t\tname=\"Biggus Dickus\"\n" +
				"\t}\n" +
				"\tnickname = \"the Great\"\n" +
				"\tattributes={ martial=1 finesse=2 charisma=3 zeal=4 }" +
				"\tdna=\"paradoxianDna\"" +
				"\tage=56\n" +
				"\tprovince=69" +
				"}"
			);
			var character = ImperatorToCK3.Imperator.Characters.Character.Parse(reader, "42", genesDB);
			var spouse1Reader = new BufferedReader(string.Empty);
			var spouse2Reader = new BufferedReader(string.Empty);
			character.Spouses = new() {
				{ 69, ImperatorToCK3.Imperator.Characters.Character.Parse(spouse1Reader, "69", genesDB) },
				{ 420, ImperatorToCK3.Imperator.Characters.Character.Parse(spouse2Reader, "420", genesDB) }
			};

			Assert.Equal((ulong)42, character.ID);
			Assert.Equal("paradoxian", character.Culture);
			Assert.Equal("orthodox", character.Religion);
			Assert.True(character.Female);
			Assert.Collection(character.Traits,
				item => Assert.Equal("lustful", item),
				item => Assert.Equal("submissive", item),
				item => Assert.Equal("greedy", item)
			);
			Assert.Equal(new Date("-346.6.28"), character.BirthDate);
			Assert.Equal(new Date("-346.6.28"), character.DeathDate);
			Assert.Equal("killed_in_battle", character.DeathReason);
			Assert.Collection(character.Spouses,
				item => {
					Assert.Equal((ulong)69, item.Key);
					Assert.Equal((ulong)69, item.Value.ID);
				},
				item => {
					Assert.Equal((ulong)420, item.Key);
					Assert.Equal((ulong)420, item.Value.ID);
				}
			);
			Assert.Collection(character.Children,
				item => {
					Assert.Equal((ulong)69, item.Key);
				},
				item => {
					Assert.Equal((ulong)420, item.Key);
				}
			);
			Assert.Equal((ulong)123, character.Mother.Key);
			Assert.Equal((ulong)124, character.Father.Key);
			Assert.Equal((ulong)125, character.Family.Key);
			Assert.Equal(420.5, character.Wealth);
			Assert.Equal("Biggus Dickus", character.Name);
			Assert.Equal("the Great", character.Nickname);
			Assert.Equal(1, character.Attributes.Martial);
			Assert.Equal(2, character.Attributes.Finesse);
			Assert.Equal(3, character.Attributes.Charisma);
			Assert.Equal(4, character.Attributes.Zeal);
			Assert.Equal("paradoxianDna", character.DNA);
			Assert.Equal((uint)56, character.Age);
			Assert.Equal((ulong)69, character.ProvinceID);
		}
		[Fact]
		public void FieldsDefaultToCorrectValues() {
			var reader = new BufferedReader(string.Empty);
			var character = ImperatorToCK3.Imperator.Characters.Character.Parse(reader, "42", genesDB);
			Assert.Equal(string.Empty, character.Culture);
			Assert.Equal(string.Empty, character.Religion);
			Assert.False(character.Female);
			Assert.Empty(character.Traits);
			Assert.Equal(new Date("1.1.1"), character.BirthDate);
			Assert.Null(character.DeathDate);
			Assert.Null(character.DeathReason);
			Assert.Empty(character.Spouses);
			Assert.Empty(character.Children);
			Assert.Equal((ulong)0, character.Mother.Key);
			Assert.Equal((ulong)0, character.Father.Key);
			Assert.Equal((ulong)0, character.Family.Key);
			Assert.Equal(0, character.Wealth);
			Assert.Equal(string.Empty, character.Name);
			Assert.Equal(string.Empty, character.Nickname);
			Assert.Equal(0, character.Attributes.Martial);
			Assert.Equal(0, character.Attributes.Finesse);
			Assert.Equal(0, character.Attributes.Charisma);
			Assert.Equal(0, character.Attributes.Zeal);
			Assert.Null(character.DNA);
			Assert.Equal((uint)0, character.Age);
			Assert.Equal((ulong)0, character.ProvinceID);
		}

		[Fact]
		public void CultureCanBeInheritedFromFamily() {
			var familyReader = new BufferedReader(
				"= { culture = paradoxian }"
			);
			var characterReader = new BufferedReader(
				"= { family = 42 }"
			);
			var family = ImperatorToCK3.Imperator.Families.Family.Parse(familyReader, 42);
			var character = ImperatorToCK3.Imperator.Characters.Character.Parse(characterReader, "69", genesDB);
			character.Family = new(42, family);
			Assert.Equal("paradoxian", character.Culture);
		}

		[Fact]
		public void PortraitDataIsNotExtractedFromDnaOfWrongLength() {
			var reader = new BufferedReader(
				"={dna=\"AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/==\"}"
			);
			var character = ImperatorToCK3.Imperator.Characters.Character.Parse(reader, "42", genesDB);
			Assert.Null(character.PortraitData);
		}
		[Fact]
		public void ColorPaletteCoordinatesCanBeExtractedFromDNA() {
			var reader = new BufferedReader(
				"={dna=\"AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==\"}"
			);
			var character = ImperatorToCK3.Imperator.Characters.Character.Parse(reader, "42", genesDB);

			Assert.Equal((uint)0, character.PortraitData.HairColorPaletteCoordinates.x);
			Assert.Equal((uint)0, character.PortraitData.HairColorPaletteCoordinates.y);
			Assert.Equal((uint)0, character.PortraitData.SkinColorPaletteCoordinates.x);
			Assert.Equal((uint)0, character.PortraitData.SkinColorPaletteCoordinates.y);
			Assert.Equal((uint)0, character.PortraitData.EyeColorPaletteCoordinates.x);
			Assert.Equal((uint)0, character.PortraitData.EyeColorPaletteCoordinates.y);
		}

		[Fact]
		public void GetAgeSexReturnsCorrectString() {
			var reader1 = new BufferedReader(
				"=\n" +
			"{\n" +
			"\tage=56\n" +
			"\tfemale=yes\n" +
			"}"
			);
			var reader2 = new BufferedReader(
				"=\n" +
			"{\n" +
			"\tage=56\n" +
			"}"
			);
			var reader3 = new BufferedReader(
				"=\n" +
			"{\n" +
			"\tage=8\n" +
			 "\tfemale=yes\n" +
			 "}"
			);
			var reader4 = new BufferedReader(
				 "=\n" +
			"{\n" +
			 "\tage=8\n" +
			 "}"
			);
			var character1 = ImperatorToCK3.Imperator.Characters.Character.Parse(reader1, "42", genesDB);
			var character2 = ImperatorToCK3.Imperator.Characters.Character.Parse(reader2, "43", genesDB);
			var character3 = ImperatorToCK3.Imperator.Characters.Character.Parse(reader3, "44", genesDB);
			var character4 = ImperatorToCK3.Imperator.Characters.Character.Parse(reader4, "45", genesDB);
			Assert.Equal("female", character1.AgeSex);
			Assert.Equal("male", character2.AgeSex);
			Assert.Equal("girl", character3.AgeSex);
			Assert.Equal("boy", character4.AgeSex);
		}

		[Fact]
		public void AUC0ConvertsTo754BC() {
			var reader = new BufferedReader(
			"= { birth_date = 0.1.1 }"
			);
			var character = ImperatorToCK3.Imperator.Characters.Character.Parse(reader, "42", genesDB);

			Assert.Equal("-754.1.1", character.BirthDate.ToString());
		}

		[Fact]
		public void AUC753ConvertsTo1BC() {
			var reader = new BufferedReader(
				"= { birth_date = 753.1.1 }"
			);
			var character = ImperatorToCK3.Imperator.Characters.Character.Parse(reader, "42", genesDB);

			Assert.Equal("-1.1.1", character.BirthDate.ToString());
		}

		[Fact]
		public void AUC754ConvertsTo1AD() {
			var reader = new BufferedReader(
				 "= { birth_date = 754.1.1 }"
			);
			var character = ImperatorToCK3.Imperator.Characters.Character.Parse(reader, "42", genesDB);

			Assert.Equal("1.1.1", character.BirthDate.ToString());
		}
	}
}
