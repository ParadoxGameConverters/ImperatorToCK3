using ImperatorToCK3.Mappers.HolySiteEffect;
using System.Collections.Generic;
using Xunit;

namespace ImperatorToCK3.UnitTests.Mappers.HolySiteEffect;

public class HolySiteEffectMapperTests {
	[Theory, MemberData(nameof(TestData))]
	public void MapperReturnsCorrectValues(string imperatorEffect, double imperatorValue, KeyValuePair<string, double>? match) {
		const string mappingsFilePath = "TestFiles/HolySiteEffectMapperTests/mappings.txt";
		var mapper = new HolySiteEffectMapper(mappingsFilePath);
		Assert.Equal(match, mapper.Match(imperatorEffect, imperatorValue));
	}
 
	public static IEnumerable<object[]> TestData => 
		new List<object[]> {
			new object[] { "wrong_effect", 1, null },
			new object[] { "effect_to_multiply", 2, new KeyValuePair<string, double>("doubled_effect", 4) },
			new object[] { "effect_to_divide", 4, new KeyValuePair<string, double>("divided_effect", 2) },
			new object[] { "effect_to_nullify", 1, new KeyValuePair<string, double>("nullified_effect", 0)}
		};
}