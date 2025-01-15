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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

internal static class CulturesOutputter {
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

		if (config.OutputCCULanguageParameters) {
			OutputCCULanguageParameters(outputModPath, ck3ModFS, config.GetCK3ModFlags());
		}
	}

	private static async Task OutputCultureHistory(string outputModPath, CultureCollection cultures, Date date) {
		Logger.Info("Outputting cultures history...");

		foreach (var culture in cultures) {
			await culture.OutputHistory(outputModPath, date);
		}
	}

	private static void OutputCCULanguageParameters(string outputModPath, ModFilesystem ck3ModFS, IDictionary<string, bool> ck3ModFlags) {
		Logger.Info("Outputting CCU language parameters...");
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
		branchEffectNode.AllChildren = allChildren;
		
		// Output the modified file.
		var toOutput = rootNode.AllChildren
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
		var fsharpList = ListModule.OfSeq(toOutput);

		var outputFilePath = Path.Join(outputModPath, relativePath);
		// Output the file with UTF8-BOM encoding.
		File.WriteAllText(outputFilePath, CKPrinter.printTopLevelKeyValueList(fsharpList), Encoding.UTF8);
		
		// Add the language parameters to common/scripted_guis/ccu_error_suppression.txt.
		// This is what WtWSMS does for the parameters it adds.
		var errorSuppressionRelativePath = "common/scripted_guis/ccu_error_suppression.txt";
		var errorSuppressionPath = ck3ModFS.GetActualFileLocation(errorSuppressionRelativePath);
		if (errorSuppressionPath is null) {
			Logger.Warn("Could not find ccu_error_suppression.txt in the CK3 mod. " +
			            "Some harmless errors related to converter-added language parameters may appear in error.log.");
			return;
		}

		// In the file, find the last line that contains: "if = { limit = { var:temp = flag:language_family",
		// and similar lines for the converter's language families.
		// Do the same for language branches.
		bool foundFamily = false;
		bool foundBranch = false;
		var errorSuppressionContent = File.ReadAllText(errorSuppressionPath);
		var contentLines = errorSuppressionContent.Split('\n');
		var newContent = new StringBuilder();
		foreach (var line in contentLines) {
			newContent.AppendLine(line.TrimEnd());
			if (line.Contains("if = { limit = { var:temp = flag:language_family_")) {
				foundFamily = true;
				foreach (var familyParameter in languageFamilyParameters) {
					newContent.AppendLine(
						$$"""
						  		if = {
						  			limit = { var:temp = flag:{{familyParameter}} }
						  			set_variable = { name = temp value = flag:{{familyParameter}} }
						  		}
						  """);
				}
			} else if (line.Contains("if = { limit = { var:temp = flag:language_branch_")) {
				foundBranch = true;
				foreach (var branchParameter in languageBranchParameters) {
					newContent.AppendLine(
						$$"""
						  		if = {
						  			limit = { var:temp = flag:{{branchParameter}} }
						  			set_variable = { name = temp value = flag:{{branchParameter}} }
						  		}
						  """);
				}
			}
		}
		if (!foundFamily) {
			Logger.Warn("Could not find the line to add language family parameters to in ccu_error_suppression.txt.");
		}
		if (!foundBranch) {
			Logger.Warn("Could not find the line to add language branch parameters to in ccu_error_suppression.txt.");
		}
		outputFilePath = Path.Join(outputModPath, errorSuppressionRelativePath);
		File.WriteAllText(outputFilePath, newContent.ToString(), Encoding.UTF8);
	}
}