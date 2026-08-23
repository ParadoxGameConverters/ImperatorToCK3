using commonItems.Mods;
using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class DecisionsOutputterTests {
	private const string RelativeDecisionPath = "common/decisions/dlc_decisions/ep3_decisions.txt";

	[Fact]
	public async Task ReturnsImmediatelyWhenByzantiumTitleIsMissing() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			Directory.CreateDirectory(Path.Combine(outputModPath, "common", "decisions", "dlc_decisions"));
			var ck3Root = Path.Combine(tempRoot, "ck3");
			WriteDecisionFile(ck3Root, DefaultDecisionFileText());

			await DecisionsOutputter.TweakERERestorationDecision(new Title.LandedTitles(), new ModFilesystem(ck3Root, Array.Empty<Mod>()), outputModPath);

			Assert.False(File.Exists(Path.Combine(outputModPath, RelativeDecisionPath)));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task UsesCk3ModFileWhenOutputCopyDoesNotExist() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			Directory.CreateDirectory(Path.Combine(outputModPath, "common", "decisions", "dlc_decisions"));
			var ck3Root = Path.Combine(tempRoot, "ck3");
			WriteDecisionFile(ck3Root, DefaultDecisionFileText("from_ck3_source"));

			await DecisionsOutputter.TweakERERestorationDecision(CreateTitlesWithByzantium(), new ModFilesystem(ck3Root, Array.Empty<Mod>()), outputModPath);

			var actualText = await ReadOutputDecisionFile(outputModPath);
			Assert.Contains("from_ck3_source = yes", actualText);
			Assert.Contains("exists = title:e_byzantium.previous_holder", actualText);
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task PrefersExistingOutputCopyOverCk3Source() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			WriteDecisionFile(outputModPath, DefaultDecisionFileText("from_output_copy"));
			var ck3Root = Path.Combine(tempRoot, "ck3");
			WriteDecisionFile(ck3Root, DefaultDecisionFileText("from_ck3_source"));

			await DecisionsOutputter.TweakERERestorationDecision(CreateTitlesWithByzantium(), new ModFilesystem(ck3Root, Array.Empty<Mod>()), outputModPath);

			var actualText = await ReadOutputDecisionFile(outputModPath);
			Assert.Contains("from_output_copy = yes", actualText);
			Assert.DoesNotContain("from_ck3_source = yes", actualText);
			Assert.Contains("exists = title:e_byzantium.previous_holder", actualText);
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task ReturnsWhenDecisionFileCannotBeFound() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			Directory.CreateDirectory(Path.Combine(outputModPath, "common", "decisions", "dlc_decisions"));
			var ck3Root = Path.Combine(tempRoot, "ck3");
			Directory.CreateDirectory(ck3Root);

			await DecisionsOutputter.TweakERERestorationDecision(CreateTitlesWithByzantium(), new ModFilesystem(ck3Root, Array.Empty<Mod>()), outputModPath);

			Assert.False(File.Exists(Path.Combine(outputModPath, RelativeDecisionPath)));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task ReturnsWithoutChangingFileWhenDecisionNodeIsMissing() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			var originalText =
				"""
				another_decision = {
					is_shown = {
						always = yes
					}
				}
				""";
			WriteDecisionFile(outputModPath, originalText);

			await DecisionsOutputter.TweakERERestorationDecision(CreateTitlesWithByzantium(), new ModFilesystem(Path.Combine(tempRoot, "ck3"), Array.Empty<Mod>()), outputModPath);

			Assert.Equal(TextTestUtils.NormalizeNewlines(originalText), await ReadOutputDecisionFile(outputModPath));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task ReturnsWithoutChangingFileWhenIsShownNodeIsMissing() {
		var tempRoot = CreateTempDir();
		try {
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			var originalText =
				"""
				recreate_byzantine_empire_decision = {
					desc = test_desc
				}
				""";
			WriteDecisionFile(outputModPath, originalText);

			await DecisionsOutputter.TweakERERestorationDecision(CreateTitlesWithByzantium(), new ModFilesystem(Path.Combine(tempRoot, "ck3"), Array.Empty<Mod>()), outputModPath);

			Assert.Equal(TextTestUtils.NormalizeNewlines(originalText), await ReadOutputDecisionFile(outputModPath));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	private static Title.LandedTitles CreateTitlesWithByzantium() {
		var titles = new Title.LandedTitles();
		titles.Add("e_byzantium");
		return titles;
	}

	private static string DefaultDecisionFileText(string markerKey = "marker") =>
		$$"""
		recreate_byzantine_empire_decision = {
			is_shown = {
				{{markerKey}} = yes
			}
		}
		""";

	private static async Task<string> ReadOutputDecisionFile(string outputModPath) {
		var text = await File.ReadAllTextAsync(Path.Combine(outputModPath, RelativeDecisionPath), Encoding.UTF8, TestContext.Current.CancellationToken);
		return TextTestUtils.NormalizeNewlines(text);
	}

	private static void WriteDecisionFile(string rootPath, string text) {
		var filePath = Path.Combine(rootPath, RelativeDecisionPath);
		Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
		File.WriteAllText(filePath, text, Encoding.UTF8);
	}

	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "DecisionsOutputter", Guid.NewGuid().ToString("N"));
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