using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Armies;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Outputter;

public static class MenAtArmsOutputter {
	private static void OutputHiddenEvent(string outputModName, IEnumerable<Character> charactersWithMaa) {
		var outputPath = Path.Combine("output", outputModName, "events", "irtock3_hidden_events.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);

		output.WriteLine("namespace = irtock3_hidden_events");
		output.WriteLine();
		output.WriteLine("irtock3_hidden_events.0001 = {");
		output.WriteLine("\ttype = character_event");
		output.WriteLine("\thidden = yes");

		output.WriteLine("\timmediate = {");
		foreach (var character in charactersWithMaa) {
			output.WriteLine(
				"\t\tset_variable = { " +
				$"name=IRToCK3_character_{character.Id} " +
				$"value=character:{character.Id} " +
				"}"
			);
		}
		output.WriteLine("\t}");

		output.WriteLine("}");
	}

	private static void OutputMenAtArmsTypes(string outputModName, IdObjectCollection<string, MenAtArmsType> menAtArmsTypes) {
		Logger.Info("Writing men-at-arms types...");

		var outputPath = Path.Combine("output", outputModName, "common/men_at_arms_types/IRToCK3_generated_types.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);

		foreach (var type in menAtArmsTypes.Where(t=>t.ToBeOutputted)) {
			output.WriteLine($"{type.Id}={PDXSerializer.Serialize(type)}");
		}
	}

	private static void OutputGuiContainer(string outputModName, ModFilesystem modFS, List<Character> charactersWithMaa) {
		const string relativeHudTopGuiPath = "gui/hud_top.gui";
		var hudTopGuiPath = modFS.GetActualFileLocation(relativeHudTopGuiPath);
		if (hudTopGuiPath is null) {
			Logger.Warn($"{relativeHudTopGuiPath} not found, can't write MAA creation commands!");
			return;
		}

		string guiText = File.ReadAllText(hudTopGuiPath);

		var outputPath = Path.Combine("output", outputModName, relativeHudTopGuiPath);
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);

		output.WriteLine(guiText.TrimEnd().TrimEnd('}'));
		output.WriteLine("\tcontainer={");
		output.WriteLine("\t\tname=\"IRToCK3_maa_toogle\"");
		output.WriteLine("\t\tdatacontext=\"[GetScriptedGui('IRToCK3_create_maa')]\"");
		output.WriteLine("\t\tvisible=\"[ScriptedGui.IsShown( GuiScope.SetRoot( GetPlayer.MakeScope ).End )]\"");

		const float duration = 0.01f;
		int state = 0;
		output.WriteLine(
			"\t\tstate = { " +
			"name=_show " +
			$"next=state{state} " +
			"on_start=\"[ExecuteConsoleCommand('effect debug_log=LOG_SPAWNING_MAA')]\" " +
			$"duration={duration} }}"
		);
		foreach (var character in charactersWithMaa) {
			foreach (var (maaType, stacks) in character.MenAtArmsStacksPerType) {
				for (int i = 0; i < stacks; ++i) {
					output.WriteLine(
						"\t\tstate = { " +
						$"name=state{state++} " +
						$"next=state{state} " +
						$"on_start=\"[ExecuteConsoleCommand(Concatenate('add_maa {maaType} ', Localize('IRToCK3_character_{character.Id}')))]\" " +
						$"duration={duration} }}");
				}
			}
		}

		output.WriteLine(
			"\t\tstate = { " +
			$"name=state{state} " +
			"on_start=\"[ExecuteConsoleCommand('effect remove_global_variable=IRToCK3_create_maa_flag')]\" " +
			$"duration={duration} " +
			"}");

		output.WriteLine("\t}");
		output.WriteLine("}");
	}

	public static void OutputMenAtArms(string outputModName, ModFilesystem modFS, CharacterCollection ck3Characters, IdObjectCollection<string, MenAtArmsType> menAtArmsTypes) {
		Logger.Info("Writing men-at-arms spawning script...");

		var charactersWithMaa = ck3Characters.Where(c => c.MenAtArmsStacksPerType.Any()).ToList();
		OutputHiddenEvent(outputModName, charactersWithMaa);
		OutputGuiContainer(outputModName, modFS, charactersWithMaa);
		OutputMenAtArmsTypes(outputModName, menAtArmsTypes);
	}
}