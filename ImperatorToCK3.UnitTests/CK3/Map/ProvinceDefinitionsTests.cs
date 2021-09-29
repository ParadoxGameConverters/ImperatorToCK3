using ImageMagick;
using ImperatorToCK3.CK3.Map;
using System;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Map {
	[Collection("MapTests")]
	[CollectionDefinition("MapTests", DisableParallelization = true)]
	public class ProvinceDefinitionsTests {
		private const string testCK3Path = "TestFiles/CK3";
		[Fact]
		public void DictionariesDefaultToEmpty() {
			var provDefs = new ProvinceDefinitions(testCK3Path);
			Assert.Collection(provDefs.ColorToProvinceDict,
				pair1 => {
					(MagickColor key, ulong value) = pair1;
					Assert.Equal(MagickColor.FromRgb(42, 3, 128), key);
					Assert.Equal((ulong)1, value);
				},
				pair2 => {
					(MagickColor key, ulong value) = pair2;
					Assert.Equal(MagickColor.FromRgb(84, 6, 1), key);
					Assert.Equal((ulong)2, value);
				},
				pair3 => {
					(MagickColor key, ulong value) = pair3;
					Assert.Equal(MagickColor.FromRgb(126, 9, 129), key);
					Assert.Equal((ulong)3, value);
				}
			);
			Assert.Collection(provDefs.ProvinceToColorDict,
				pair1 => {
					(ulong key, MagickColor value) = pair1;
					Assert.Equal((ulong)1, key);
					Assert.Equal(MagickColor.FromRgb(42, 3, 128), value);
				},
				pair2 => {
					(ulong key, MagickColor value) = pair2;
					Assert.Equal((ulong)2, key);
					Assert.Equal(MagickColor.FromRgb(84, 6, 1), value);
				},
				pair3 => {
					(ulong key, MagickColor value) = pair3;
					Assert.Equal((ulong)3, key);
					Assert.Equal(MagickColor.FromRgb(126, 9, 129), value);
				}
			);
		}

		[Fact]
		public void ExceptionIsThrownOnUnparseableLine() {
			const string ck3Path = "TestFiles/corruptedCK3";
			// this definition.csv has a line with quoted province id
			Assert.Throws<FormatException>(() => _ = new ProvinceDefinitions(ck3Path));
		}
	}
}
