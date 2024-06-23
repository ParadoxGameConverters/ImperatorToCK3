using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter; 

public static class OnActionOutputter {
	public static async Task OutputEverything(Configuration config, ModFilesystem ck3ModFS, string outputModPath){
		await OutputCustomGameStartOnAction(config);
		if (config.FallenEagleEnabled) {
			await DisableUnneededFallenEagleOnActions(outputModPath);
			await RemoveStruggleStartFromFallenEagleOnActions(ck3ModFS, outputModPath);
		}
		Logger.IncrementProgress();
	}
	
	public static async Task OutputCustomGameStartOnAction(Configuration config) {
		Logger.Info("Writing game start on-action...");
		var filePath = $"output/{config.OutputModName}/common/on_action/IRToCK3_game_start.txt";
		await using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
		
		const string customOnGameStartOnAction = "irtock3_on_game_start_after_lobby";
		
		await writer.WriteLineAsync("on_game_start_after_lobby = {");
		await writer.WriteLineAsync($"\ton_actions = {{ {customOnGameStartOnAction } }}");
		await writer.WriteLineAsync("}");
		
		await writer.WriteLineAsync($"{customOnGameStartOnAction} = {{");
		await writer.WriteLineAsync("\teffect = {");
		
		if (config.LegionConversion == LegionConversion.MenAtArms) {
			await writer.WriteLineAsync("""
			                            	# IRToCK3: add MAA regiments
			                            	random_player = {
			                            		trigger_event = irtock3_hidden_events.0001
			                            	}
			                            """);
		}

		if (config.LegionConversion == LegionConversion.MenAtArms) {
			await writer.WriteLineAsync("\t\tset_global_variable = IRToCK3_create_maa_flag");
        }

		if (config.FallenEagleEnabled) {
			// As of the "Last of the Romans" update, TFE only disables Nicene for start dates >= 476.9.4.
			// But for the converter it's important that Nicene is disabled for all start dates >= 451.8.25.
			await writer.WriteLineAsync("""
			                            	# IRToCK3: disable Nicene after the Council of Chalcedon.
			                            	if = {
			                            		limit = {
			                            			game_start_date >= 451.8.25
			                            		}
			                            		faith:armenian_apostolic = {
			                            			remove_doctrine = unavailable_doctrine
			                            		}
			                            		faith:nestorian = {
			                            			remove_doctrine = unavailable_doctrine
			                            		}
			                            		faith:coptic = {
			                            			remove_doctrine = unavailable_doctrine
			                            		}
			                            		faith:syriac = {
			                            			remove_doctrine = unavailable_doctrine
			                            		}
			                            		faith:chalcedonian = {
			                            			remove_doctrine = unavailable_doctrine
			                            		}
			                            		faith:nicene = {
			                            			add_doctrine = unavailable_doctrine
			                            		}
			                            	}
			                            """);
		}
		
		await writer.WriteLineAsync("\t}");
		await writer.WriteLineAsync("}");
	}

	private static async Task DisableUnneededFallenEagleOnActions(string outputModPath) {
		Logger.Info("Disabling unneeded Fallen Eagle on-actions...");
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
			var filePath = $"{outputModPath}/common/on_action/{filename}";
			await using var writer = new StreamWriter(filePath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
			await writer.WriteLineAsync("# disabled by IRToCK3");
		}
	}

	private static async Task RemoveStruggleStartFromFallenEagleOnActions(ModFilesystem ck3ModFS, string outputModPath) {
		Logger.Info("Removing struggle start from Fallen Eagle on-actions...");
		var inputPath = ck3ModFS.GetActualFileLocation("common/on_action/TFE_game_start.txt");
		if (!File.Exists(inputPath)) {
			Logger.Debug("TFE_game_start.txt not found.");
			return;
		}
		var fileContent = await File.ReadAllTextAsync(inputPath);

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
							game_start_date >= 476.9.4
						}
						start_struggle = { struggle_type = italian_struggle start_phase = struggle_TFE_italian_phase_turmoil }
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

		var outputPath = $"{outputModPath}/common/on_action/TFE_game_start.txt";
		await using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath);
		await output.WriteAsync(fileContent);
	}
}
