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
							heritage_group_second
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
					set_variable = { name = heritage_family value = 100 }
					set_variable = { name = heritage_group value = 100 }
					set_variable = { name = language_family value = 100 }
					set_variable = { name = language_group value = 100 }
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
			Assert.Contains("has_cultural_parameter = heritage_group_second", output, StringComparison.Ordinal);
			Assert.Contains("has_cultural_parameter = language_family_kra_dai", output, StringComparison.Ordinal);
			Assert.Contains("has_cultural_parameter = language_group_tai", output, StringComparison.Ordinal);

			// TFE path uses numeric assignment rather than flag assignment.
			Assert.DoesNotContain("value = flag:language_family_kra_dai", output, StringComparison.Ordinal);

			// Numeric parameters continue after the last already-used value (100).
			Assert.Contains("value = 101", output, StringComparison.Ordinal);
			Assert.Contains("value = 102", output, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task OutputCultures_CCUTFE_MissingPrecedingChildren_SkipsParameterAddition() {
		var tempDir = CreateTempDir();
		try {
			var currentDirectory = Directory.GetCurrentDirectory();
			var configurablesDir = Path.Combine(currentDirectory, "configurables");
			Directory.CreateDirectory(configurablesDir);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_heritage_parameters.txt"),
				"heritage_families = { heritage_family_test }\nheritage_groups = { heritage_group_test }",
				TestContext.Current.CancellationToken
			);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_language_parameters.txt"),
				"language_families = { language_family_test }\nlanguage_branches = {}\nlanguage_groups = { language_group_test }",
				TestContext.Current.CancellationToken
			);

			var outputModPath = Path.Combine(tempDir, "output");
			Directory.CreateDirectory(Path.Combine(outputModPath, "common", "scripted_effects"));

			var ck3ModRoot = Path.Combine(tempDir, "ck3mod");
			var scriptedEffectsDir = Path.Combine(ck3ModRoot, "common", "scripted_effects");
			Directory.CreateDirectory(scriptedEffectsDir);
			await File.WriteAllTextAsync(Path.Combine(scriptedEffectsDir, "ccu_scripted_effects.txt"),
				"""
				ccu_initialize_culture = {}
				""", TestContext.Current.CancellationToken);

			var config = new Configuration();
			config.DetectSpecificCK3Mods([new Mod("The Fallen Eagle", "", dependencies: [])]);

			var ck3ModFS = new ModFilesystem(ck3ModRoot, Array.Empty<Mod>());
			var emptyPillars = new PillarCollection(new ColorFactory(), new OrderedDictionary<string, bool>());
			var emptyCultures = new CultureCollection(new ColorFactory(), emptyPillars, new OrderedDictionary<string, bool>());

			await CulturesOutputter.OutputCultures(outputModPath, emptyCultures, ck3ModFS, config, new Date(867, 1, 1));

			var outputPath = Path.Combine(outputModPath, "common", "scripted_effects", "ccu_scripted_effects.txt");
			Assert.True(File.Exists(outputPath));
			var output = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
			Assert.DoesNotContain("heritage_family_test", output, StringComparison.Ordinal);
			Assert.DoesNotContain("heritage_group_test", output, StringComparison.Ordinal);
			Assert.DoesNotContain("language_family_test", output, StringComparison.Ordinal);
			Assert.DoesNotContain("language_group_test", output, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task OutputCultures_CCUWithWtWSMS_AddsAllParameterTypesAndErrorSuppression() {
		var tempDir = CreateTempDir();
		try {
			var currentDirectory = Directory.GetCurrentDirectory();
			var configurablesDir = Path.Combine(currentDirectory, "configurables");
			Directory.CreateDirectory(configurablesDir);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_heritage_parameters.txt"),
				"""
				heritage_families = {
					MOD_DEPENDENT = {
						IF wtwsms = {
							heritage_family_test
						}
					}
				}
				heritage_groups = {
					MOD_DEPENDENT = {
						IF wtwsms = {
							heritage_group_test
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
						IF wtwsms = {
							language_family_test
						}
					}
				}
				language_branches = {
					MOD_DEPENDENT = {
						IF wtwsms = {
							language_branch_test
						}
					}
				}
				language_groups = {
					MOD_DEPENDENT = {
						IF wtwsms = {
							language_group_test
						}
					}
				}
				""",
				TestContext.Current.CancellationToken
			);

			var outputModPath = Path.Combine(tempDir, "output");
			Directory.CreateDirectory(Path.Combine(outputModPath, "common", "scripted_effects"));
			// OutputCCUErrorSuppression writes into common/scripted_guis under the output mod.
			Directory.CreateDirectory(Path.Combine(outputModPath, "common", "scripted_guis"));

			var ck3ModRoot = Path.Combine(tempDir, "ck3mod");
			var scriptedEffectsDir = Path.Combine(ck3ModRoot, "common", "scripted_effects");
			Directory.CreateDirectory(scriptedEffectsDir);
			await File.WriteAllTextAsync(Path.Combine(scriptedEffectsDir, "ccu_scripted_effects.txt"),
				string.Join('\n',
					"ccu_initialize_heritage_family_effect = {}",
					"ccu_initialize_heritage_group_effect = {}",
					"ccu_initialize_language_family_effect = {}",
					"ccu_initialize_language_branch_effect = {}",
					"ccu_initialize_language_group_effect = {}"
				), TestContext.Current.CancellationToken);

			var scriptedGuisDir = Path.Combine(ck3ModRoot, "common", "scripted_guis");
			Directory.CreateDirectory(scriptedGuisDir);
			await File.WriteAllTextAsync(Path.Combine(scriptedGuisDir, "ccu_error_suppression.txt"),
				string.Join('\n',
					"if = { limit = { var:temp = flag:heritage_family_vanilla } set_variable = { name = temp value = flag:heritage_family_vanilla } }",
					"if = { limit = { var:temp = flag:heritage_group_vanilla } set_variable = { name = temp value = flag:heritage_group_vanilla } }",
					"if = { limit = { var:temp = flag:language_family_vanilla } set_variable = { name = temp value = flag:language_family_vanilla } }",
					"if = { limit = { var:temp = flag:language_branch_vanilla } set_variable = { name = temp value = flag:language_branch_vanilla } }",
					"if = { limit = { var:temp = flag:language_group_vanilla } set_variable = { name = temp value = flag:language_group_vanilla } }"
				), TestContext.Current.CancellationToken);

			var config = new Configuration();
			config.DetectSpecificCK3Mods([new Mod("When the World Stopped Making Sense", "", dependencies: [])]);

			var ck3ModFS = new ModFilesystem(ck3ModRoot, Array.Empty<Mod>());
			var emptyPillars = new PillarCollection(new ColorFactory(), new OrderedDictionary<string, bool>());
			var emptyCultures = new CultureCollection(new ColorFactory(), emptyPillars, new OrderedDictionary<string, bool>());

			await CulturesOutputter.OutputCultures(outputModPath, emptyCultures, ck3ModFS, config, new Date(867, 1, 1));

			var outputScriptedEffects = await File.ReadAllTextAsync(
				Path.Combine(outputModPath, "common", "scripted_effects", "ccu_scripted_effects.txt"),
				TestContext.Current.CancellationToken);
			foreach (var parameter in new[] {
					"heritage_family_test", "heritage_group_test",
					"language_family_test", "language_branch_test", "language_group_test"
				}) {
				Assert.Contains($"has_cultural_parameter = {parameter}", outputScriptedEffects, StringComparison.Ordinal);
				Assert.Contains($"flag:{parameter}", outputScriptedEffects, StringComparison.Ordinal);
			}

			var outputSuppression = await File.ReadAllTextAsync(
				Path.Combine(outputModPath, "common", "scripted_guis", "ccu_error_suppression.txt"),
				TestContext.Current.CancellationToken);
			foreach (var parameter in new[] {
					"heritage_family_test", "heritage_group_test",
					"language_family_test", "language_branch_test", "language_group_test"
				}) {
				Assert.Contains($"flag:{parameter}", outputSuppression, StringComparison.Ordinal);
			}
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task OutputCultures_CCUWithRajasOfAsia_OutputsLanguageBranchParameters() {
		var tempDir = CreateTempDir();
		try {
			var currentDirectory = Directory.GetCurrentDirectory();
			var configurablesDir = Path.Combine(currentDirectory, "configurables");
			Directory.CreateDirectory(configurablesDir);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_heritage_parameters.txt"),
				"heritage_families = {}\nheritage_groups = {}",
				TestContext.Current.CancellationToken
			);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_language_parameters.txt"),
				"""
				language_families = {}
				language_branches = {
					MOD_DEPENDENT = {
						IF roa = {
							language_branch_roa_test
						}
					}
				}
				language_groups = {}
				""",
				TestContext.Current.CancellationToken
			);

			var outputModPath = Path.Combine(tempDir, "output");
			Directory.CreateDirectory(Path.Combine(outputModPath, "common", "scripted_effects"));

			var ck3ModRoot = Path.Combine(tempDir, "ck3mod");
			var scriptedEffectsDir = Path.Combine(ck3ModRoot, "common", "scripted_effects");
			Directory.CreateDirectory(scriptedEffectsDir);
			await File.WriteAllTextAsync(Path.Combine(scriptedEffectsDir, "ccu_scripted_effects.txt"),
				"""
				ccu_initialize_heritage_family_effect = {}
				ccu_initialize_heritage_group_effect = {}
				ccu_initialize_language_family_effect = {}
				ccu_initialize_language_branch_effect = {}
				ccu_initialize_language_group_effect = {}
				""", TestContext.Current.CancellationToken);

			var config = new Configuration();
			config.DetectSpecificCK3Mods([new Mod("Rajas of Asia", "", dependencies: [])]);

			var ck3ModFS = new ModFilesystem(ck3ModRoot, Array.Empty<Mod>());
			var emptyPillars = new PillarCollection(new ColorFactory(), new OrderedDictionary<string, bool>());
			var emptyCultures = new CultureCollection(new ColorFactory(), emptyPillars, new OrderedDictionary<string, bool>());

			await CulturesOutputter.OutputCultures(outputModPath, emptyCultures, ck3ModFS, config, new Date(867, 1, 1));

			var outputScriptedEffects = await File.ReadAllTextAsync(
				Path.Combine(outputModPath, "common", "scripted_effects", "ccu_scripted_effects.txt"),
				TestContext.Current.CancellationToken);
			Assert.Contains("flag:language_branch_roa_test", outputScriptedEffects, StringComparison.Ordinal);
			Assert.False(File.Exists(Path.Combine(outputModPath, "common", "scripted_guis", "ccu_error_suppression.txt")), "Error suppression is only output for WtWSMS.");
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task OutputCultures_CCUWithoutScriptedEffectsFile_AbortsParameterOutput() {
		var tempDir = CreateTempDir();
		try {
			var currentDirectory = Directory.GetCurrentDirectory();
			var configurablesDir = Path.Combine(currentDirectory, "configurables");
			Directory.CreateDirectory(configurablesDir);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_heritage_parameters.txt"),
				"heritage_families = { heritage_family_test }\nheritage_groups = { heritage_group_test }",
				TestContext.Current.CancellationToken
			);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_language_parameters.txt"),
				"language_families = { language_family_test }\nlanguage_branches = { language_branch_test }\nlanguage_groups = { language_group_test }",
				TestContext.Current.CancellationToken
			);

			var outputModPath = Path.Combine(tempDir, "output");
			Directory.CreateDirectory(outputModPath);

			var ck3ModRoot = Path.Combine(tempDir, "ck3mod");
			Directory.CreateDirectory(ck3ModRoot); // no common/scripted_effects/ccu_scripted_effects.txt

			var config = new Configuration();
			config.DetectSpecificCK3Mods([new Mod("When the World Stopped Making Sense", "", dependencies: [])]);

			var ck3ModFS = new ModFilesystem(ck3ModRoot, Array.Empty<Mod>());
			var emptyPillars = new PillarCollection(new ColorFactory(), new OrderedDictionary<string, bool>());
			var emptyCultures = new CultureCollection(new ColorFactory(), emptyPillars, new OrderedDictionary<string, bool>());

			await CulturesOutputter.OutputCultures(outputModPath, emptyCultures, ck3ModFS, config, new Date(867, 1, 1));

			Assert.False(File.Exists(Path.Combine(outputModPath, "common", "scripted_effects", "ccu_scripted_effects.txt")));
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task OutputCultures_CCUWithMissingEffectNodes_SkipsParameterAdditionButOutputsFile() {
		var tempDir = CreateTempDir();
		try {
			var currentDirectory = Directory.GetCurrentDirectory();
			var configurablesDir = Path.Combine(currentDirectory, "configurables");
			Directory.CreateDirectory(configurablesDir);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_heritage_parameters.txt"),
				"heritage_families = { heritage_family_test }\nheritage_groups = { heritage_group_test }",
				TestContext.Current.CancellationToken
			);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_language_parameters.txt"),
				"language_families = { language_family_test }\nlanguage_branches = { language_branch_test }\nlanguage_groups = { language_group_test }",
				TestContext.Current.CancellationToken
			);

			var outputModPath = Path.Combine(tempDir, "output");
			Directory.CreateDirectory(Path.Combine(outputModPath, "common", "scripted_effects"));

			var ck3ModRoot = Path.Combine(tempDir, "ck3mod");
			var scriptedEffectsDir = Path.Combine(ck3ModRoot, "common", "scripted_effects");
			Directory.CreateDirectory(scriptedEffectsDir);
			await File.WriteAllTextAsync(Path.Combine(scriptedEffectsDir, "ccu_scripted_effects.txt"),
				"some_unrelated_effect = {}", TestContext.Current.CancellationToken);

			var config = new Configuration();
			config.DetectSpecificCK3Mods([new Mod("When the World Stopped Making Sense", "", dependencies: [])]);

			var ck3ModFS = new ModFilesystem(ck3ModRoot, Array.Empty<Mod>());
			var emptyPillars = new PillarCollection(new ColorFactory(), new OrderedDictionary<string, bool>());
			var emptyCultures = new CultureCollection(new ColorFactory(), emptyPillars, new OrderedDictionary<string, bool>());

			await CulturesOutputter.OutputCultures(outputModPath, emptyCultures, ck3ModFS, config, new Date(867, 1, 1));

			var outputPath = Path.Combine(outputModPath, "common", "scripted_effects", "ccu_scripted_effects.txt");
			Assert.True(File.Exists(outputPath));
			var output = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
			Assert.DoesNotContain("heritage_family_test", output, StringComparison.Ordinal);
			Assert.DoesNotContain("language_branch_test", output, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task OutputCultures_CCUWtWSMS_KeepsErrorSuppressionFileWithoutMarkers() {
		var tempDir = CreateTempDir();
		try {
			var currentDirectory = Directory.GetCurrentDirectory();
			var configurablesDir = Path.Combine(currentDirectory, "configurables");
			Directory.CreateDirectory(configurablesDir);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_heritage_parameters.txt"),
				"heritage_families = { heritage_family_test }\nheritage_groups = { heritage_group_test }",
				TestContext.Current.CancellationToken
			);
			await File.WriteAllTextAsync(
				Path.Combine(configurablesDir, "ccu_language_parameters.txt"),
				"language_families = { language_family_test }\nlanguage_branches = { language_branch_test }\nlanguage_groups = { language_group_test }",
				TestContext.Current.CancellationToken
			);

			var outputModPath = Path.Combine(tempDir, "output");
			Directory.CreateDirectory(Path.Combine(outputModPath, "common", "scripted_effects"));
			Directory.CreateDirectory(Path.Combine(outputModPath, "common", "scripted_guis"));

			var ck3ModRoot = Path.Combine(tempDir, "ck3mod");
			var scriptedEffectsDir = Path.Combine(ck3ModRoot, "common", "scripted_effects");
			Directory.CreateDirectory(scriptedEffectsDir);
			await File.WriteAllTextAsync(Path.Combine(scriptedEffectsDir, "ccu_scripted_effects.txt"),
				"""
				ccu_initialize_heritage_family_effect = {}
				ccu_initialize_heritage_group_effect = {}
				ccu_initialize_language_family_effect = {}
				ccu_initialize_language_branch_effect = {}
				ccu_initialize_language_group_effect = {}
				""", TestContext.Current.CancellationToken);

			var scriptedGuisDir = Path.Combine(ck3ModRoot, "common", "scripted_guis");
			Directory.CreateDirectory(scriptedGuisDir);
			await File.WriteAllTextAsync(Path.Combine(scriptedGuisDir, "ccu_error_suppression.txt"),
				"something_unrelated = yes", TestContext.Current.CancellationToken);

			var config = new Configuration();
			config.DetectSpecificCK3Mods([new Mod("When the World Stopped Making Sense", "", dependencies: [])]);

			var ck3ModFS = new ModFilesystem(ck3ModRoot, Array.Empty<Mod>());
			var emptyPillars = new PillarCollection(new ColorFactory(), new OrderedDictionary<string, bool>());
			var emptyCultures = new CultureCollection(new ColorFactory(), emptyPillars, new OrderedDictionary<string, bool>());

			await CulturesOutputter.OutputCultures(outputModPath, emptyCultures, ck3ModFS, config, new Date(867, 1, 1));

			var outputSuppressionPath = Path.Combine(outputModPath, "common", "scripted_guis", "ccu_error_suppression.txt");
			Assert.True(File.Exists(outputSuppressionPath));
			var outputSuppression = await File.ReadAllTextAsync(outputSuppressionPath, TestContext.Current.CancellationToken);
			Assert.Contains("something_unrelated = yes", outputSuppression, StringComparison.Ordinal);
			Assert.DoesNotContain("flag:heritage_family_test", outputSuppression, StringComparison.Ordinal);
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

	[Fact]
	public void IsModActiveHelper_ReturnsFalseForMissingKey() {
		OrderedDictionary<string, bool> flags = new() { ["tfe"] = true };
		// Use reflection to test private helper IsModActive
		var method = typeof(CulturesOutputter).GetMethod("IsModActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		Assert.NotNull(method);
		bool resultWtwsms = (bool)method!.Invoke(null, [flags, "wtwsms"])!;
		bool resultTfe = (bool)method.Invoke(null, [flags, "tfe"])!;
		bool resultMissing = (bool)method.Invoke(null, [flags, "nonexistent"])!;
		Assert.False(resultWtwsms);
		Assert.True(resultTfe);
		Assert.False(resultMissing);
	}
}
