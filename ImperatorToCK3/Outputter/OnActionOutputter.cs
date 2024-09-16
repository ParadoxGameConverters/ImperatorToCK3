using commonItems;
using commonItems.Collections;
using commonItems.Mods;
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
			await DisableUnneededFallenEagleOnActions(outputModPath);
			await RemoveStruggleStartFromFallenEagleOnActions(ck3ModFS, outputModPath);
		} else if (!config.WhenTheWorldStoppedMakingSenseEnabled) { // vanilla
			await RemoveUnneededPartsOfVanillaOnActions(ck3ModFS, outputModPath);
		}
		Logger.IncrementProgress();
	}

	private static async Task RemoveUnneededPartsOfVanillaOnActions(ModFilesystem ck3ModFS, string outputModPath) {
		Logger.Info("Removing unneeded parts of vanilla on-actions...");

		// Load removable blocks from configurables.
		Dictionary<string, string[]> partsToRemovePerFile = [];
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, fileName) => {
			var blocksToRemove = new BlobList(reader).Blobs.Select(b => b.Trim()).ToArray();
			partsToRemovePerFile[fileName] = blocksToRemove;
		});
		parser.RegisterRegex(CommonRegexes.QuotedString, (reader, fileNameInQuotes) => {
			var blocksToRemove = new BlobList(reader).Blobs.Select(b => b.Trim()).ToArray();
			partsToRemovePerFile[fileNameInQuotes.RemQuotes()] = blocksToRemove;
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile("configurables/removable_on_action_blocks.txt");
		
		// Log count of blocks to remove for each file.
		foreach (var (file, partsToRemove) in partsToRemovePerFile) {
			Logger.Debug($"Loaded {partsToRemove.Length} blocks to remove from {file}.");
		}

		foreach (var (file, partsToRemove) in partsToRemovePerFile) {
			var relativePath = Path.Join("common/on_action", file);
			var inputPath = ck3ModFS.GetActualFileLocation(relativePath);
			if (!File.Exists(inputPath)) {
				Logger.Debug($"{relativePath} not found.");
				return;
			}

			string lineEndings = GetLineEndingsInFile(inputPath);
			
			var fileContent = await File.ReadAllTextAsync(inputPath);

			foreach (var block in partsToRemove) {
				// If the file uses other line endings than CRLF, we need to modify the search string.
				string searchString;
				if (lineEndings == "LF") {
					searchString = block.Replace("\r\n", "\n");
				} else if (lineEndings == "CR") {
					searchString = block.Replace("\r\n", "\r");
				} else {
					searchString = block;
				}
				
				// Log if the block is not found.
				if (!fileContent.Contains(searchString)) {
					Logger.Warn($"Block not found in file {relativePath}: {searchString}");
					continue;
				}
				
				fileContent = fileContent.Replace(searchString, "");
			}

			var outputPath = $"{outputModPath}/{relativePath}";
			await using var output = FileHelper.OpenWriteWithRetries(outputPath);
			await output.WriteAsync(fileContent);
		}
	}
	
	private static string GetLineEndingsInFile(string filePath) {
		using StreamReader sr = new StreamReader(filePath);
		bool returnSeen = false;
		while (sr.Peek() >= 0) {
			char c = (char)sr.Read();
			if (c == '\n') {
				return returnSeen ? "CRLF" : "LF";
			}
			else if (returnSeen) {
				return "CR";
			}

			returnSeen = c == '\r';
		}

		if (returnSeen) {
			return "CR";
		} else {
			return "LF";
		}
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
		await using var output = FileHelper.OpenWriteWithRetries(outputPath);
		await output.WriteAsync(fileContent);
	}
}
