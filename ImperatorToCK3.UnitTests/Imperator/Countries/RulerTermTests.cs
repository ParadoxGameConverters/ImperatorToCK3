using commonItems;
using ImperatorToCK3.Imperator.Countries;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Countries {
	[Collection("RulerTermTests")]
	[CollectionDefinition("RulerTermTests", DisableParallelization = true)]
	public class RulerTermTests {
		private static readonly StringWriter consoleOutput = new();
		static RulerTermTests() {
			Console.SetOut(consoleOutput);
		}
		[Fact]
		public void IgnoredTokensAreStored() {
			var reader1 = new BufferedReader(
				"character = 69 " +
				"start_date = 500.2.3 " +
				"government = dictatorship " +
				"corruption = unused"
			);
			_ = RulerTerm.Parse(reader1);

			var reader2 = new BufferedReader(
				"character = 69 " +
				"start_date = 500.2.3 " +
				"government = dictatorship " +
				"list = { unused }"
			);
			_ = RulerTerm.Parse(reader2);
			Assert.True(RulerTerm.IgnoredTokens.SetEquals(new HashSet<string> { "corruption", "list" }));
		}

		[Fact]
		public void PreImperatorTermCanBeRead() {
			var countries = new ImperatorToCK3.Imperator.Countries.Countries();
			var countryReader = new BufferedReader("= { tag = SPA }");
			var sparta = Country.Parse(countryReader, 69);
			countries.StoredCountries.Add(sparta.ID, sparta);

			var preImpTermReader = new BufferedReader("= { name=\"Alexander\"" +
				" birth_date=200.1.1 death_date=300.1.1 throne_date=250.1.1" +
				" nickname=THE_BOLD religion=hellenic culture=spartan" +
				" country=SPA }"
			);
			var impRulerTerm = new RulerTerm(preImpTermReader, countries);
			Assert.Null(impRulerTerm.CharacterId);
			Assert.Equal(new Date(250, 1, 1, AUC: true), impRulerTerm.StartDate);
			Assert.Null(impRulerTerm.Government);

			var ruler = impRulerTerm.PreImperatorRuler;
			Assert.NotNull(ruler);
			Assert.Equal("Alexander", ruler.Name);
			Assert.Equal("hellenic", ruler.Religion);
			Assert.Equal("spartan", ruler.Culture);
			Assert.Equal("THE_BOLD", ruler.Nickname);
			Assert.Equal(new Date(200, 1, 1, AUC: true), ruler.BirthDate);
			Assert.Equal(new Date(300, 1, 1, AUC: true), ruler.DeathDate);
			Assert.Equal("SPA", ruler.Country.Tag);
		}

		[Fact]
		public void WrongTagIsLoggedForPreImperatorRulers() {
			var countries = new ImperatorToCK3.Imperator.Countries.Countries();
			Assert.Empty(countries.StoredCountries);

			var preImpTermReader = new BufferedReader(
				"= { name=\"Alexander\" throne_date=250.1.1 country=WER }"
			);
			_ = new RulerTerm(preImpTermReader, countries);
			Assert.Contains("[WARN] Pre-Imperator ruler has wrong tag: WER!", consoleOutput.ToString());
		}
	}
}
