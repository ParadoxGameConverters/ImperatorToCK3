using commonItems;
using ImperatorToCK3.CommonUtils.Genes;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Characters; 

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CharacterTests {
	private readonly GenesDB genesDB = new();
	[Fact]
	public void FieldsCanBeSet() {
		var dnaStr = "AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==";
		var reader = new BufferedReader(
			"= {" +
			"\tcountry=69" +
			"\thome_country=68" +
			"\tculture=\"paradoxian\"" +
			"\treligion=\"orthodox\"" +
			"\tfemale=yes" +
			"\ttraits = { \"lustful\" \"submissive\" \"greedy\" }" +
			"\tbirth_date=408.6.28" + // will be converted to AD date on loading
			"\tdeath_date=408.6.28" + // will be converted to AD date on loading
			"\tdeath = killed_in_battle" +
			"\tspouse= { 3 4 } " +
			"\tchildren = { 69 420 } " +
			"\tmother=123" +
			"\tfather=124" +
			"\tfamily=125" +
			"\twealth=\"420.5\"" +
			"\tunborn={" +
			"\t\t{" +
			"\t\t\tmother=42" +
			"\t\t\tfather=3" +
			"\t\t\tdate=452.4.19" +
			"\t\t}" +
			"\t\t{" +
			"\t\t\tmother=42" +
			"\t\t\tfather=3" +
			"\t\t\tdate=452.1.26" +
			"\t\t}" +
			"\t}" +
			"\tfirst_name_loc = {\n" +
			"\t\tname=\"Biggus_Dickus\"\n" +
			"\t\tcustom_name=\"CUSTOM NAME\"\n" +
			"\t}\n" +
			"\tnickname = \"the Great\"\n" +
			"\tattributes={ martial=1 finesse=2 charisma=3 zeal=4 }" +
			"\tdna=\"" + dnaStr + "\"" +
			"\tage=56\n" +
			"\tprovince=69" +
			"\tprisoner_home=68" +
			"}"
		);
		var character = ImperatorToCK3.Imperator.Characters.Character.Parse(reader, "42", genesDB);
		var spouse1Reader = new BufferedReader(string.Empty);
		var spouse2Reader = new BufferedReader(string.Empty);
		character.Spouses = new Dictionary<ulong, ImperatorToCK3.Imperator.Characters.Character> {
			{ 3, ImperatorToCK3.Imperator.Characters.Character.Parse(spouse1Reader, "3", genesDB) },
			{ 4, ImperatorToCK3.Imperator.Characters.Character.Parse(spouse2Reader, "4", genesDB) }
		};

		Assert.Equal((ulong)42, character.Id);

		Assert.Null(character.Country); // we have a country id, but no linked country yet
		var countriesReader = new BufferedReader("={ 69={} 68={} }");
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		countries.LoadCountries(countriesReader);
		character.LinkCountry(countries);
		Assert.NotNull(character.Country);
		Assert.Equal((ulong)69, character.Country.Id);

		Assert.Null(character.HomeCountry); // we have a home country id, but no linked home country yet
		character.LinkHomeCountry(countries);
		Assert.NotNull(character.HomeCountry);
		Assert.Equal((ulong)68, character.HomeCountry.Id);

		Assert.Null(character.PrisonerHome); // we have a prisoner home id, but no linked prisoner home yet
		character.LinkPrisonerHome(countries);
		Assert.NotNull(character.PrisonerHome);
		Assert.Equal((ulong)68, character.PrisonerHome.Id);

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
				Assert.Equal((ulong)3, item.Key);
				Assert.Equal((ulong)3, item.Value.Id);
			},
			item => {
				Assert.Equal((ulong)4, item.Key);
				Assert.Equal((ulong)4, item.Value.Id);
			}
		);

		Assert.Empty(character.Children); // children not linked yet
		var characters = new ImperatorToCK3.Imperator.Characters.CharacterCollection();
		characters.Add(character);
		var child1 = ImperatorToCK3.Imperator.Characters.Character.Parse(new BufferedReader("={ mother=42 }"), "69", null);
		var child2 = ImperatorToCK3.Imperator.Characters.Character.Parse(new BufferedReader("={ mother=42 }"), "420", null);
		characters.Add(child1);
		characters.Add(child2);
		child1.LinkMother(characters);
		child2.LinkMother(characters);

		Assert.Collection(character.Children,
			item => {
				Assert.Equal((ulong)69, item.Key);
			},
			item => {
				Assert.Equal((ulong)420, item.Key);
			}
		);

		Assert.Null(character.Mother); // mother not linked yet
		Assert.Null(character.Father); // father not linked yet
		var mother = new ImperatorToCK3.Imperator.Characters.Character(123);
		var father = new ImperatorToCK3.Imperator.Characters.Character(124);
		characters.Add(mother);
		characters.Add(father);
		character.LinkMother(characters);
		character.LinkFather(characters);
		Assert.NotNull(character.Mother);
		Assert.NotNull(character.Father);
		Assert.Equal((ulong)123, character.Mother.Id);
		Assert.Equal((ulong)124, character.Father.Id);

		Assert.Null(character.Family); // Despite "family=125" in character definition, Family is null until linked.
		Assert.Equal(420.5, character.Wealth);
		Assert.Collection(character.Unborns,
			unborn => {
				Assert.Equal((ulong)42, unborn.MotherId);
				Assert.Equal((ulong)3, unborn.FatherId);
				Assert.Equal(new Date("-302.4.19"), unborn.BirthDate);
			},
			unborn => {
				Assert.Equal((ulong)42, unborn.MotherId);
				Assert.Equal((ulong)3, unborn.FatherId);
				Assert.Equal(new Date("-302.1.26"), unborn.BirthDate);
			});
		Assert.Equal("Biggus_Dickus", character.Name);
		Assert.Equal("CUSTOM NAME", character.CustomName);
		Assert.Equal("the Great", character.Nickname);
		Assert.Equal(1, character.Attributes.Martial);
		Assert.Equal(2, character.Attributes.Finesse);
		Assert.Equal(3, character.Attributes.Charisma);
		Assert.Equal(4, character.Attributes.Zeal);
		Assert.Equal(dnaStr, character.DNA);
		Assert.Equal((uint)56, character.Age);
		Assert.Equal((ulong)69, character.ProvinceId);
	}
	[Fact]
	public void FieldsDefaultToCorrectValues() {
		var reader = new BufferedReader(string.Empty);
		var character = ImperatorToCK3.Imperator.Characters.Character.Parse(reader, "42", genesDB);
		Assert.Null(character.Country);
		Assert.Equal(string.Empty, character.Culture);
		Assert.Equal(string.Empty, character.Religion);
		Assert.False(character.Female);
		Assert.Empty(character.Traits);
		Assert.Equal(new Date("1.1.1"), character.BirthDate);
		Assert.Null(character.DeathDate);
		Assert.Null(character.DeathReason);
		Assert.Empty(character.Spouses);
		Assert.Empty(character.Children);
		Assert.Null(character.Mother);
		Assert.Null(character.Father);
		Assert.Null(character.Family);
		Assert.Equal(0, character.Wealth);
		Assert.Equal(string.Empty, character.Name);
		Assert.Null(character.CustomName);
		Assert.Null(character.Nickname);
		Assert.Equal(0, character.Attributes.Martial);
		Assert.Equal(0, character.Attributes.Finesse);
		Assert.Equal(0, character.Attributes.Charisma);
		Assert.Equal(0, character.Attributes.Zeal);
		Assert.Null(character.DNA);
		Assert.Equal((uint)0, character.Age);
		Assert.Null(character.ProvinceId);
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
		character.Family = family;
		Assert.Equal("paradoxian", character.Culture);
	}

	[Fact]
	public void PortraitDataIsNotExtractedFromEmptyDNAString() {
		var reader = new BufferedReader(
			// ReSharper disable once StringLiteralTypo
			"={dna=\"\"}"
		);
		var character = ImperatorToCK3.Imperator.Characters.Character.Parse(reader, "42", genesDB);
		Assert.Null(character.PortraitData);
	}
	[Fact]
	public void ColorPaletteCoordinatesCanBeExtractedFromDNA() {
		var reader = new BufferedReader(
			// ReSharper disable once StringLiteralTypo
			"={dna=\"AAAAAAAAAAAAAAAAAH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AfwB/AH8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==\"}"
		);
		var character = ImperatorToCK3.Imperator.Characters.Character.Parse(reader, "42", genesDB);

		Assert.Equal(0, character.PortraitData!.HairColorPaletteCoordinates.X);
		Assert.Equal(0, character.PortraitData.HairColorPaletteCoordinates.Y);
		Assert.Equal(0, character.PortraitData.SkinColorPaletteCoordinates.X);
		Assert.Equal(0, character.PortraitData.SkinColorPaletteCoordinates.Y);
		Assert.Equal(0, character.PortraitData.EyeColorPaletteCoordinates.X);
		Assert.Equal(0, character.PortraitData.EyeColorPaletteCoordinates.Y);
	}

	[Fact]
	public void AgeSexReturnsCorrectString() {
		var reader1 = new BufferedReader(
			"= {\n" +
			"\tage=56\n" +
			"\tfemale=yes\n" +
			"}"
		);
		var reader2 = new BufferedReader(
			"= {\n" +
			"\tage=56\n" +
			"}"
		);
		var reader3 = new BufferedReader(
			"= {\n" +
			"\tage=8\n" +
			"\tfemale=yes\n" +
			"}"
		);
		var reader4 = new BufferedReader(
			"= {\n" +
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

	[Fact]
	public void IgnoredTokensAreSaved() {
		var reader1 = new BufferedReader("= { culture=paradoxian ignoredKeyword1=something ignoredKeyword2={} }");
		var reader2 = new BufferedReader("= { ignoredKeyword1=stuff ignoredKeyword3=stuff }");
		_ = ImperatorToCK3.Imperator.Characters.Character.Parse(reader1, "1", null);
		_ = ImperatorToCK3.Imperator.Characters.Character.Parse(reader2, "2", null);

		var expectedIgnoredTokens = new HashSet<string> {
			"ignoredKeyword1", "ignoredKeyword2", "ignoredKeyword3"
		};
		Assert.True(ImperatorToCK3.Imperator.Characters.Character.IgnoredTokens.SetEquals(expectedIgnoredTokens));
	}

	[Fact]
	public void CountryIsNotLinkedWithoutParsedId() {
		var character = new ImperatorToCK3.Imperator.Characters.Character(1);
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		character.LinkCountry(countries);
		character.LinkHomeCountry(countries);
		character.LinkPrisonerHome(countries);
		Assert.Null(character.Country);
		Assert.Null(character.HomeCountry);
		Assert.Null(character.PrisonerHome);
	}

	[Fact] public void LinkingCountryWithNoDefinitionIsLogged() {
		var output = new StringWriter();
		Console.SetOut(output);

		var characterReader = new BufferedReader("= { country=69 }");
		var character = ImperatorToCK3.Imperator.Characters.Character.Parse(characterReader, "1", null);
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		character.LinkCountry(countries);

		Assert.Contains("[WARN] Country with ID 69 has no definition!", output.ToString());
	}

	[Fact]
	public void LinkingHomeCountryWithNoDefinitionIsLogged() {
		var output = new StringWriter();
		Console.SetOut(output);

		var characterReader = new BufferedReader("= { home_country=69 }");
		var character = ImperatorToCK3.Imperator.Characters.Character.Parse(characterReader, "1", null);
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		character.LinkHomeCountry(countries);

		Assert.Contains("[WARN] Country with ID 69 has no definition!", output.ToString());
	}

	[Fact]
	public void LinkingPrisonerHomeWithNoDefinitionIsLogged() {
		var output = new StringWriter();
		Console.SetOut(output);

		var characterReader = new BufferedReader("= { prisoner_home=69 }");
		var character = ImperatorToCK3.Imperator.Characters.Character.Parse(characterReader, "1", null);
		var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
		character.LinkPrisonerHome(countries);

		Assert.Contains("[WARN] Country with ID 69 has no definition!", output.ToString());
	}
}