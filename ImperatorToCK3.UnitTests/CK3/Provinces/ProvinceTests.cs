using commonItems;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.Mappers.Culture;
using ImperatorToCK3.Mappers.Religion;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Provinces {
	public class ProvinceTests {
		[Fact]
		public void FieldsDefaultToCorrectValues() {
			var province = new Province();
			Assert.Equal((ulong)0, province.Id);
			Assert.Equal(string.Empty, province.Religion);
			Assert.Equal(string.Empty, province.Culture);
			Assert.Equal("none", province.Holding);
		}

		[Fact]
		public void FieldsCanBeSet() {
			var province = new Province();
			province.Religion = "orthodox";
			Assert.Equal("orthodox", province.Religion);
			province.Culture = "roman";
			Assert.Equal("roman", province.Culture);
		}

		[Fact]
		public void ProvinceCanBeLoadedFromStream() {
			var reader = new BufferedReader(
				"{ culture=roman random_key=random_value religion=orthodox holding=castle_holding }"
			);
			var province = new Province(42, reader, new Date(867, 1, 1));
			Assert.Equal((ulong)42, province.Id);
			Assert.Equal("orthodox", province.Religion);
			Assert.Equal("roman", province.Culture);
			Assert.Equal("castle_holding", province.Holding);
		}

		[Fact]
		public void SetHoldingLogicWorks() {
			var reader1 = new BufferedReader(" = { province_rank=city_metropolis }");
			var reader2 = new BufferedReader(" = { province_rank=city fort=yes }");
			var reader3 = new BufferedReader(" = { province_rank=city }");
			var reader4 = new BufferedReader(" = { province_rank=settlement holy_site = 69 fort=yes }");
			var reader5 = new BufferedReader(" = { province_rank=settlement fort=yes }");
			var reader6 = new BufferedReader(" = { province_rank=settlement }");

			var impProvince = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader1, 42);
			var impProvince2 = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader2, 43);
			var impProvince3 = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader3, 44);
			var impProvince4 = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader4, 45);
			var impProvince5 = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader5, 46);
			var impProvince6 = ImperatorToCK3.Imperator.Provinces.Province.Parse(reader6, 47);

			var province1 = new Province();
			var province2 = new Province();
			var province3 = new Province();
			var province4 = new Province();
			var province5 = new Province();
			var province6 = new Province();

			var cultureMapper = new CultureMapper();
			var religionMapper = new ReligionMapper();

			province1.InitializeFromImperator(impProvince, cultureMapper, religionMapper);
			province2.InitializeFromImperator(impProvince2, cultureMapper, religionMapper);
			province3.InitializeFromImperator(impProvince3, cultureMapper, religionMapper);
			province4.InitializeFromImperator(impProvince4, cultureMapper, religionMapper);
			province5.InitializeFromImperator(impProvince5, cultureMapper, religionMapper);
			province6.InitializeFromImperator(impProvince6, cultureMapper, religionMapper);

			Assert.Equal("city_holding", province1.Holding);
			Assert.Equal("castle_holding", province2.Holding);
			Assert.Equal("city_holding", province3.Holding);
			Assert.Equal("church_holding", province4.Holding);
			Assert.Equal("castle_holding", province5.Holding);
			Assert.Equal("tribal_holding", province6.Holding);
		}
	}
}
