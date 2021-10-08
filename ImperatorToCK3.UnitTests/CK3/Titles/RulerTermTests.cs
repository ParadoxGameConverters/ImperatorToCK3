using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Government;
using ImperatorToCK3.Mappers.Localization;
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
				new Dictionary<string, Character>(),
				govMapper,
				new LocalizationMapper(),
				new ReligionMapper(),
				new CultureMapper(),
				new ProvinceMapper()
			);
			Assert.Equal("imperator69", ck3RulerTerm.CharacterId);
			Assert.Equal(new Date(500, 2, 3, AUC: true), ck3RulerTerm.StartDate);
			Assert.Equal("feudal_government", ck3RulerTerm.Government);
		}

		[Fact]
		public void PreImperatorTermIsCorrectlyConverted() {
			var countries = new ImperatorToCK3.Imperator.Countries.Countries();
			var countryReader = new BufferedReader("= { tag = SPA }");
			var sparta = ImperatorToCK3.Imperator.Countries.Country.Parse(countryReader, 69);
			countries.StoredCountries.Add(sparta.ID, sparta);

			var preImpTermReader = new BufferedReader("= { name=\"Alexander\"" +
				" birth_date=200.1.1 death_date=300.1.1 throne_date=250.1.1" +
				" nickname=THE_BOLD religion=hellenic culture=spartan" +
				" country=SPA }"
			);
			var impRulerTerm = new ImperatorToCK3.Imperator.Countries.RulerTerm(preImpTermReader, countries);

			var govReader = new BufferedReader("link = {imp=dictatorship ck3=feudal_government }");
			var govMapper = new GovernmentMapper(govReader);
			var ck3RulerTerm = new RulerTerm(impRulerTerm,
				new Dictionary<string, Character>(),
				govMapper,
				new LocalizationMapper(),
				new ReligionMapper(),
				new CultureMapper(),
				new ProvinceMapper()
			);
			Assert.Equal("imperatorRegnalSPAAlexander504.1.1BC", ck3RulerTerm.CharacterId);
			Assert.Equal(new Date(250, 1, 1, AUC: true), ck3RulerTerm.StartDate);
			var ruler = ck3RulerTerm.PreImperatorRuler;
			Assert.NotNull(ruler);
			Assert.Equal("Alexander", ruler.Name);
		}
	}
}
