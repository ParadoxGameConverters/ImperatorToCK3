using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using commonItems;
using ImperatorToCK3.Imperator.Genes;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Genes {
	public class WeighBlockTests {


		[Fact] public void objectsCanBeAdded() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
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
		public void objectsCanBeAddedByMethod() {
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
		public void sumOfAbsoluteWeightsDefaultsToZero() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
				"}"
			);
			var weightBlock = new WeightBlock(reader);

			Assert.Equal((uint)0, weightBlock.SumOfAbsoluteWeights);
		}

		[Fact]
		public void getMatchingObjectThrowsErrorOnNegativeArgument() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
				"\t2 = female_hair_greek_2\n" +
				"}"
			);
			var weightBlock = new WeightBlock(reader);

			Assert.Throws<ArgumentOutOfRangeException>(()=>weightBlock.GetMatchingObject(-0.5));
		}

		[Fact]
		public void getMatchingObjectThrowsErrorOnArgumentGreaterThan1() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
				"\t2 = female_hair_greek_2\n" +
				"}"
			);
			var weightBlock = new WeightBlock(reader);

			Assert.Throws<ArgumentOutOfRangeException>(() => weightBlock.GetMatchingObject(1.234));
		}

		[Fact]
		public void getMatchingObjectReturnsNulloptWhenObjectsMapIsEmpty() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
				"}"
			);
			var weightBlock = new WeightBlock(reader);

			Assert.Null(weightBlock.GetMatchingObject(0.345));
		}
	}
}
