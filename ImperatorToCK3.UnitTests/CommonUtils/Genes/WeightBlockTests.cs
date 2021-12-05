using commonItems;
using ImperatorToCK3.CommonUtils.Genes;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.CommonUtils.Genes {
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
				"={\n" +
				"\t5 = female_hair_greek_1\n" +
				"\t2 = sdfsdf\n" +
				"\t6 = random\n" +
				"}"
			);
			var weightBlock = new WeightBlock(reader);

			Assert.Equal((uint)5, weightBlock.GetAbsoluteWeight("female_hair_greek_1"));
			Assert.Equal((uint)2, weightBlock.GetAbsoluteWeight("sdfsdf"));
			Assert.Equal((uint)6, weightBlock.GetAbsoluteWeight("random"));
			Assert.Equal((uint)13, weightBlock.SumOfAbsoluteWeights);

			Assert.Equal("female_hair_greek_1", weightBlock.GetMatchingObject(0.37234234));
			Assert.Equal("sdfsdf", weightBlock.GetMatchingObject(0.52234234234));
			Assert.Equal("random", weightBlock.GetMatchingObject(1));
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
				"={\n" +
				"\t2 = female_hair_greek_2\n" +
				"}"
			);
			var weightBlock = new WeightBlock(reader);

			Assert.Throws<ArgumentOutOfRangeException>(() => weightBlock.GetMatchingObject(-0.5));
		}

		[Fact]
		public void GetMatchingObjectThrowsErrorOnArgumentGreaterThan1() {
			var reader = new BufferedReader(
				"={\n" +
				"\t2 = female_hair_greek_2\n" +
				"}"
			);
			var weightBlock = new WeightBlock(reader);

			Assert.Throws<ArgumentOutOfRangeException>(() => weightBlock.GetMatchingObject(1.234));
		}

		[Fact]
		public void GetMatchingObjectReturnsNulloptWhenObjectsMapIsEmpty() {
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
		public void GetMatchingPercentageThrowsOnWrongEntryName() {
			var weightBlock = new WeightBlock();
			var e = Assert.Throws<KeyNotFoundException>(() => weightBlock.GetMatchingPercentage("ENTRY"));
			Assert.Contains("Set entry ENTRY not found!", e.ToString());
		}
	}
}
