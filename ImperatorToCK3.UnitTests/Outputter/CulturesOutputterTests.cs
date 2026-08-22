using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Mods;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.Outputter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class CulturesOutputterTests {
	[Fact]
	public async Task OutputCultures_WritesCulturesGroupedByHeritage_AndOrdersParentsBeforeChildren() {
		var tempDir = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempDir, "output");
			var culturesPath = Path.Combine(outputModPath, "common", "culture", "cultures");
			Directory.CreateDirectory(culturesPath);

			var config = new Configuration();
			var ck3ModFS = new ModFilesystem(outputModPath, Array.Empty<Mod>());

			var ck3ModFlags = new OrderedDictionary<string, bool>();
			var pillarCollection = new PillarCollection(new ColorFactory(), ck3ModFlags);
			var heritage = new Pillar("heritage_test", new PillarData { Type = "heritage" });
			var language = new Pillar("language_test", new PillarData { Type = "language" });
			pillarCollection.AddOrReplace(heritage);
			pillarCollection.AddOrReplace(language);

			var cultureCollection = new CultureCollection(new ColorFactory(), pillarCollection, ck3ModFlags);

			var nameList = new NameList("test_namelist", new BufferedReader("male_names = { testname }"));

			var parentCultureData = new CultureData {
				Color = new Color(1, 2, 3),
				Heritage = heritage,
				Language = language
			};
			parentCultureData.NameLists.Add(nameList);

			var childCultureData = new CultureData {
				Color = new Color(4, 5, 6),
				Heritage = heritage,
				Language = language
			};
			childCultureData.ParentCultureIds.Add("parent");
			childCultureData.NameLists.Add(nameList);

			var parentCulture = new Culture("parent", parentCultureData);
			var childCulture = new Culture("child", childCultureData);

			// Insert child first to ensure ordering logic is exercised.
			cultureCollection.AddOrReplace(childCulture);
			cultureCollection.AddOrReplace(parentCulture);

			await CulturesOutputter.OutputCultures(outputModPath, cultureCollection, ck3ModFS, config, new Date(867, 1, 1));

			var outputFile = Path.Combine(culturesPath, "heritage_test.txt");
			Assert.True(File.Exists(outputFile));

			var output = await File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);
			var parentIndex = output.IndexOf("parent =", StringComparison.Ordinal);
			var childIndex = output.IndexOf("child =", StringComparison.Ordinal);
			Assert.True(parentIndex >= 0);
			Assert.True(childIndex >= 0);
			Assert.True(parentIndex < childIndex);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task OutputCultures_OutputsCCUParameters_WhenConfigured() {
		var tempDir = CreateTempDir();
		try {
			var currentDirectory = Directory.GetCurrentDirectory();
			var configurablesDir = Path.Combine(currentDirectory, "configurables");
			Directory.CreateDirectory(configurablesDir);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_heritage_parameters.txt"),
				"""
				heritage_families = {}
				heritage_groups = {
					MOD_DEPENDENT = {
						IF tfe = {
							heritage_group_nuragic
						}
					}
				}
				""",
				TestContext.Current.CancellationToken
			);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_language_parameters.txt"),
				"""
				language_families = {
					MOD_DEPENDENT = {
						IF tfe = {
							language_family_kra_dai
						}
					}
				}
				language_branches = {}
				language_groups = {
					MOD_DEPENDENT = {
						IF tfe = {
							language_group_tai
						}
					}
				}
				""",
				TestContext.Current.CancellationToken
			);

			var outputModPath = Path.Combine(tempDir, "output");
			Directory.CreateDirectory(Path.Combine(outputModPath, "common", "scripted_effects"));

			var ck3ModRoot = Path.Combine(tempDir, "ck3mod");
			var scriptedEffectsDir = Path.Combine(ck3ModRoot, "common", "scripted_effects");
			Directory.CreateDirectory(scriptedEffectsDir);
			var inputScriptedEffectsPath = Path.Combine(scriptedEffectsDir, "ccu_scripted_effects.txt");
			await File.WriteAllTextAsync(inputScriptedEffectsPath,
				"""
				ccu_initialize_culture = {
					if = { set_variable = { name = heritage_family value = 1 } }
					if = { set_variable = { name = heritage_group value = 2 } }
					if = { set_variable = { name = language_family value = 3 } }
					if = { set_variable = { name = language_group value = 4 } }
				}
				""",
				TestContext.Current.CancellationToken);

			var config = new Configuration();
			config.DetectSpecificCK3Mods([new Mod("The Fallen Eagle", "", dependencies: [])]);

			var ck3ModFS = new ModFilesystem(ck3ModRoot, Array.Empty<Mod>());
			var emptyPillars = new PillarCollection(new ColorFactory(), new OrderedDictionary<string, bool>());
			var emptyCultures = new CultureCollection(new ColorFactory(), emptyPillars, new OrderedDictionary<string, bool>());

			await CulturesOutputter.OutputCultures(outputModPath, emptyCultures, ck3ModFS, config, new Date(867, 1, 1));

			var outputScriptedEffectsPath = Path.Combine(outputModPath, "common", "scripted_effects", "ccu_scripted_effects.txt");
			Assert.True(File.Exists(outputScriptedEffectsPath));

			var output = await File.ReadAllTextAsync(outputScriptedEffectsPath, TestContext.Current.CancellationToken);
			Assert.Contains("has_cultural_parameter = heritage_group_nuragic", output, StringComparison.Ordinal);
			Assert.Contains("has_cultural_parameter = language_family_kra_dai", output, StringComparison.Ordinal);
			Assert.Contains("has_cultural_parameter = language_group_tai", output, StringComparison.Ordinal);

			// TFE path uses numeric assignment rather than flag assignment.
			Assert.DoesNotContain("value = flag:language_family_kra_dai", output, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "CulturesOutputter", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static void TryDeleteDir(string dir) {
		try {
			if (Directory.Exists(dir)) {
				Directory.Delete(dir, recursive: true);
			}
		} catch {
			// Best-effort cleanup only.
		}
	}
}
