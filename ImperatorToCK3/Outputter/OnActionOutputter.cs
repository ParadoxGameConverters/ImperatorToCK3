using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CK3.Cleanup;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter; 

public static class OnActionOutputter {
	public static async Task OutputEverything(Configuration config, ModFilesystem ck3ModFS, string outputModPath){
		await OutputCustomGameStartOnAction(config);
		if (config.FallenEagleEnabled) {
			await DisableUnneededFallenEagleOnActionFiles(outputModPath);
		}
		Logger.IncrementProgress();
	}

	public static async Task OutputCustomGameStartOnAction(Configuration config) {
		Logger.Info("Writing game start on-action...");

		var sb = new StringBuilder();
		
		const string customOnGameStartOnAction = "irtock3_on_game_start_after_lobby";
		
		sb.AppendLine("on_game_start_after_lobby = {");
		sb.AppendLine($"\ton_actions = {{ {customOnGameStartOnAction } }}");
		sb.AppendLine("}");
		
		sb.AppendLine($"{customOnGameStartOnAction} = {{");
		sb.AppendLine("\teffect = {");
		
		if (config.LegionConversion == LegionConversion.MenAtArms) {
			sb.AppendLine("""
			                            	# IRToCK3: add MAA regiments
			                            	random_player = {
			                            		trigger_event = irtock3_hidden_events.0001
			                            	}
			                            """);
		}

		if (config.LegionConversion == LegionConversion.MenAtArms) {
			sb.AppendLine("\t\tset_global_variable = IRToCK3_create_maa_flag");
        }

		if (config.FallenEagleEnabled) {
			// As of the "Last of the Romans" update, TFE only disables Nicene for start dates >= 476.9.4.
			// But for the converter it's important that Nicene is disabled for all start dates >= 451.8.25.
			sb.AppendLine("""
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
		
		sb.AppendLine("\t}");
		sb.AppendLine("}");
		
		var filePath = $"output/{config.OutputModName}/common/on_action/IRToCK3_game_start.txt";
		await using var writer = new StreamWriter(filePath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
		await writer.WriteAsync(sb.ToString());
	}

	private static async Task DisableUnneededFallenEagleOnActionFiles(string outputModPath) {
		Logger.Info("Disabling unneeded Fallen Eagle on-actions...");
		var onActionsToDisable = new OrderedSet<string> {
			"sevenhouses_on_actions.txt",
			"senate_tasks_on_actions.txt",
		};
		foreach (var filename in onActionsToDisable) {
			var filePath = $"{outputModPath}/common/on_action/{filename}";
			await using var writer = new StreamWriter(filePath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
			await writer.WriteLineAsync("# disabled by IRToCK3");
		}
	}
}
