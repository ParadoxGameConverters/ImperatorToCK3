using System.IO;

namespace ImperatorToCK3.Outputter; 

public static class OnActionOutputter {
	public static void OutputCustomGameStartOnAction(Configuration config) {
		var filePath = $"output/{config.OutputModName}/common/on_action/IRToCK3_game_start.txt";
		using var writer = new StreamWriter(filePath);
		
		const string customOnGameStartOnAction = "irtock3_on_game_start";
		
		writer.WriteLine("on_game_start = {");
		writer.WriteLine($"\ton_actions = {{ {customOnGameStartOnAction } }}");
		writer.WriteLine("}");
		
		writer.WriteLine($"{customOnGameStartOnAction} = {{");
		writer.WriteLine("\teffect = {");
		
		writer.WriteLine("""
			# IRToCK3: Show welcoming event
			every_player = { # Welcoming event
				trigger_event = {
					id = welcome.1
					days = 0
				}
			}
		""");

		if (config.LegionConversion == LegionConversion.MenAtArms) {
			writer.WriteLine("""
				# IRToCK3: add MAA regiments
				random_player = {
					trigger_event = {
						id = irtock3_hidden_events.0001
						days = 0
					}
				}
			""");
		}
		
		writer.WriteLine("""
			# IRToCK3: Detect no culture in setup
			every_living_character = {
				limit = {
					has_culture = culture:aaa_noculture
				}
				every_player = {
					trigger_event = {
						id = welcome.2
						days = 0
					}
				}
			}
			every_county = {
				limit = {
					culture = culture:aaa_noculture
				}
				every_player = {
					trigger_event = {
						id = welcome.2
						days = 0
					}
				}
			}
		""");
		
		writer.WriteLine("\t}");
		writer.WriteLine("}");
	}
}