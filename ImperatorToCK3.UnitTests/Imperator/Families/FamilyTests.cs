using System;
using System.IO;
using commonItems;
using Xunit;
using ImperatorToCK3.Imperator.Families;
using System.Collections.Generic;

namespace ImperatorToCK3.UnitTests.Imperator.Families {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class FamilyTests {
		[Fact] public void FieldsCanBeSet() {
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
			Assert.Equal((ulong)42, family.ID);
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
			Assert.Equal((ulong)42, family.ID);
			Assert.Equal(string.Empty, family.Culture);
			Assert.Equal(0, family.Prestige);
			Assert.Equal(0, family.PrestigeRatio);
			Assert.Equal(string.Empty, family.Key);
			Assert.False(family.Minor);
			Assert.Empty(family.Members);
		}

		[Fact] public void LinkingNullMemberIsLogged() {
			var reader = new BufferedReader(
				"= { }"
			);
			var family = Family.Parse(reader, 42);

			var output = new StringWriter();
			Console.SetOut(output);
			family.LinkMember(null);
			Assert.Contains("[WARN] Family 42: cannot link null member!", output.ToString());
		}

		[Fact]
		public void CannotLinkMemberWithoutPreexistingMatchingID() {
			var reader = new BufferedReader(
				"= { member={40 50 5} }"
			);
			var family = Family.Parse(reader, 42);

			var characterReader = new BufferedReader(string.Empty);
			var character = ImperatorToCK3.Imperator.Characters.Character.Parse(characterReader, "6", null);

			var output = new StringWriter();
			Console.SetOut(output);

			family.LinkMember(character);
			Assert.Contains("[WARN] Family 42: cannot link 6 (not found in members)!", output.ToString());
		}

		[Fact]
		public void MemberLinkingWorks() {
			var reader = new BufferedReader(
				"= { member={40 50 5} }"
			);
			var family = Family.Parse(reader, 42);

			var characterReader = new BufferedReader("= { culture = kushite }");
			var genesDB = new ImperatorToCK3.Imperator.Genes.GenesDB();
			var character = ImperatorToCK3.Imperator.Characters.Character.Parse(characterReader, "50", genesDB);
			family.LinkMember(character);

			Assert.Equal("kushite", ((ImperatorToCK3.Imperator.Characters.Character)family.Members[1]).Culture);
		}

		[Fact] public void IgnoredTokensAreSaved() {
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
}
