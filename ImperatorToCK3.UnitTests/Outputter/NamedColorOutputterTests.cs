using commonItems.Colors;
using ImperatorToCK3.Outputter;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class NamedColorOutputterTests {
	[Fact]
	public async Task OutputterOutputsColorsNotFoundInCK3ColorCollection() {
		var imperatorColors = new NamedColorCollection {
			["a"] = new(1, 1, 1),
			["b"] = new(2, 2, 2),
			["c"] = new(3, 3, 3),
			["d"] = new(4, 4, 4)
		};

		var ck3Colors = new NamedColorCollection {
			["a"] = new(1, 1, 1), // same name and color as in Imperator
			["b"] = new(69, 69, 69) // same name as in Imperator
		};

		Directory.CreateDirectory("output/colors_test/common/named_colors");
		await NamedColorsOutputter.OutputNamedColors("output/colors_test", imperatorColors, ck3Colors);
		var output = await File.ReadAllTextAsync("output/colors_test/common/named_colors/IRtoCK3_colors_from_Imperator.txt");
		Assert.DoesNotContain("a=", output);
		Assert.DoesNotContain("b=", output);
		Assert.DoesNotContain("c=rgb {3 3 3}", output);
		Assert.DoesNotContain("d=rgb {4 4 4}", output);
	}

	[Fact]
	public async Task OutputterOutputsNothingWhenThereIsNothingToOutput() {
		var imperatorColors = new NamedColorCollection {
			["a"] = new(1, 1, 1),
			["b"] = new(2, 2, 2),
		};

		var ck3Colors = new NamedColorCollection {
			// same colors as in Imperator, there is no need to output anything
			["a"] = new(1, 1, 1),
			["b"] = new(2, 2, 2),
		};

		Directory.CreateDirectory("output/colors_test/common/named_colors");
		await NamedColorsOutputter.OutputNamedColors("colors_test2", imperatorColors, ck3Colors);
		Assert.False(File.Exists("output/colors_test2/common/named_colors/IRtoCK3_colors_from_Imperator.txt"));
	}
}