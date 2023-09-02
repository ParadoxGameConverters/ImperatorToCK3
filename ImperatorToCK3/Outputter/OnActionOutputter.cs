using commonItems.Collections;
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
			"senate_on_actions.txt",
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
}