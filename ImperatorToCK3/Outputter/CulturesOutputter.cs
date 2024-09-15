using commonItems;
using commonItems.Mods;
using commonItems.Serialization;
using CWTools.CSharp;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CommonUtils;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class CulturesOutputter {
	public static async Task OutputCultures(string outputModPath, CultureCollection cultures, Date date) {
		Logger.Info("Outputting cultures...");

		var sb = new StringBuilder();
		foreach (var culture in cultures) {
			sb.AppendLine($"{culture.Id}={PDXSerializer.Serialize(culture)}");
		}

		var outputPath = Path.Combine(outputModPath, "common/culture/cultures/IRtoCK3_all_cultures.txt");
		await using var output = FileHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());

		await OutputCultureHistory(outputModPath, cultures, date);
	}

	private static async Task OutputCultureHistory(string outputModPath, CultureCollection cultures, Date date) {
		Logger.Info("Outputting cultures history...");

		foreach (var culture in cultures) {
			await culture.OutputHistory(outputModPath, date);
		}
	}

	private static void OutputCCULanguageParameters(ModFilesystem ck3ModFS, IDictionary<string, bool> ck3ModFlags) {
		Logger.Info("Outputting CCU language parameters for WtWSMS...");
		List<string> languageFamilyParameters = [];
		List<string> languageBranchParameters = [];
		
		// Read from configurable.
		var fileParser = new Parser();
		fileParser.RegisterKeyword("language_families", reader => {
			var familiesParser = new Parser();
			familiesParser.RegisterModDependentBloc(ck3ModFlags);
			familiesParser.RegisterRegex(CommonRegexes.Catchall, (_, familyParameter) => {
				languageFamilyParameters.Add(familyParameter);
			});
		});
		fileParser.RegisterKeyword("language_branches", reader => {
			var branchesParser = new Parser();
			branchesParser.RegisterModDependentBloc(ck3ModFlags);
			branchesParser.RegisterRegex(CommonRegexes.Catchall, (_, branchParameter) => {
				languageBranchParameters.Add(branchParameter);
			});
		});
		fileParser.ParseFile("configurables/ccu_language_parameters.txt");
		
		// Modify the common\scripted_effects\ccu_scripted_effects.txt file.
		var scriptedEffectsPath = ck3ModFS.GetActualFileLocation("common/scripted_effects/ccu_scripted_effects.txt");
		if (scriptedEffectsPath is null) {
			Logger.Warn("Could not find ccu_scripted_effects.txt in the CK3 mod. Aborting the outputting of language parameters.");
		}
		
		// Parse the file using CWTools.
		// TODO: FINISH AND TEST THIS

		//var parsed = CWTools.Parser.JominiParser.parseEffectFile(scriptedEffectsPath);
		//var effects = parsed.GetResult();
		// var familyEffect = effects.FirstOrDefault(e => e.name == "ccu_initialize_language_family_effect");
		// Logger.Notice("Found ccu_initialize_language_family_effect");
		// foreach (var VARIABLE in familyEffect.) {
		// 	
		// }

		var parsed = CWTools.Parser.CKParser.parseFile(scriptedEffectsPath);
		var statements = parsed.GetResult();
		
		// Print all statements for debugging.
		foreach (var statement in statements) {
			Logger.Notice(statement.ToString());
		}


		var familyEffect = statements.FirstOrDefault(s => s.ToString() == "ccu_initialize_language_family_effect");



	}
}