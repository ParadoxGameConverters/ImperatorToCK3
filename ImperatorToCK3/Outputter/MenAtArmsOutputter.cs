using commonItems;
using commonItems.Serialization;
using ImperatorToCK3.CK3.Characters;
using System;
using System.IO;

namespace ImperatorToCK3.Outputter; 

public static class MenAtArmsOutputter {
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
		foreach (var character in ck3Characters) {
			foreach (var (maaType, stacks) in character.MenAtArmsStacksPerType) {
				for (int i = 0; i < stacks; ++i) {
					output.WriteLine($"\tstate = {{ name=_show on_start=\"[ExecuteConsoleCommand('add_maa {maaType} {character.Id}')]\" duration={duration} on_finish=\"[GetVariableSystem.Clear( 'IRToCK3_create_maa_flag' )]\" }}");
				}
			}
		}
		output.WriteLine("}");
	}
}