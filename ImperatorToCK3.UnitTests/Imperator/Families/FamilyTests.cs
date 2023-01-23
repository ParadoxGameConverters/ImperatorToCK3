using commonItems;
using ImperatorToCK3.Imperator.Families;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Families;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class FamilyTests {
	[Fact]
	public void FieldsCanBeSet() {
		var reader = new BufferedReader(
			"= {" +
			"\tculture=\"paradoxian\"" +
			"\tprestige=\"420.5\"" +
			"\tprestige_ratio=\"0.75\"" +
			"\tkey=\"paradoxian\"" +
			"\tminor_family=\"yes\"" +
			"}"
		);
		var family = Family.Parse(reader, 42);
		Assert.Equal((ulong)42, family.Id);
		Assert.Equal("paradoxian", family.Culture);
		Assert.Equal(420.5, family.Prestige);
		Assert.Equal(0.75, family.PrestigeRatio);
		Assert.Equal("paradoxian", family.Key);
		Assert.True(family.Minor);
	}
	[Fact]
	public void FieldsDefaultToCorrectValues() {
		var reader = new BufferedReader(
			"= { }"
		);
		var family = Family.Parse(reader, 42);
		Assert.Equal((ulong)42, family.Id);
		Assert.Equal(string.Empty, family.Culture);
		Assert.Equal(0, family.Prestige);
		Assert.Equal(0, family.PrestigeRatio);
		Assert.Equal(string.Empty, family.Key);
		Assert.False(family.Minor);
		Assert.Empty(family.MemberIds);
	}

	[Fact]
	public void LinkingNullMemberIsLogged() {
		var reader = new BufferedReader(
			"= { }"
		);
		var family = Family.Parse(reader, 42);

		var output = new StringWriter();
		Console.SetOut(output);
		family.AddMember(null);
		Assert.Contains("[WARN] Family 42: cannot link null member!", output.ToString());
	}

	[Fact]
	public void IgnoredTokensAreSaved() {
		var reader1 = new BufferedReader("= { culture=paradoxian ignoredKeyword1=something ignoredKeyword2={} }");
		var reader2 = new BufferedReader("= { ignoredKeyword1=stuff ignoredKeyword3=stuff }");
		_ = Family.Parse(reader1, 1);
		_ = Family.Parse(reader2, 2);

		var expectedIgnoredTokens = new HashSet<string> {
			"ignoredKeyword1", "ignoredKeyword2", "ignoredKeyword3"
		};
		Assert.True(Family.IgnoredTokens.SetEquals(expectedIgnoredTokens));
	}
}