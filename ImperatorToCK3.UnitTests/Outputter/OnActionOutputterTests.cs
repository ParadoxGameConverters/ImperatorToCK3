using commonItems.Mods;
using ImperatorToCK3;
using ImperatorToCK3.Outputter;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class OnActionOutputterTests {
	private const string OutputModName = "outputModOnAction";

	private static Configuration CreateConfiguration(bool menAtArms, bool fallenEagleEnabled) {
		var config = new Configuration {
			LegionConversion = menAtArms ? LegionConversion.MenAtArms : LegionConversion.No,
			OutputModName = OutputModName
		};
		if (fallenEagleEnabled) {
			config.DetectSpecificCK3Mods([new Mod("The Fallen Eagle", "", dependencies: [])]);
		}
		return config;
	}

	private static async Task<string> OutputAndRead(bool menAtArms, bool fallenEagleEnabled) {
		var config = CreateConfiguration(menAtArms, fallenEagleEnabled);
		Directory.CreateDirectory(Path.Combine("output", OutputModName, "common", "on_action"));
		await OnActionOutputter.OutputCustomGameStartOnAction(config);
		var outputPath = Path.Combine("output", OutputModName, "common", "on_action", "IRToCK3_game_start.txt");
		return await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
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
	public async Task MenAtArmsWithoutTfeOutputsMenAtArmsEffectsOnly() {
		try {
			var output = await OutputAndRead(menAtArms: true, fallenEagleEnabled: false);

			Assert.Contains("on_game_start_after_lobby", output, StringComparison.Ordinal);
			Assert.Contains("irtock3_on_game_start_after_lobby", output, StringComparison.Ordinal);
			Assert.Contains("trigger_event = irtock3_hidden_events.0001", output, StringComparison.Ordinal);
			Assert.Contains("set_global_variable = IRToCK3_create_maa_flag", output, StringComparison.Ordinal);

			Assert.DoesNotContain("451.8.25", output, StringComparison.Ordinal);
			Assert.DoesNotContain("sevenhouses_dead", output, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(Path.Combine("output", OutputModName));
		}
	}

	[Fact]
	public async Task MenAtArmsWithTfeOutputsMenAtArmsAndFallenEagleEffects() {
		try {
			var output = await OutputAndRead(menAtArms: true, fallenEagleEnabled: true);

			Assert.Contains("trigger_event = irtock3_hidden_events.0001", output, StringComparison.Ordinal);
			Assert.Contains("set_global_variable = IRToCK3_create_maa_flag", output, StringComparison.Ordinal);

			Assert.Contains("game_start_date >= 451.8.25", output, StringComparison.Ordinal);
			Assert.Contains("add_doctrine = unavailable_doctrine", output, StringComparison.Ordinal);
			Assert.Contains("sevenhouses_dead", output, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(Path.Combine("output", OutputModName));
		}
	}

	[Fact]
	public async Task NoLegionsWithoutTfeOutputsBareOnAction() {
		try {
			var output = await OutputAndRead(menAtArms: false, fallenEagleEnabled: false);

			Assert.DoesNotContain("irtock3_hidden_events.0001", output, StringComparison.Ordinal);
			Assert.DoesNotContain("IRToCK3_create_maa_flag", output, StringComparison.Ordinal);
			Assert.DoesNotContain("451.8.25", output, StringComparison.Ordinal);
			Assert.DoesNotContain("sevenhouses_dead", output, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(Path.Combine("output", OutputModName));
		}
	}

	[Fact]
	public async Task NoLegionsWithTfeOutputsFallenEagleEffectsOnly() {
		try {
			var output = await OutputAndRead(menAtArms: false, fallenEagleEnabled: true);

			Assert.DoesNotContain("irtock3_hidden_events.0001", output, StringComparison.Ordinal);
			Assert.DoesNotContain("IRToCK3_create_maa_flag", output, StringComparison.Ordinal);

			Assert.Contains("game_start_date >= 451.8.25", output, StringComparison.Ordinal);
			Assert.Contains("sevenhouses_dead", output, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(Path.Combine("output", OutputModName));
		}
	}

	[Fact]
	public async Task OutputEverythingWritesGameStartOnAction() {
		try {
			var config = CreateConfiguration(menAtArms: false, fallenEagleEnabled: false);
			var ck3ModFS = new ModFilesystem(".", Array.Empty<Mod>());

			Directory.CreateDirectory(Path.Combine("output", OutputModName, "common", "on_action"));
			await OnActionOutputter.OutputEverything(config, ck3ModFS, Path.Combine("output", OutputModName));

			var outputPath = Path.Combine("output", OutputModName, "common", "on_action", "IRToCK3_game_start.txt");
			var output = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
			Assert.Contains("irtock3_on_game_start_after_lobby", output, StringComparison.Ordinal);
		} finally {
			TryDeleteDir(Path.Combine("output", OutputModName));
		}
	}
}
