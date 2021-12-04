using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Localization;
using ImperatorToCK3.Mappers.Nickname;
using ImperatorToCK3.Mappers.Province;
using ImperatorToCK3.Mappers.Religion;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Titles {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class RulerTermTests {
		[Fact]
		public void ImperatorRulerTermIsCorrectlyConverted() {
			var reader = new BufferedReader(
				"character = 69 " +
				"start_date = 500.2.3 " +
				"government = dictatorship"
			);
			var impRulerTerm = ImperatorToCK3.Imperator.Countries.RulerTerm.Parse(reader);
			var govReader = new BufferedReader("link = {imp=dictatorship ck3=feudal_government }");
			var govMapper = new GovernmentMapper(govReader);
			var ck3RulerTerm = new RulerTerm(impRulerTerm,
				new ImperatorToCK3.CK3.Characters.CharacterCollection(),
				govMapper,
				new LocalizationMapper(),
				new ReligionMapper(),
				new CultureMapper(),
				new NicknameMapper("TestFiles/configurables/nickname_map.txt"),
				new ProvinceMapper()
			);
			Assert.Equal("imperator69", ck3RulerTerm.CharacterId);
			Assert.Equal(new Date(500, 2, 3, AUC: true), ck3RulerTerm.StartDate);
			Assert.Equal("feudal_government", ck3RulerTerm.Government);
		}

		[Fact]
		public void PreImperatorTermIsCorrectlyConverted() {
			var countries = new ImperatorToCK3.Imperator.Countries.CountryCollection();
			var countryReader = new BufferedReader("= { tag = SPA capital=420 }");
			var sparta = ImperatorToCK3.Imperator.Countries.Country.Parse(countryReader, 69);
			countries.Add(sparta);

			var preImpTermReader = new BufferedReader("= { name=\"Alexander\"" +
				" birth_date=200.1.1 death_date=300.1.1 throne_date=250.1.1" +
				" nickname=stupid religion=hellenic culture=spartan" +
				" country=SPA }"
			);
			var impRulerTerm = new ImperatorToCK3.Imperator.Countries.RulerTerm(preImpTermReader, countries);

			var govReader = new BufferedReader("link = {imp=dictatorship ck3=feudal_government }");
			var govMapper = new GovernmentMapper(govReader);
			var religionMapper = new ReligionMapper(new BufferedReader("link={imp=hellenic ck3=hellenic}"));
			religionMapper.LoadRegionMappers(
				new ImperatorToCK3.Mappers.Region.ImperatorRegionMapper(),
				new ImperatorToCK3.Mappers.Region.CK3RegionMapper()
			);
			var ck3Characters = new ImperatorToCK3.CK3.Characters.CharacterCollection();
			var ck3RulerTerm = new RulerTerm(impRulerTerm,
				ck3Characters,
				govMapper,
				new LocalizationMapper(),
				religionMapper,
				new CultureMapper(new BufferedReader("link = { imp=spartan ck3=greek }")),
				new NicknameMapper("TestFiles/configurables/nickname_map.txt"),
				new ProvinceMapper()
			);
			Assert.Equal("imperatorRegnalSPAAlexander504.1.1BC", ck3RulerTerm.CharacterId);
			Assert.Equal(new Date(250, 1, 1, AUC: true), ck3RulerTerm.StartDate);
			var ruler = ck3RulerTerm.PreImperatorRuler;
			Assert.NotNull(ruler);
			Assert.Equal("Alexander", ruler.Name);

			var ck3Character = ck3Characters["imperatorRegnalSPAAlexander504.1.1BC"];
			Assert.Equal(new Date(0, 1, 1), ck3Character.BirthDate); // BC dates are not supported by CK3
			Assert.Equal(new Date(0, 1, 30), ck3Character.DeathDate); // BC dates are not supported by CK3
			Assert.Equal("Alexander", ck3Character.Name);
			Assert.Equal("dull", ck3Character.Nickname);
			Assert.Equal("greek", ck3Character.Culture);
			Assert.Equal("hellenic", ck3Character.Religion);
		}
	}
}
