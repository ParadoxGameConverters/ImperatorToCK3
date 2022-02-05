using commonItems;
using ImperatorToCK3.CK3.Provinces;
using ImperatorToCK3.Outputter;
using System.IO;
using System.Text;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class ProvinceOutputterTests {
	private readonly Date date = new(400, 1, 1);
	[Fact]
	public void CultureIsOutputted() {
		var provReader = new BufferedReader("culture=roman");
		var province = new Province(1, provReader, date);

		var sb = new StringBuilder();
		var sw = new StringWriter(sb);
		ProvinceOutputter.OutputProvince(sw, province);

		var sr = new StringReader(sb.ToString());
		Assert.Equal("1={", sr.ReadLine());
		Assert.Equal("\tculture=roman", sr.ReadLine());
		Assert.Equal("\tholding=none", sr.ReadLine());
		Assert.Equal("}", sr.ReadLine());
	}
	[Fact]
	public void ReligionIsOutputted() {
		var provReader = new BufferedReader("religion=orthodox");
		var province = new Province(1, provReader, date);

		var sb = new StringBuilder();
		var sw = new StringWriter(sb);
		ProvinceOutputter.OutputProvince(sw, province);

		var sr = new StringReader(sb.ToString());
		Assert.Equal("1={", sr.ReadLine());
		Assert.Equal("\treligion=orthodox", sr.ReadLine());
		Assert.Equal("\tholding=none", sr.ReadLine());
		Assert.Equal("}", sr.ReadLine());
	}
	[Fact]
	public void HoldingIsOutputted() {
		var provReader = new BufferedReader("holding=castle_holding");
		var province = new Province(1, provReader, date);

		var sb = new StringBuilder();
		var sw = new StringWriter(sb);
		ProvinceOutputter.OutputProvince(sw, province);

		var sr = new StringReader(sb.ToString());
		Assert.Equal("1={", sr.ReadLine());
		Assert.Equal("\tholding=castle_holding", sr.ReadLine());
		Assert.Equal("}", sr.ReadLine());
	}
	[Fact]
	public void BuildingsAreOutputted() {
		var provReader = new BufferedReader("= { buildings = { b1 b2 } }");
		var province = new Province(1, provReader, date);

		var sb = new StringBuilder();
		var sw = new StringWriter(sb);
		ProvinceOutputter.OutputProvince(sw, province);

		var sr = new StringReader(sb.ToString());
		Assert.Equal("1={", sr.ReadLine());
		Assert.Equal("\tholding=none", sr.ReadLine());
		Assert.Equal("\tbuildings={", sr.ReadLine());
		Assert.Equal("\t\tb1", sr.ReadLine());
		Assert.Equal("\t\tb2", sr.ReadLine());
		Assert.Equal("\t}", sr.ReadLine());
		Assert.Equal("}", sr.ReadLine());
	}
}