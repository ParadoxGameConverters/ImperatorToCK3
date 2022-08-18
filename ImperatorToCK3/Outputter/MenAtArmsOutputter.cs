using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Characters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ImperatorToCK3.Outputter; 

public static class MenAtArmsOutputter {
	private static void OutputHiddenEvent(string outputModName, IEnumerable<Character> charactersWithMaa) {
		var outputPath = Path.Combine("output", outputModName, "events", "IRToCK3_hidden_events.txt");
		using var outputStream = File.OpenWrite(outputPath);
		using var output = new StreamWriter(outputStream, System.Text.Encoding.UTF8);
		
		output.WriteLine("namespace=IRToCK3_hidden_events");
		output.WriteLine();
		output.WriteLine("IRToCK3_hidden_events.0001 = {");
		output.WriteLine("\ttype = character_event");
		output.WriteLine("\thidden = yes");
		output.WriteLine("\timmediate = {");
		foreach (var character in charactersWithMaa) {
			output.WriteLine(
				"\t\tset_variable = { " +
			    $"name=IRToCK3_character_{character.Id} " +
			    $"value=character:{character.Id} " +
				"}");
		}
		output.WriteLine("\t\tset_global_variable = IRToCK3_create_maa_flag");
		output.WriteLine("\t}");
		output.WriteLine("}");
	}
	
	public static void OutputMenAtArms(string outputModName, CharacterCollection ck3Characters) {
		Logger.Info("Writing men-at-arms...");
		
		var outputPath = Path.Combine("output", outputModName, "gui", "IRToCK3_gui.gui");
		using var outputStream = File.OpenWrite(outputPath);
		using var output = new StreamWriter(outputStream, System.Text.Encoding.UTF8);
		
		output.WriteLine("container={");
		output.WriteLine("\tname=\"IRToCK3_maa_toogle\"");
		output.WriteLine("\tparentanchor = top|hcenter"); // TODO: check if needed
		output.WriteLine("\tposition = { 0 185 }"); // TODO: check if needed
		output.WriteLine("\tlayer = top"); // TODO: check if needed
		output.WriteLine("\tdatacontext=\"[GetScriptedGui('IRToCK3_create_maa')]\"");
		output.WriteLine("\tvisible=\"[ScriptedGui.IsShown( GuiScope.SetRoot( GetPlayer.MakeScope ).End )]\"");
		
		const float duration = 0.01f;
		int state = 0;
		output.WriteLine("\tstate = { " +
		                 "name=_show " +
		                 $"next=state{state} " +
		                 "on_start=\"[ExecuteConsoleCommand('effect debug_log=LOG_SPAWNING_MAA')]\" " +
		                 $"duration={duration.ToString(CultureInfo.InvariantCulture)} }}");

		var charactersWithMaa = ck3Characters.Where(c => c.MenAtArmsStacksPerType.Any()).ToList();
		OutputHiddenEvent(outputModName, charactersWithMaa);
		foreach (var character in charactersWithMaa) {
			foreach (var (maaType, stacks) in character.MenAtArmsStacksPerType) {
				for (int i = 0; i < stacks; ++i) {
					output.WriteLine(
						"\tstate = { " +
		                 $"name=state{state++} " +
		                 $"next=state{state} " +
		                 $"on_start=\"[ExecuteConsoleCommand(Concatenate('add_maa {maaType} ', Localize('IRToCK3_character_{character.Id}')))]\" " +
		                 $"duration={duration.ToString(CultureInfo.InvariantCulture)} }}");
				}
			}
		}
		output.WriteLine(
			"\tstate = { " +
	         $"name=state{state} " +
	         "on_start=\"[ExecuteConsoleCommand('instabuild')]\" " + // Gives regiments full strength
	         $"duration={duration.ToString(CultureInfo.InvariantCulture)} " +
	         "on_finish=\"[ExecuteConsoleCommand('effect remove_global_variable=IRToCK3_create_maa_flag')]\" " +
	         "}");
		
		output.WriteLine("}");
	}
}