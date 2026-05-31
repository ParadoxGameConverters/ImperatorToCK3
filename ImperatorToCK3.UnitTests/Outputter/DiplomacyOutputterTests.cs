using ImperatorToCK3.CK3.Titles;
using ImperatorToCK3.Outputter;
using ImperatorToCK3.UnitTests.TestHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class DiplomacyOutputterTests {
	[Fact]
	public async Task LeaguesWithFewerThanTwoMembersAreSkipped() {
		var outputModPath = CreateTempDir();
		try {
			EnsureOutputDirectoryExists(outputModPath);
			var titles = new Title.LandedTitles();
			var oneMemberLeague = new List<Title> {
				titles.Add("k_single")
			};

			await DiplomacyOutputter.OutputLeagues(outputModPath, [[], oneMemberLeague]);

			var actualText = await ReadOutput(outputModPath);
			var expectedText = TextTestUtils.NormalizeNewlines(
				"""
				on_game_start_after_lobby = {
					on_actions = {
						irtock3_confederation_setup
					}
				}

				irtock3_confederation_setup = {
					effect = {
					}
				}

				"""
			);

			Assert.Equal(expectedText, actualText);
		} finally {
			TryDeleteDir(outputModPath);
		}
	}

	[Fact]
	public async Task TwoMemberLeagueOutputsBaseConfederationSetup() {
		var outputModPath = CreateTempDir();
		try {
			EnsureOutputDirectoryExists(outputModPath);
			var titles = new Title.LandedTitles();
			var league = new List<Title> {
				titles.Add("k_first"),
				titles.Add("k_second")
			};

			await DiplomacyOutputter.OutputLeagues(outputModPath, [league]);

			var actualText = await ReadOutput(outputModPath);
			actualText.ShouldContainLine("## Beginning of new confederation/league setup");
			actualText.ShouldContainLine("title:k_first.holder = {");
			actualText.ShouldContainLine("title:k_second.holder = {");
			actualText.ShouldContainLine("add_to_list = irtock3_confederation_members");
			Assert.Contains("irtock3_confederation_setup_effect = yes", actualText);
			Assert.Contains("## End of this confederation/league setup", actualText);
			Assert.DoesNotContain("title:k_third.holder", actualText);
		} finally {
			TryDeleteDir(outputModPath);
		}
	}

	[Fact]
	public async Task AdditionalLeagueMembersGetTheirOwnBlocks() {
		var outputModPath = CreateTempDir();
		try {
			EnsureOutputDirectoryExists(outputModPath);
			var titles = new Title.LandedTitles();
			var league = new List<Title> {
				titles.Add("k_first"),
				titles.Add("k_second"),
				titles.Add("k_third"),
				titles.Add("k_fourth")
			};

			await DiplomacyOutputter.OutputLeagues(outputModPath, [league]);

			var actualText = await ReadOutput(outputModPath);
			Assert.Equal(4, CountOccurrences(actualText, "add_to_list = irtock3_confederation_members"));
			actualText.ShouldContainLine("title:k_third.holder = {");
			actualText.ShouldContainLine("title:k_fourth.holder = {");
		} finally {
			TryDeleteDir(outputModPath);
		}
	}

	[Fact]
	public async Task MultipleValidLeaguesProduceMultipleConfederationSections() {
		var outputModPath = CreateTempDir();
		try {
			EnsureOutputDirectoryExists(outputModPath);
			var titles = new Title.LandedTitles();
			var firstLeague = new List<Title> {
				titles.Add("k_alpha"),
				titles.Add("k_beta")
			};
			var secondLeague = new List<Title> {
				titles.Add("k_gamma"),
				titles.Add("k_delta"),
				titles.Add("k_epsilon")
			};

			await DiplomacyOutputter.OutputLeagues(outputModPath, [firstLeague, secondLeague]);

			var actualText = await ReadOutput(outputModPath);
			Assert.Equal(2, CountOccurrences(actualText, "## Beginning of new confederation/league setup"));
			Assert.Equal(2, CountOccurrences(actualText, "irtock3_confederation_setup_effect = yes"));
			actualText.ShouldContainLine("title:k_alpha.holder = {");
			actualText.ShouldContainLine("title:k_epsilon.holder = {");
		} finally {
			TryDeleteDir(outputModPath);
		}
	}

	private static async Task<string> ReadOutput(string outputModPath) {
		var outputPath = Path.Combine(outputModPath, "common/on_action/irtock3_confederations.txt");
		var text = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		return TextTestUtils.NormalizeNewlines(text);
	}

	private static int CountOccurrences(string text, string value) {
		var count = 0;
		var index = 0;
		while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0) {
			count++;
			index += value.Length;
		}
		return count;
	}

	private static void EnsureOutputDirectoryExists(string outputModPath) {
		Directory.CreateDirectory(Path.Combine(outputModPath, "common", "on_action"));
	}

	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "DiplomacyOutputter", Guid.NewGuid().ToString("N"));
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

internal static class DiplomacyOutputterTestsExtensions {
	public static void ShouldContainLine(this string text, string line) {
		Assert.Contains(TextTestUtils.NormalizeNewlines(line), text);
	}
}