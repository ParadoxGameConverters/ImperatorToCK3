using commonItems;
using System;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Imperator.Provinces {
	[Collection("Sequential")]
	[CollectionDefinition("Sequential", DisableParallelization = true)]
	public class ProvincesTests {
		[Fact]
		public void ProvincesDefaultToEmpty() {
			var reader = new BufferedReader("={}");
			var provinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection(reader);

			Assert.Empty(provinces);
		}

		[Fact]
		public void ProvincesCanBeLoaded() {
			var reader = new BufferedReader(
				"=\n" +
				"{\n" +
				"42={}\n" +
				"43={}\n" +
				"}"
			);
			var provinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection(reader);

			Assert.Equal((ulong)42, provinces[42].Id);
			Assert.Equal((ulong)43, provinces[43].Id);
		}

		[Fact]
		public void PopCanBeLinked() {
			var reader = new BufferedReader("={42={pop=8}}\n");
			var provinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection(reader);

			var reader2 = new BufferedReader(
				 "8={type=\"citizen\" culture=\"roman\" religion=\"paradoxian\"}\n"
			);
			var pops = new ImperatorToCK3.Imperator.Pops.Pops();
			pops.LoadPops(reader2);
			provinces.LinkPops(pops);

			var province = provinces[42];
			var pop = province.Pops[8];

			Assert.NotNull(pop);
			Assert.Equal("citizen", pop.Type);
		}

		[Fact]
		public void MultiplePopsCanBeLinked() {
			var reader = new BufferedReader(
				"={\n" +
				"43={ pop = 10}\n" +
				"42={pop=8}\n" +
				"44={pop= 9}\n" +
				"}\n"
			);
			var provinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection(reader);

			var reader2 = new BufferedReader(
				"={\n" +
				"8={type=\"citizen\" culture=\"roman\" religion=\"paradoxian\"}\n" +
				"9={type=\"tribal\" culture=\"persian\" religion=\"gsg\"}\n" +
				"10={type=\"freemen\" culture=\"greek\" religion=\"zoroastrian\"}\n" +
				"}\n"
			);
			var pops = new ImperatorToCK3.Imperator.Pops.Pops();
			pops.LoadPops(reader2);
			provinces.LinkPops(pops);

			var province = provinces[42];
			var pop = province.Pops[8];
			var province2 = provinces[43];
			var pop2 = province2.Pops[10];
			var province3 = provinces[44];
			var pop3 = province3.Pops[9];

			Assert.NotNull(pop);
			Assert.Equal("citizen", pop.Type);
			Assert.NotNull(pop2);
			Assert.Equal("freemen", pop2.Type);
			Assert.NotNull(pop3);
			Assert.Equal("tribal", pop3.Type);
		}

		[Fact]
		public void BrokenLinkAttemptThrowsWarning() {
			var reader = new BufferedReader(
				"={\n" +
				"42={ pop = 8 }\n" +
				"44={ pop = 10 }\n" + // no pop 10
				"}\n"
			);
			var provinces = new ImperatorToCK3.Imperator.Provinces.ProvinceCollection(reader);

			var reader2 = new BufferedReader(
				"={\n" +
				"8={type=\"citizen\" culture=\"roman\" religion=\"paradoxian\"}\n" +
				"9={type=\"tribal\" culture=\"persian\" religion=\"gsg\"}\n" +
				"}\n"
			);
			var pops = new ImperatorToCK3.Imperator.Pops.Pops();
			pops.LoadPops(reader2);

			var output = new StringWriter();
			Console.SetOut(output);

			provinces.LinkPops(pops);

			var logStr = output.ToString();
			Assert.Contains("[WARN] Pop with ID 10 has no definition!", logStr);
		}
	}
}
