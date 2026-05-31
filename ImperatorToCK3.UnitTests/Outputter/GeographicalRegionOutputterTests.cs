using commonItems.Mods;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Mappers.Region;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class GeographicalRegionOutputterTests {
	[Fact]
	public async Task RegionsAreOutputtedToCanonicalFile() {
		var tempRoot = CreateTempDir();
		try {
			var ck3Root = CreateCk3RegionFiles(tempRoot);
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			Directory.CreateDirectory(Path.Combine(outputModPath, "map_data", "geographical_regions"));

			var regionMapper = LoadRegionMapper(ck3Root);

			await GeographicalRegionOutputter.OutputRegions(outputModPath, regionMapper);

			var actualText = await ReadOutput(outputModPath);
			var expectedText = TextTestUtils.NormalizeNewlines(
				"""
				world_region = {
					provinces = { 1 2 }
				}
				custom_region = {
					regions = { world_region }
					provinces = { 3 }
				}

				"""
			);

			Assert.Equal(expectedText, actualText);
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task StaleRegionFilesAreDeletedAfterOutput() {
		var tempRoot = CreateTempDir();
		try {
			var ck3Root = CreateCk3RegionFiles(tempRoot);
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			var outputDir = Path.Combine(outputModPath, "map_data", "geographical_regions");
			Directory.CreateDirectory(outputDir);
			await File.WriteAllTextAsync(Path.Combine(outputDir, "old_regions.txt"), "obsolete", TestContext.Current.CancellationToken);
			await File.WriteAllTextAsync(Path.Combine(outputDir, "another_old_file.txt"), "obsolete", TestContext.Current.CancellationToken);
			await File.WriteAllTextAsync(Path.Combine(outputDir, "irtock3_all_regions.txt"), "old content", TestContext.Current.CancellationToken);

			var regionMapper = LoadRegionMapper(ck3Root);

			await GeographicalRegionOutputter.OutputRegions(outputModPath, regionMapper);

			Assert.False(File.Exists(Path.Combine(outputDir, "old_regions.txt")));
			Assert.False(File.Exists(Path.Combine(outputDir, "another_old_file.txt")));
			Assert.True(File.Exists(Path.Combine(outputDir, "irtock3_all_regions.txt")));
			Assert.DoesNotContain("old content", await ReadOutput(outputModPath));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	private static CK3RegionMapper LoadRegionMapper(string ck3Root) {
		var mapper = new CK3RegionMapper();
		mapper.LoadRegions(new ModFilesystem(ck3Root, Array.Empty<Mod>()), new Title.LandedTitles());
		return mapper;
	}

	private static string CreateCk3RegionFiles(string tempRoot) {
		var ck3Root = Path.Combine(tempRoot, "ck3");
		var regionsDir = Path.Combine(ck3Root, "map_data", "geographical_regions");
		Directory.CreateDirectory(regionsDir);
		Directory.CreateDirectory(Path.Combine(ck3Root, "map_data"));

		File.WriteAllText(
			Path.Combine(regionsDir, "test_regions.txt"),
			"""
			world_region = {
				provinces = { 1 2 }
			}
			custom_region = {
				regions = { world_region }
				provinces = { 3 }
			}
			"""
		);
		File.WriteAllText(Path.Combine(ck3Root, "map_data", "island_region.txt"), string.Empty);

		return ck3Root;
	}

	private static async Task<string> ReadOutput(string outputModPath) {
		var outputPath = Path.Combine(outputModPath, "map_data", "geographical_regions", "irtock3_all_regions.txt");
		var text = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		return TextTestUtils.NormalizeNewlines(text);
	}

	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "GeographicalRegionOutputter", Guid.NewGuid().ToString("N"));
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