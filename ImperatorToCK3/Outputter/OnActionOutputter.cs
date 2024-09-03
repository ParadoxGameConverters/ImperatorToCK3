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
		} else { // vanilla
			await RemoveUnneededPartsOfVanillaOnActions(ck3ModFS, outputModPath);
		}
		Logger.IncrementProgress();
	}

	private static async Task RemoveUnneededPartsOfVanillaOnActions(ModFilesystem ck3ModFS, string outputModPath) {
		Logger.Info("Removing unneeded parts of vanilla on-actions...");
		var inputPath = ck3ModFS.GetActualFileLocation("common/on_action/game_start.txt");
		if (!File.Exists(inputPath)) {
			Logger.Debug("game_start.txt not found.");
			return;
		}
		var fileContent = await File.ReadAllTextAsync(inputPath);

		// List of blocks to remove as of 2024-09-03.
		string[] partsToRemove = [
			// events
			"\t\tfp1_scandinavian_adventurers.0011\t# FP1 - Corral famous Norse adventurers that haven't done much yet.\n",
			"\t\tfp1_scandinavian_adventurers.0021\t# FP1 - Mark game-start prioritised adventurers.\n",
			"\t\teasteregg_event.0001\t\t\t\t# Charna and Jakub duel.\n",
			"\t\tgame_rule.1011\t#Hungarian Migration management.\n",
			"""
					### 867 - RADHANITES IN KHAZARIA ###
					character:74025 = {
						if = {
							limit = {
								is_alive = yes
								is_landed = yes
							}
						}
						trigger_event = bookmark.0200
					}
			""",
			"""
					### 867 - WRATH OF THE NORTHMEN ###
					#Æthelred dying (probably)
					character:33358 = {
						if = {
							limit = {
								is_alive = yes
								is_landed = yes
							}
							trigger_event = {
								id = bookmark.0001
								days = { 365 730 }
							}
						}
					}
			""",
			"""
					#Alfred the Great becoming the Great
					character:7627 = {
						if = {
							limit = {
								is_alive = yes
								is_landed = yes
							}
							trigger_event = {
								id = bookmark.0002
								days = 1800 #~5 years
							}
						}
					}
			""",
			"""
					### 867 - THE GREAT ADVENTURERS ###
					character:251187 = {
						if = {
							limit = {
								is_alive = yes
								is_landed = yes
								AND = {
									character:251180 = { is_ai = yes }
									character:251181 = {
										is_ai = yes
										is_alive = yes
									}
								}
							}
							trigger_event = {
								id = bookmark.0101
								days = { 21 35 }
							}
						}
					}
			""",
			// setup
			"""
					### 1066 - LOUIS THE GERMAN ###
					if = {
						limit = {
							exists = character:90107
							current_date >= 1066.1.1
						}
						character:90107 = { give_nickname = nick_the_german_post_mortem }
					}
			""",
			"""
					# UNITY CONFIG
					## 867.
					if = {
						limit = { game_start_date = 867.1.1 }
						# Twiddle some starting unities.
						## The Abassids are in the middle of a self-killing frenzy, so we lower theirs substantially.
						house:house_abbasid ?= {
							add_unity_value = {
								value = -100
								# This is from historical circumstances, so we just do use the house head.
								character = house_head
								desc = clan_unity_historical_circumstances.desc
							}
						}
						## The Samanids are juuuuust about to get started on killing each other over who gets to lead Transoxiana.
						house:house_samanid ?= {
							add_unity_value = {
								value = -40
								# This is from historical circumstances, so we just do use the house head.
								character = house_head
								desc = clan_unity_historical_circumstances.desc
							}
						}
						## The Afrighids (both of them) are having fairly few arguments because only one of them can speak and it's very easy to manage relations with a baby.
						dynasty:1042112.dynast.house ?= {
							add_unity_value = {
								value = 50
								# This is from historical circumstances, so we just do use the house head.
								character = house_head
								desc = clan_unity_historical_circumstances.desc
							}
						}
						## The Tahirids are scattered but actually get along quite well and support each other politically (mostly).
						dynasty:811.dynast.house ?= {
							add_unity_value = {
								value = 100
								# This is from historical circumstances, so we just do use the house head.
								character = house_head
								desc = clan_unity_historical_circumstances.desc
							}
						}
						## The Umayyads are having something of a renaissance.
						dynasty:597.dynast.house ?= {
							add_unity_value = {
								value = 100
								# This is from historical circumstances, so we just do use the house head.
								character = house_head
								desc = clan_unity_historical_circumstances.desc
							}
						}
					}
					# LEGITIMACY CONFIG
					## 867.
					if = {
						limit = { game_start_date = 867.1.1 }
						## Basileus Basileios was actually elected, so he's technically legitimate, but starts at level 2. With this he should be level 3.
						character:1700 = {
							add_legitimacy = major_legitimacy_gain
						}
					}
			""",
			"""
					if = { # Special historical events for Matilda!
						limit = {
							character:7757 ?= { is_alive = yes }
						}
						character:7757 ?= {
							trigger_event = bookmark.1066 # Matildas marriage to her step-brother, with plausible historical options!
							trigger_event = { # Matildas suspected witchcraft, the player decides if its true or not!
								id = bookmark.1067
								years = { 1 5 }
							}
						}
					}
			""",
			"""
					if = { # Special historical events for Vratislav!
						limit = {
							character:522 ?= { is_alive = yes }
						}
						character:522 ?= {
							trigger_event = { # Vratislav and the Slavic Marches, he didn't historically get them (one briefly, but eh). The player chooses to appease the emperor or go after the coveted lands themselves!
								id = bookmark.1068
								days = { 35 120 }
							}
							trigger_event = { # Jaromir, Vratislav's brother, was a pain - this event is an opportunity for the player to handle the rivalry
								id = bookmark.1069
								days = { 1 29 }
							}
						}
					}
			""",
			"""
					if = { # Special historical events for Robert the Fox!
						limit = {
							character:1128 ?= { is_alive = yes }
						}
						character:1128 ?= {
							trigger_event = { # A Norman Sicily - Robert historically conquered quite a bit here, the player can choose how far they want to go and the risk they want to take. The more risk, the more event troops/claims.
								id = bookmark.1070
								days = { 35 120 }
							}
							trigger_event = { # The Pretender Monk - Raiktor is a historical character, a monk wo pretended to be a deposed Byzantine emperor which Robert used to beat up Byzantium. Here you can follow historical conquests (taking a bit of the coast) or go full on 'install him as emperor for real'-mode!
								id = bookmark.1071
								years = { 1 7 }
							}
						}
					}
			""",
			"""
					if = { # Special historical events for Emir Yahya!
						limit = {
							character:3924 ?= { is_alive = yes }
						}
						character:3924 ?= {
							trigger_event = { # Conquering Cordoba - Gain an opportunity to conquer Cordoba while gaining one of two buffs; one intrigue-focused, and one military. Historically he was poisoned after having conquered the city... but that's no fun for the player!
								id = bookmark.1072
								days = { 10 35 }
							}
						}
					}
			""",
			"""
					# Pre-defined historic regencies setup.
					## NOTE: we do these first to avoid feed messages getting weird due to regents being replaced immediately after getting their position.
					## 867.
					### None. Yet.
					## 1066.
					if = {
						limit = { game_start_date = 1066.9.15 }
						# Designate some regents.
						## King Philippe of France & Duke Boudewijn of Flanders (friend of his dad's)
						character:214 = {
							designate_diarch = character:364
							# Baldwin of Flanders also promised the prior king he'd take care of Philippe, so we add that starting loyalty hook.
							add_hook = {
								type = predecessor_loyalty_hook
								target = character:364
								years = historic_regent_loyal_after_death_hook_duration_years_char_214_value
							}
						}
						### Plus remember who the promise was made to.
						character:364 = {
							add_opinion = {
								target = character:214
								modifier = promise_to_predecessor
								opinion = 50
							}
							set_variable = {
								name = promise_to_predecessor
								value = character:208
								years = historic_regent_loyal_after_death_hook_duration_years_char_214_value
							}
						}
						## Count Bouchard of Vendome & Guy de Bachaumont (his uncle)
						character:40905 = { designate_diarch = character:40376 }
						## Caliph al-Mustansir & Rasad (his mother)
						character:3096 = { designate_diarch = character:additional_fatimids_1 }
						## Count Ermengol of Urgell & Infanta Sancha of Aragon (his stepmother)
						character:110550 = { designate_diarch = character:110514 }
						## Duke Dirk of Holland & Count Robrecht of Zeeland (his stepfather)
						character:106520 = { designate_diarch = character:368 }
						## Duke Sven of Ostergotland & Kol Sverker (his father)
						character:100530 = { designate_diarch = character:100529 }
						## King Salamon of Hungary & Queen Mother Anastasia (his mother, in the absence of any better recorded options, and to keep other hostile relatives out of the job)
						character:476 = { designate_diarch = character:637 }
						## Prince Demetre of Georgia & Alda Oseti (his mother)
						character:9957 = { designate_diarch = character:9956 }
						## Sultan al-Muazzam Alp Arslan and Hassan "the Order of the Realm".
						character:3040 = {
							designate_diarch = character:3050
							# This is a vizierate as well, so start the diarchy manually.
							start_diarchy = vizierate
							# Tell Alp that he appointed Hassan so he remembers not to dismiss him.
							set_variable = {
								name = my_vizier
								value = character:3050
							}
						}
						# Plus remove all the generated opinions.
						## King Philippe of France & Duke Boudewijn of Flanders
						remove_generated_diarch_consequences_effect = {
							NEW_DIARCH = character:364
							LIEGE = character:214
						}
						## Count Bouchard of Vendome & Guy de Bachaumont
						remove_generated_diarch_consequences_effect = {
							NEW_DIARCH = character:40376
							LIEGE = character:40905
						}
						## Caliph al-Mustansir & Rasad
						remove_generated_diarch_consequences_effect = {
							NEW_DIARCH = character:additional_fatimids_1
							LIEGE = character:3096
						}
						## Count Ermengol of Urgell & Infanta Sancha of Aragon
						remove_generated_diarch_consequences_effect = {
							NEW_DIARCH = character:110514
							LIEGE = character:110550
						}
						## Duke Dirk of Holland & Count Robrecht of Zeeland
						remove_generated_diarch_consequences_effect = {
							NEW_DIARCH = character:368
							LIEGE = character:106520
						}
						## Duke Sven of Ostergotland & Kol Sverker
						remove_generated_diarch_consequences_effect = {
							NEW_DIARCH = character:100529
							LIEGE = character:100530
						}
						## King Salamon of Hungary & Queen Mother Anastasia
						remove_generated_diarch_consequences_effect = {
							NEW_DIARCH = character:637
							LIEGE = character:476
						}
						## Prince Demetre of Georgia & Alda Oseti
						remove_generated_diarch_consequences_effect = {
							NEW_DIARCH = character:9956
							LIEGE = character:9957
						}
						## Sultan al-Muazzam Alp Arslan and Hassan "the Order of the Realm".
						remove_generated_diarch_consequences_effect = {
							NEW_DIARCH = character:3050
							LIEGE = character:3040
						}
					}
			""",
			"""
					## Fatimid Caliphate - basically stuck in the back-end of an entrenched regencies from game start.
					if = {
						limit = { exists = character:3096 }
						character:3096 = { trigger_event = diarchy.0012 }
					}
			""",
			// achievements
			"""
							##11 Bod Chen Po
							if = {
								limit = {
									this.dynasty = dynasty:105800
								}
								add_achievement_global_variable_effect = {
									VARIABLE = started_bod_chen_po_achievement
									VALUE = yes
								}
							}
			""",
			"""
							##14 Brave and Bold
							if = {
								limit = {
									game_start_date < 868.1.1
									this.dynasty = dynasty:699 #Piast
								}
								add_achievement_global_variable_effect = {
									VARIABLE = started_brave_and_bold_achievement
									VALUE = yes
								}
							}
			""",
			"""
							## 19. A.E.I.O.U & Me
							if = {
								limit = {
									# Etichonen, of whom the Hapsburgs are a cadet - we check dynasty rather than house so that an accidental cadet doesn't screw you.
									this.house ?= house:house_habsburg
								}
								add_achievement_global_variable_effect = {
									VARIABLE = started_a_e_i_o_u_and_me_achievement
									VALUE = yes
								}
							}
			""",
			"""
					### ACHIEVEMENT (FP3): The Ummayad Strikes Back
					every_player = {
						if = {
							limit = {
								dynasty = character:73683.dynasty
								location = { geographical_region = world_europe_west_iberia }
							}
							set_global_variable = fp3_the_umma_strikes_back_achievement_tracker # Is not removed (sad!)
						}
					}
			""",
		];

		foreach (var block in partsToRemove) {
			// The file uses LF line endings, so we need to make the search string use LF line endings as well.
			fileContent = fileContent.Replace(block.Replace("\r\n", "\n"), "");
		}

		var outputPath = $"{outputModPath}/common/on_action/game_start.txt";
		await using var output = FileHelper.OpenWriteWithRetries(outputPath);
		await output.WriteAsync(fileContent);
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
