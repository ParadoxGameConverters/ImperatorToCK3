using commonItems;
using commonItems.Mods;
using commonItems.Serialization;
using CWTools.CSharp;
using CWTools.Parser;
using CWTools.Process;
using ImperatorToCK3.CK3;
using ImperatorToCK3.CK3.Cultures;
using ImperatorToCK3.CommonUtils;
using Microsoft.FSharp.Collections;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

public static class CulturesOutputter {
	public static async Task OutputCultures(string outputModPath, CultureCollection cultures, ModFilesystem ck3ModFS, Configuration config, Date date) {
		Logger.Info("Outputting cultures...");

		var sb = new StringBuilder();
		foreach (var culture in cultures) {
			sb.AppendLine($"{culture.Id}={PDXSerializer.Serialize(culture)}");
		}

		var outputPath = Path.Combine(outputModPath, "common/culture/cultures/IRtoCK3_all_cultures.txt");
		await using var output = FileHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
		await output.WriteAsync(sb.ToString());

		await OutputCultureHistory(outputModPath, cultures, date);

		if (config.WhenTheWorldStoppedMakingSenseEnabled) {
			OutputCCULanguageParameters(outputModPath, ck3ModFS, config.GetCK3ModFlags());
		}
	}

	private static async Task OutputCultureHistory(string outputModPath, CultureCollection cultures, Date date) {
		Logger.Info("Outputting cultures history...");

		foreach (var culture in cultures) {
			await culture.OutputHistory(outputModPath, date);
		}
	}

	private static void OutputCCULanguageParameters(string outputModPath, ModFilesystem ck3ModFS, IDictionary<string, bool> ck3ModFlags) { // TODO: test this in real conversion
		Logger.Info("Outputting CCU language parameters for WtWSMS...");
		List<string> languageFamilyParameters = [];
		List<string> languageBranchParameters = [];
		
		// Read converter-added language families and branches from the configurable.
		var fileParser = new Parser();
		fileParser.RegisterKeyword("language_families", reader => {
			var familiesParser = new Parser();
			familiesParser.RegisterModDependentBloc(ck3ModFlags);
			familiesParser.RegisterRegex(CommonRegexes.Catchall, (_, familyParameter) => {
				languageFamilyParameters.Add(familyParameter);
			});
			familiesParser.ParseStream(reader);
		});
		fileParser.RegisterKeyword("language_branches", reader => {
			var branchesParser = new Parser();
			branchesParser.RegisterModDependentBloc(ck3ModFlags);
			branchesParser.RegisterRegex(CommonRegexes.Catchall, (_, branchParameter) => {
				languageBranchParameters.Add(branchParameter);
			});
			branchesParser.ParseStream(reader);
		});
		fileParser.ParseFile("configurables/ccu_language_parameters.txt");
		
		// Print all the loaded language families and branches.
		Logger.Notice("Loaded language families:");
		foreach (var family in languageFamilyParameters) {
			Logger.Notice(family);
		}
		Logger.Notice("Loaded language branches:");
		foreach (var branch in languageBranchParameters) {
			Logger.Notice(branch);
		}
		
		// Modify the common\scripted_effects\ccu_scripted_effects.txt file.
		var relativePath = "common/scripted_effects/ccu_scripted_effects.txt";
		// Modify the common\scripted_effects\ccu_scripted_effects.txt file.
		var scriptedEffectsPath = ck3ModFS.GetActualFileLocation(relativePath);
		if (scriptedEffectsPath is null) {
			Logger.Warn("Could not find ccu_scripted_effects.txt in the CK3 mod. Aborting the outputting of language parameters.");
		}
		
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		
		var fileName = Path.GetFileName(scriptedEffectsPath);
		var statements = CKParser.parseFile(scriptedEffectsPath).GetResult();
		//var statements = Parsers.ParseScriptFile(fileName, File.ReadAllText(scriptedEffectsPath)).GetResult();
		var rootNode = Parsers.ProcessStatements(fileName, scriptedEffectsPath, statements);

		var nodes = rootNode.Nodes.ToArray();

		var familyEffectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_language_family_effect");
		if (familyEffectNode is null) {
			Logger.Warn("ccu_initialize_language_family_effect effect not found!");
			return;
		} 
		List<Child> allChildren = familyEffectNode.AllChildren;
		foreach (var languageFamily in languageFamilyParameters) {
			var statementsForFamily = CKParser.parseString(
			$$"""
			else_if = {
				limit = { has_cultural_parameter = {{languageFamily}} }
				set_variable = { name = language_family value = flag:{{languageFamily}} }
			} 
			""", fileName).GetResult();
			
			var rootNodeForFamily = Parsers.ProcessStatements(fileName, scriptedEffectsPath, statementsForFamily);
			allChildren.Add(Child.NewNodeC(rootNodeForFamily.Nodes.First()));
		}
		familyEffectNode.AllChildren = allChildren;

		var branchEffectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_language_branch_effect");
		if (branchEffectNode is null) {
			Logger.Warn("ccu_initialize_language_branch_effect effect not found!");
			return;
		}
		allChildren = branchEffectNode.AllChildren;
		foreach (var languageBranch in languageBranchParameters) {
			var statementsForBranch = CKParser.parseString(
			$$"""
			else_if = {
				limit = { has_cultural_parameter = {{languageBranch}} }
				set_variable = { name = language_branch value = flag:{{languageBranch}} }
			} 
			""", fileName).GetResult();
			
			var rootNodeForBranch = Parsers.ProcessStatements(fileName, scriptedEffectsPath, statementsForBranch);
			allChildren.Add(Child.NewNodeC(rootNodeForBranch.Nodes.First()));
		}
		
		// Output the modified file.
		var tooutput = rootNode.AllChildren
			.Select(c => {
				if (c.IsLeafC) {
					return c.leaf.ToRaw;
				} else if (c.IsNodeC) {
					return c.node.ToRaw;
				}

				return null;
			})
			.Where(s => s is not null)
			.Cast<Types.Statement>()
			.ToList();
		var fsharpList = ListModule.OfSeq(tooutput);

		var outputFilePath = Path.Join(outputModPath, relativePath);
		// Output the file with UTF8-BOM encoding.
		File.WriteAllText(outputFilePath, CKPrinter.printTopLevelKeyValueList(fsharpList), encoding: Encoding.UTF8);
	}
}