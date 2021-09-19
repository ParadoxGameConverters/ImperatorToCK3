﻿using commonItems;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Government;
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
			var ck3RulerTerm = new RulerTerm(impRulerTerm, govMapper);
			Assert.Equal("imperator69", ck3RulerTerm.CharacterId);
			Assert.Equal(new Date(500, 2, 3, AUC: true), ck3RulerTerm.StartDate);
			Assert.Equal("feudal_government", ck3RulerTerm.Government);
		}
	}
}
