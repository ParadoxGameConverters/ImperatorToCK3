using commonItems;
using System;
using System.IO;
using System.Linq;
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

	[Fact]
	public void MergeDividedFamiliesWithNoFamiliesCompletes() {
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		var characters = new ImperatorToCK3.Imperator.Characters.CharacterCollection();

		// Should complete without error when no families exist
		families.MergeDividedFamilies(characters);
		
		Assert.Empty(families);
	}

	[Fact]
	public void MergeDividedFamiliesWithSingleFamilyCompletes() {
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		var characters = new ImperatorToCK3.Imperator.Characters.CharacterCollection();
		
		var family = new ImperatorToCK3.Imperator.Families.Family(1);
		families.TryAdd(family);
		
		// Should complete without merging when only one family exists
		families.MergeDividedFamilies(characters);
		
		Assert.Single(families);
	}

	[Fact] 
	public void MergeDividedFamiliesWithMultipleFamiliesWithSameKeyButNoRelationship() {
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		var characters = new ImperatorToCK3.Imperator.Characters.CharacterCollection();
		
		// Create families with same key
		var family1Reader = new BufferedReader("key=\"test_family\" member={1}");
		var family1 = ImperatorToCK3.Imperator.Families.Family.Parse(family1Reader, 1);
		
		var family2Reader = new BufferedReader("key=\"test_family\" member={2}");
		var family2 = ImperatorToCK3.Imperator.Families.Family.Parse(family2Reader, 2);
		
		families.TryAdd(family1);
		families.TryAdd(family2);
		
		// Create characters that are not related
		var character1 = new ImperatorToCK3.Imperator.Characters.Character(1);
		var character2 = new ImperatorToCK3.Imperator.Characters.Character(2);
		characters.TryAdd(character1);
		characters.TryAdd(character2);
		
		families.MergeDividedFamilies(characters);
		
		// Should still have 2 families since no relationship exists
		Assert.Equal(2, families.Count);
	}

	[Fact]
	public void MergeDividedFamiliesWithParentChildRelationshipMergesFamilies() {
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		var characters = new ImperatorToCK3.Imperator.Characters.CharacterCollection();
		
		// Create families with same key
		var family1Reader = new BufferedReader("key=\"test_family\" member={1}");
		var family1 = ImperatorToCK3.Imperator.Families.Family.Parse(family1Reader, 1);
		
		var family2Reader = new BufferedReader("key=\"test_family\" member={2}");
		var family2 = ImperatorToCK3.Imperator.Families.Family.Parse(family2Reader, 2);
		
		families.TryAdd(family1);
		families.TryAdd(family2);
		
		// Create parent-child relationship
		var parent = new ImperatorToCK3.Imperator.Characters.Character(1);
		var child = new ImperatorToCK3.Imperator.Characters.Character(2);
		child.Father = parent;
		
		characters.TryAdd(parent);
		characters.TryAdd(child);
		
		families.MergeDividedFamilies(characters);
		
		// Should have merged into 1 family
		Assert.Single(families);
		var mergedFamily = families.First();
		Assert.Contains(1UL, mergedFamily.MemberIds);
		Assert.Contains(2UL, mergedFamily.MemberIds);
	}

	[Fact]
	public void LoadFamiliesIgnoresUnrecognizedTokens() {
		var reader = new BufferedReader(
			"= {\n" +
			"42={ key=\"test\" unknown_token=5 }\n" +
			"}"
		);
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		families.LoadFamilies(reader);

		Assert.Single(families);
		Assert.Equal("test", families.First().Key);
	}

	[Fact]
	public void MergeDividedFamiliesWithMotherChildRelationshipMergesFamilies() {
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		var characters = new ImperatorToCK3.Imperator.Characters.CharacterCollection();
		
		// Create families with same key
		var family1Reader = new BufferedReader("key=\"test_family\" member={1}");
		var family1 = ImperatorToCK3.Imperator.Families.Family.Parse(family1Reader, 1);
		
		var family2Reader = new BufferedReader("key=\"test_family\" member={2}");
		var family2 = ImperatorToCK3.Imperator.Families.Family.Parse(family2Reader, 2);
		
		families.TryAdd(family1);
		families.TryAdd(family2);
		
		// Create mother-child relationship
		var mother = new ImperatorToCK3.Imperator.Characters.Character(1);
		var child = new ImperatorToCK3.Imperator.Characters.Character(2);
		child.Mother = mother;
		
		characters.TryAdd(mother);
		characters.TryAdd(child);
		
		families.MergeDividedFamilies(characters);
		
		// Should have merged into 1 family
		Assert.Single(families);
		var mergedFamily = families.First();
		Assert.Contains(1UL, mergedFamily.MemberIds);
		Assert.Contains(2UL, mergedFamily.MemberIds);
	}

	[Fact]
	public void MergeDividedFamiliesWithMultipleIterationsNeeded() {
		var families = new ImperatorToCK3.Imperator.Families.FamilyCollection();
		var characters = new ImperatorToCK3.Imperator.Characters.CharacterCollection();
		
		// Create three families with same key
		var family1Reader = new BufferedReader("key=\"test_family\" member={1}");
		var family1 = ImperatorToCK3.Imperator.Families.Family.Parse(family1Reader, 1);
		
		var family2Reader = new BufferedReader("key=\"test_family\" member={2}");
		var family2 = ImperatorToCK3.Imperator.Families.Family.Parse(family2Reader, 2);
		
		var family3Reader = new BufferedReader("key=\"test_family\" member={3}");
		var family3 = ImperatorToCK3.Imperator.Families.Family.Parse(family3Reader, 3);
		
		families.TryAdd(family1);
		families.TryAdd(family2);
		families.TryAdd(family3);
		
		// Create chain: grandparent -> parent -> child
		var grandparent = new ImperatorToCK3.Imperator.Characters.Character(1);
		var parent = new ImperatorToCK3.Imperator.Characters.Character(2);
		var child = new ImperatorToCK3.Imperator.Characters.Character(3);
		
		parent.Father = grandparent;
		child.Father = parent;
		
		characters.TryAdd(grandparent);
		characters.TryAdd(parent);
		characters.TryAdd(child);
		
		families.MergeDividedFamilies(characters);
		
		// Should have merged all into 1 family
		Assert.Single(families);
		var mergedFamily = families.First();
		Assert.Contains(1UL, mergedFamily.MemberIds);
		Assert.Contains(2UL, mergedFamily.MemberIds);
		Assert.Contains(3UL, mergedFamily.MemberIds);
	}
}