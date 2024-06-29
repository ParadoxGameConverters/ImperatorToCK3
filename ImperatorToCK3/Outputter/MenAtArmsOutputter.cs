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
using System.Text;

namespace ImperatorToCK3.Outputter;

public static class MenAtArmsOutputter {
	private static void OutputHiddenEvent(string outputModName, IEnumerable<Character> charactersWithMaa) {
		var sb = new StringBuilder();
		
		sb.AppendLine("namespace = irtock3_hidden_events");
		sb.AppendLine();
		sb.AppendLine("irtock3_hidden_events.0001 = {");
		sb.AppendLine("\ttype = character_event");
		sb.AppendLine("\thidden = yes");

		sb.AppendLine("\timmediate = {");
		foreach (var character in charactersWithMaa) {
			sb.AppendLine(
				"\t\tset_variable = { " +
				$"name=IRToCK3_character_{character.Id} " +
				$"value=character:{character.Id} " +
				"}"
			);
		}
		sb.AppendLine("\t}");

		sb.AppendLine("}");
		
		var outputPath = Path.Combine("output", outputModName, "events", "irtock3_hidden_events.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);
		output.Write(sb.ToString());
	}

	private static void OutputMenAtArmsTypes(string outputModName, IdObjectCollection<string, MenAtArmsType> menAtArmsTypes) {
		Logger.Info("Writing men-at-arms types...");
		
		var sb = new StringBuilder();
		foreach (var type in menAtArmsTypes.Where(t=>t.ToBeOutputted)) {
			sb.AppendLine($"{type.Id}={PDXSerializer.Serialize(type)}");
		}

		var outputPath = Path.Combine("output", outputModName, "common/men_at_arms_types/IRToCK3_generated_types.txt");
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);
		output.Write(sb.ToString());
	}

	private static void OutputGuiContainer(string outputModName, ModFilesystem modFS, Character[] charactersWithMaa) {
		const string relativeHudTopGuiPath = "gui/hud_top.gui";
		var hudTopGuiPath = modFS.GetActualFileLocation(relativeHudTopGuiPath);
		if (hudTopGuiPath is null) {
			Logger.Warn($"{relativeHudTopGuiPath} not found, can't write MAA creation commands!");
			return;
		}

		string guiText = File.ReadAllText(hudTopGuiPath);

		var sb = new StringBuilder();
		sb.AppendLine(guiText.TrimEnd().TrimEnd('}'));
		sb.AppendLine("\tcontainer={");
		sb.AppendLine("\t\tname=\"IRToCK3_maa_toogle\"");
		sb.AppendLine("\t\tdatacontext=\"[GetScriptedGui('IRToCK3_create_maa')]\"");
		sb.AppendLine("\t\tvisible=\"[ScriptedGui.IsShown( GuiScope.SetRoot( GetPlayer.MakeScope ).End )]\"");

		const float duration = 0.01f;
		int state = 0;
		sb.AppendLine(
			"\t\tstate = { " +
			"name=_show " +
			$"next=state{state} " +
			"on_start=\"[ExecuteConsoleCommand('effect debug_log=LOG_SPAWNING_MAA')]\" " +
			$"duration={duration} }}"
		);
		foreach (var character in charactersWithMaa) {
			foreach (var (maaType, stacks) in character.MenAtArmsStacksPerType) {
				for (int i = 0; i < stacks; ++i) {
					// TODO: Use ExecuteConsoleCommands instead of using ExecuteConsoleCommand in a loop
					// TODO: use on_finish instead of on_start, on_start may execute twice according to a CK3 mod coop
					sb.AppendLine(
						"\t\tstate = { " +
						$"name=state{state++} " +
						$"next=state{state} " +
						$"on_start=\"[ExecuteConsoleCommand(Concatenate('add_maa {maaType} ', Localize('IRToCK3_character_{character.Id}')))]\" " +
						$"duration={duration} }}");
				}
			}
		}

		sb.AppendLine(
			"\t\tstate = { " +
			$"name=state{state} " +
			"on_start=\"[ExecuteConsoleCommand('effect remove_global_variable=IRToCK3_create_maa_flag')]\" " +
			$"duration={duration} " +
			"}");

		sb.AppendLine("\t}");
		sb.AppendLine("}");

		var outputPath = Path.Combine("output", outputModName, relativeHudTopGuiPath);
		using var output = FileOpeningHelper.OpenWriteWithRetries(outputPath, System.Text.Encoding.UTF8);
		output.Write(sb.ToString());
	}

	public static void OutputMenAtArms(string outputModName, ModFilesystem modFS, CharacterCollection ck3Characters, IdObjectCollection<string, MenAtArmsType> menAtArmsTypes) {
		Logger.Info("Writing men-at-arms spawning script...");

		var charactersWithMaa = ck3Characters.Where(c => c.MenAtArmsStacksPerType.Any()).ToArray();
		OutputHiddenEvent(outputModName, charactersWithMaa);
		OutputGuiContainer(outputModName, modFS, charactersWithMaa);
		OutputMenAtArmsTypes(outputModName, menAtArmsTypes);
	}
}