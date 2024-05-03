using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Text;

namespace ImperatorToCK3.Outputter; 

public static class OnActionOutputter {
	public static void OutputCustomGameStartOnAction(Configuration config) {
		var filePath = $"output/{config.OutputModName}/common/on_action/IRToCK3_game_start.txt";
		using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
		
		const string customOnGameStartOnAction = "irtock3_on_game_start_after_lobby";
		
		writer.WriteLine("on_game_start_after_lobby = {");
		writer.WriteLine($"\ton_actions = {{ {customOnGameStartOnAction } }}");
		writer.WriteLine("}");
		
		writer.WriteLine($"{customOnGameStartOnAction} = {{");
		writer.WriteLine("\teffect = {");
		
		if (config.LegionConversion == LegionConversion.MenAtArms) {
			writer.WriteLine("""
				# IRToCK3: add MAA regiments
				random_player = {
					trigger_event = irtock3_hidden_events.0001
				}
			""");
		}

		if (config.LegionConversion == LegionConversion.MenAtArms) {
			writer.WriteLine("\t\tset_global_variable = IRToCK3_create_maa_flag");
        }
		
		writer.WriteLine("\t}");
		writer.WriteLine("}");
	}
	
	public static void DisableUnneededFallenEagleOnActions(string outputModName) {
		var onActionsToDisable = new OrderedSet<string> {
			"sea_minority_game_start.txt", 
			"sevenhouses_on_actions.txt", 
			"government_change_on_actions.txt",
			"tribs_on_action.txt",
			"AI_war_on_actions.txt",
			"senate_tasks_on_actions.txt",
			"new_electives_on_action.txt",
			"tfe_struggle_on_actions.txt",
			"roman_vicar_positions_on_actions.txt",
		};
		foreach (var filename in onActionsToDisable) {
			var filePath = $"output/{outputModName}/common/on_action/{filename}";
			using var writer = new StreamWriter(filePath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
			writer.WriteLine("# disabled by IRToCK3");
		}
	}

	public static void RemoveStruggleStartFromFallenEagleOnActions(ModFilesystem ck3ModFS, string outputModName) {
		var inputPath = ck3ModFS.GetActualFileLocation("common/on_action/TFE_game_start.txt");
		if (!File.Exists(inputPath)) {
			Logger.Debug("TFE_game_start.txt not found.");
			return;
		}
		var fileContent = File.ReadAllText(inputPath);

		// List of blocks to remove as of 2024-01-07.
		string[] struggleStartBlocksToRemove = [
			"""
					if = {
						limit = {
							AND = {
								game_start_date >= 476.9.4
								game_start_date <= 768.1.1
							}
						}
						start_struggle = { struggle_type = britannia_struggle start_phase = struggle_britannia_phase_migration }
					}
			""",
			"""
					if = {
						limit = {
							AND = {
								game_start_date <= 651.1.1 # Death of Yazdegerd III
							}
						}
						start_struggle = { struggle_type = roman_persian_struggle start_phase = struggle_TFE_roman_persian_phase_contention }
					}
					start_struggle = { struggle_type = eastern_iranian_struggle start_phase = struggle_TFE_eastern_iranian_phase_expansion }
					start_struggle = { struggle_type = north_indian_struggle start_phase = struggle_TFE_north_indian_phase_invasion }
			""",
		];

		foreach (var block in struggleStartBlocksToRemove) {
			fileContent = fileContent.Replace(block, "");
		}

		var outputPath = $"output/{outputModName}/common/on_action/TFE_game_start.txt";
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath);
		output.Write(fileContent);
	}
}
