using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using DotLiquid;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.Mappers.Technology;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.CK3.Cultures;

[Collection("Sequential")]
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class CultureTests {
	private readonly ColorFactory colorFactory = new();

	private CultureData MakeCultureData() {
		var heritage = new Pillar("heritage_pillar", new PillarData { Type = "heritage" });
		var language = new Pillar("language_pillar", new PillarData { Type = "language" });
		return new CultureData {
			Color = colorFactory.GetColor(new BufferedReader("rgb { 100 150 200 }")),
			Heritage = heritage,
			Language = language,
			TraditionIds = new OrderedSet<string> { "tradition_1" }
		};
	}

	[Fact]
	public void Ctor_KeepsOnlyLastBuildingGfxAttribute() {
		var cultureData = MakeCultureData();
		cultureData.Attributes.AddRange([
			new KeyValuePair<string, StringOfItem>("building_gfx", new StringOfItem("gfx_first")),
			new KeyValuePair<string, StringOfItem>("other_attr", new StringOfItem("value")),
			new KeyValuePair<string, StringOfItem>("building_gfx", new StringOfItem("gfx_last"))
		]);

		var culture = new Culture("test_culture", cultureData);

		var buildingGfxAttrs = culture.Attributes.Where(pair => pair.Key == "building_gfx").ToList();
		Assert.Single(buildingGfxAttrs);
		Assert.Equal("gfx_last", buildingGfxAttrs[0].Value.ToString());
		Assert.Contains(culture.Attributes, pair => pair.Key == "other_attr");
	}

	[Fact]
	public void Ctor_KeepsSingleBuildingGfxAttribute() {
		var cultureData = MakeCultureData();
		cultureData.Attributes.Add(
			new KeyValuePair<string, StringOfItem>("building_gfx", new StringOfItem("gfx_only"))
		);

		var culture = new Culture("test_culture", cultureData);

		Assert.Single(culture.Attributes, pair => pair.Key == "building_gfx");
	}

	[Fact]
	public void Serialize_WithBraces_IncludesBracesAndParents() {
		var cultureData = MakeCultureData();
		cultureData.ParentCultureIds = new OrderedSet<string> { "parent_culture" };
		var culture = new Culture("test_culture", cultureData);

		var result = culture.Serialize(indent: "", withBraces: true);

		Assert.Contains("{", result);
		Assert.Contains("}", result);
		Assert.Contains("color =", result);
		Assert.Contains("parents =", result);
		Assert.Contains("parent_culture", result);
		Assert.Contains("heritage = heritage_pillar", result);
		Assert.Contains("language = language_pillar", result);
		Assert.Contains("traditions =", result);
		Assert.Contains("tradition_1", result);
	}

	[Fact]
	public void Serialize_WithoutBraces_OmitsBracesAndParents() {
		var cultureData = MakeCultureData();
		var nameList = new NameList("name_list_1", new BufferedReader(
			"{ male_names = { John } female_names = { Jane } }"
		));
		cultureData.NameLists.Add(nameList);
		var culture = new Culture("test_culture", cultureData);

		var result = culture.Serialize(indent: "", withBraces: false);

		// No outer braces: output starts with the color line, not "{".
		Assert.StartsWith("color", result.TrimStart());
		Assert.DoesNotContain("parents =", result);
		Assert.Contains("heritage = heritage_pillar", result);
		Assert.Contains("name_list = name_list_1", result);
	}

	[Fact]
	public async Task OutputHistory_DoesNothingWhenNoInnovationsAndNoFallenEagle() {
		var culture = new Culture("test_culture", MakeCultureData());
		var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		try {
			await culture.OutputHistory(tempDir, new Configuration(), new Date(867, 1, 1));

			Assert.False(File.Exists(Path.Combine(tempDir, "history/cultures", "test_culture.txt")));
		} finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, recursive: true);
			}
		}
	}

	[Fact]
	public async Task OutputHistory_WritesDiscoveredInnovationsAndProgress() {
		var culture = new Culture("test_culture", MakeCultureData());
		var mapper = LoadInnovationMapper("""
			link = { ir = inv_link_1 ck3 = innovation_linked }
			bonus = { ir = inv_bonus_1 ir = inv_bonus_2 ck3 = innovation_bonus }
			""");
		culture.ImportInnovationsFromImperator(
			new[] { "inv_link_1", "inv_bonus_1" }.ToFrozenSet(),
			mapper
		);
		Assert.Contains("innovation_linked", culture.InnovationsFromImperator);
		Assert.Contains("innovation_bonus", culture.InnovationProgressesFromImperator.Keys);

		var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		try {
			Directory.CreateDirectory(Path.Combine(tempDir, "history/cultures"));
			await culture.OutputHistory(tempDir, new Configuration(), new Date(867, 1, 1));

			var historyText = await File.ReadAllTextAsync(
				Path.Combine(tempDir, "history/cultures", "test_culture.txt"), TestContext.Current.CancellationToken);
			Assert.Contains("discover_innovation = innovation_linked", historyText);
			Assert.Contains("culture_innovation = innovation_bonus", historyText);
			Assert.Contains("progress =", historyText);
			Assert.DoesNotContain("join_era", historyText);
		} finally {
			Directory.Delete(tempDir, recursive: true);
		}
	}

	[Fact]
	public async Task OutputHistory_WithFallenEagle_WritesJoinEraLine() {
		var culture = new Culture("test_culture", MakeCultureData());
		var mapper = LoadInnovationMapper(
			"bonus = { ir = inv_bonus_1 ir = inv_bonus_2 ir = inv_bonus_3 ir = inv_bonus_4 ck3 = innovation_bonus }");
		// Bonus fully matched (4x25) => progress 100 => discovered innovation.
		culture.ImportInnovationsFromImperator(
			new[] { "inv_bonus_1", "inv_bonus_2", "inv_bonus_3", "inv_bonus_4" }.ToFrozenSet(), mapper);
		Assert.Contains("innovation_bonus", culture.InnovationsFromImperator);
		Assert.Empty(culture.InnovationProgressesFromImperator);

		var config = new Configuration();
		var flagsField = typeof(Configuration).GetField("activeCK3ModFlags",
			BindingFlags.Instance | BindingFlags.NonPublic)!;
		((HashSet<string>)flagsField.GetValue(config)!).Add("tfe");

		var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		try {
			Directory.CreateDirectory(Path.Combine(tempDir, "history/cultures"));
			await culture.OutputHistory(tempDir, config, new Date(867, 1, 1));

			var historyText = await File.ReadAllTextAsync(
				Path.Combine(tempDir, "history/cultures", "test_culture.txt"), TestContext.Current.CancellationToken);
			Assert.Contains("join_era = culture_era_classical_antiquity", historyText);
			Assert.Contains("discover_innovation = innovation_bonus", historyText);
		} finally {
			Directory.Delete(tempDir, recursive: true);
		}
	}

	[Fact]
	public void MaleAndFemaleNames_AreAggregatedFromNameLists() {
		var cultureData = MakeCultureData();
		cultureData.NameLists.Add(new NameList("nl1", new BufferedReader(
			"{ male_names = { John } female_names = { Jane } }"
		)));
		cultureData.NameLists.Add(new NameList("nl2", new BufferedReader(
			"{ male_names = { Bob } female_names = { Alice } }"
		)));
		var culture = new Culture("test_culture", cultureData);

		Assert.Equal(new List<string> { "John", "Bob" }, culture.MaleNames);
		Assert.Equal(new List<string> { "Jane", "Alice" }, culture.FemaleNames);
	}

	[Fact]
	public void MaleAndFemaleNames_AreEmptyWithoutNameLists() {
		var culture = new Culture("test_culture", MakeCultureData());

		Assert.Empty(culture.MaleNames);
		Assert.Empty(culture.FemaleNames);
	}

	private static InnovationMapper LoadInnovationMapper(string mapContent) {
		var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".liquid");
		try {
			File.WriteAllText(tempPath, mapContent);
			var mapper = new InnovationMapper();
			mapper.LoadLinksAndBonuses(tempPath, new Hash());
			return mapper;
		} finally {
			File.Delete(tempPath);
		}
	}
}
