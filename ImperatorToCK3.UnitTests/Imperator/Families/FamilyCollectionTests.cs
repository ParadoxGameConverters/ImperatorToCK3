using commonItems;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Families;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class FamilyCollectionTests {
	[Fact]
	public void FamiliesDefaultToEmpty() {
		var reader = new BufferedReader(
			"= {}"
		);
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		families.LoadFamilies(reader);

		Assert.Empty(families);
	}

	[Fact]
	public void FamiliesCanBeLoaded() {
		var reader = new BufferedReader(
			"= {\n" +
			"42={}\n" +
			"43={}\n" +
			"}"
		);
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		families.LoadFamilies(reader);

		Assert.Collection(families,
			item => Assert.Equal((ulong)42, item.Id),
			item => Assert.Equal((ulong)43, item.Id));
	}
	
	[Fact]
	public void FamiliesCanBeLoadedFromBloc() {
		var reader = new BufferedReader(
			"families = {\n42={}\n43={}\n}"
		);
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		families.LoadFamiliesFromBloc(reader);

		Assert.Collection(families,
			item => Assert.Equal((ulong)42, item.Id),
			item => Assert.Equal((ulong)43, item.Id));
	}

	[Fact]
	public void LiteralNoneFamiliesAreNotLoaded() {
		var reader = new BufferedReader(
			"={\n" +
			"42=none\n" +
			"43={}\n" +
			"44=none\n" +
			"}"
		);
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		families.LoadFamilies(reader);

		Assert.Collection(families,
			item => Assert.Equal((ulong)43, item.Id)
		);
	}

	[Fact]
	public void FamilyRedefinitionIsLogged() {
		var reader = new BufferedReader(
			"= {\n42={}\n42={}\n}"
		);
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		
		var output = new StringWriter();
		Console.SetOut(output);
		families.LoadFamilies(reader);
		
		Assert.Contains("Redefinition of family 42.", output.ToString());
	}
}