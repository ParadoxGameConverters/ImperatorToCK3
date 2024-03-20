using commonItems;
using ImperatorToCK3.CommonUtils.Genes;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils.Genes; 

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class WeightBlockTests {
	[Fact]
	public void ErrorIsLoggedOnUnparsableWeight() {
		var output = new StringWriter();
		Console.SetOut(output);
		var reader = new BufferedReader(
			"={\n" +
			"\t2 = sdfsdf\n" +
			"\t-2 = female_hair_greek_1\n" +
			"}"
		);
		_ = new WeightBlock(reader);
		Assert.Contains("[ERROR] Could not parse absolute weight: -2", output.ToString());
	}
	[Fact]
	public void ObjectsCanBeAdded() {
		var reader = new BufferedReader(
			"= {\n" +
			"\t5 = female_hair_greek_1\n" +
			"\t2 = female_hair_greek_2\n" +
			"\t6 = female_hair_greek_3\n" +
			"}"
		);
		var weightBlock = new WeightBlock(reader);

		Assert.Equal((uint)5, weightBlock.GetAbsoluteWeight("female_hair_greek_1"));
		Assert.Equal((uint)2, weightBlock.GetAbsoluteWeight("female_hair_greek_2"));
		Assert.Equal((uint)6, weightBlock.GetAbsoluteWeight("female_hair_greek_3"));
		Assert.Equal((uint)13, weightBlock.SumOfAbsoluteWeights);

		Assert.Equal("female_hair_greek_1", weightBlock.GetMatchingObject(0.37234234));
		Assert.Equal("female_hair_greek_2", weightBlock.GetMatchingObject(0.52234234234));
		Assert.Equal("female_hair_greek_3", weightBlock.GetMatchingObject(1));
	}

	[Fact]
	public void ObjectsCanBeAddedByMethod() {
		var weightBlock = new WeightBlock();
		weightBlock.AddObject("new_object", 69);
		weightBlock.AddObject("new_object2", 5);
		Assert.Equal((uint)69, weightBlock.GetAbsoluteWeight("new_object"));
		Assert.Equal((uint)5, weightBlock.GetAbsoluteWeight("new_object2"));
		Assert.Equal((uint)74, weightBlock.SumOfAbsoluteWeights);

		Assert.Equal("new_object", weightBlock.GetMatchingObject(0));
		Assert.Equal("new_object2", weightBlock.GetMatchingObject(0.95));
	}

	[Fact]
	public void SumOfAbsoluteWeightsDefaultsToZero() {
		var reader = new BufferedReader("= {}");
		var weightBlock = new WeightBlock(reader);

		Assert.Equal((uint)0, weightBlock.SumOfAbsoluteWeights);
	}

	[Fact]
	public void GetMatchingObjectThrowsErrorOnNegativeArgument() {
		var reader = new BufferedReader(
			"= {\n" +
			"\t2 = female_hair_greek_2\n" +
			"}"
		);
		var weightBlock = new WeightBlock(reader);

		Assert.Throws<ArgumentOutOfRangeException>(() => weightBlock.GetMatchingObject(-0.5));
	}

	[Fact]
	public void GetMatchingObjectThrowsErrorOnArgumentGreaterThan1() {
		var reader = new BufferedReader(
			"= {\n" +
			"\t2 = female_hair_greek_2\n" +
			"}"
		);
		var weightBlock = new WeightBlock(reader);

		Assert.Throws<ArgumentOutOfRangeException>(() => weightBlock.GetMatchingObject(1.234));
	}

	[Fact]
	public void GetMatchingObjectReturnsNullWhenObjectsMapIsEmpty() {
		var reader = new BufferedReader(
			"= {}"
		);
		var weightBlock = new WeightBlock(reader);

		Assert.Null(weightBlock.GetMatchingObject(0.345));
	}

	[Fact]
	public void GetMatchingPercentageReturnsCorrectValues() {
		var weightBlock = new WeightBlock();
		weightBlock.AddObject("a", 1);
		weightBlock.AddObject("b", 1);

		Assert.Equal(0d, weightBlock.GetMatchingPercentage("a"));
		Assert.Equal(0.5d, weightBlock.GetMatchingPercentage("b"));
	}

	[Fact]
	public void GetMatchingPercentageReturnsNullWrongEntryName() {
		Assert.Null(new WeightBlock().GetMatchingPercentage("ENTRY"));
	}
}