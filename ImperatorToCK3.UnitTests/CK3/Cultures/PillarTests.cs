using commonItems;
using commonItems.Colors;
using ImperatorToCK3.CK3.Cultures;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Cultures; 

public class PillarTests {
	private readonly ColorFactory colorFactory = new();

	[Fact]
	public void PillarIsCorrectlyInitialized() {
		var pillar = new Pillar("test_pillar", new PillarData { Type = "test_type" });
		Assert.Equal("test_pillar", pillar.Id);
		Assert.Equal("test_type", pillar.Type);
	}

	[Fact]
	public void Serialize_WithBracesAndNoColorOrParameters() {
		var pillar = new Pillar("p", new PillarData { Type = "t" });

		var result = pillar.Serialize(indent: "", withBraces: true);

		Assert.Contains("{", result);
		Assert.Contains("}", result);
		Assert.Contains("type=t", result);
		Assert.DoesNotContain("color=", result);
		Assert.DoesNotContain("parameters=", result);
	}

	[Fact]
	public void Serialize_WithBracesColorAndParameters() {
		var data = new PillarData {
			Type = "t",
			Color = colorFactory.GetColor(new BufferedReader("rgb { 1 2 3 }")),
			Parameters = new() { ["key1"] = "val1" }
		};
		var pillar = new Pillar("p", data);

		var result = pillar.Serialize(indent: "", withBraces: true);

		Assert.Contains("{", result);
		Assert.Contains("}", result);
		Assert.Contains("type=t", result);
		Assert.Contains("color=", result);
		Assert.Contains("parameters=", result);
		Assert.Contains("key1", result);
	}

	[Fact]
	public void Serialize_WithoutBracesAndNoColorOrParameters() {
		var pillar = new Pillar("p", new PillarData { Type = "t" });

		var result = pillar.Serialize(indent: "", withBraces: false);

		Assert.DoesNotContain("{", result);
		Assert.DoesNotContain("}", result);
		Assert.Contains("type=t", result);
		Assert.DoesNotContain("color=", result);
		Assert.DoesNotContain("parameters=", result);
	}

	[Fact]
	public void Serialize_WithoutBracesColorAndNoParameters() {
		var data = new PillarData {
			Type = "t",
			Color = colorFactory.GetColor(new BufferedReader("rgb { 1 2 3 }"))
		};
		var pillar = new Pillar("p", data);

		var result = pillar.Serialize(indent: "", withBraces: false);

		// No outer braces: output starts with the type line, not "{".
		Assert.True(result.TrimStart().StartsWith("type"));
		Assert.Contains("type=t", result);
		Assert.Contains("color=", result);
		Assert.DoesNotContain("parameters=", result);
	}
}