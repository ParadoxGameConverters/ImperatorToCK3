using commonItems;
using commonItems.Collections;
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

		if (config.OutputCCUParameters) {
			OutputCCUParameters(outputModPath, ck3ModFS, config.GetCK3ModFlags());
		}
	}

	private static async Task OutputCultureHistory(string outputModPath, CultureCollection cultures, Date date) {
		Logger.Info("Outputting cultures history...");

		foreach (var culture in cultures) {
			await culture.OutputHistory(outputModPath, date);
		}
	}

	private static void OutputCCUParameters(string outputModPath, ModFilesystem ck3ModFS, OrderedDictionary<string, bool> ck3ModFlags) {
		Logger.Info("Outputting CCU heritage and language parameters...");
		
		OrderedSet<string> heritageFamilyParameters = [];
		OrderedSet<string> heritageGroupParameters = [];
		
		OrderedSet<string> languageFamilyParameters = [];
		OrderedSet<string> languageBranchParameters = [];
		OrderedSet<string> languageGroupParameters = [];
		
		// Read converter-added heritage families and groups from the configurable.
		var heritageParamsFileParser = new Parser();
		heritageParamsFileParser.RegisterKeyword("heritage_families", reader => ReadParamsIntoSet(reader, heritageFamilyParameters, ck3ModFlags));
		heritageParamsFileParser.RegisterKeyword("heritage_groups", reader => ReadParamsIntoSet(reader, heritageGroupParameters, ck3ModFlags));
		heritageParamsFileParser.ParseFile("configurables/ccu_heritage_parameters.txt");
		
		// Read converter-added language families, branches and groups from the configurable.
		var languageParamsFileParser = new Parser();
		languageParamsFileParser.RegisterKeyword("language_families", reader => ReadParamsIntoSet(reader, languageFamilyParameters, ck3ModFlags));
		languageParamsFileParser.RegisterKeyword("language_branches", reader => ReadParamsIntoSet(reader, languageBranchParameters, ck3ModFlags));
		languageParamsFileParser.RegisterKeyword("language_groups", reader => ReadParamsIntoSet(reader, languageGroupParameters, ck3ModFlags));
		languageParamsFileParser.ParseFile("configurables/ccu_language_parameters.txt");
		
		// Modify the common\scripted_effects\ccu_scripted_effects.txt file.
		var relativePath = "common/scripted_effects/ccu_scripted_effects.txt";
		// Modify the common\scripted_effects\ccu_scripted_effects.txt file.
		var scriptedEffectsPath = ck3ModFS.GetActualFileLocation(relativePath);
		if (scriptedEffectsPath is null) {
			Logger.Warn("Could not find ccu_scripted_effects.txt in the CK3 mod. Aborting the outputting of CCU parameters.");
			return;
		}
		
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		
		var fileName = Path.GetFileName(scriptedEffectsPath);
		var statements = CKParser.parseFile(scriptedEffectsPath).GetResult();
		var rootNode = Parsers.ProcessStatements(fileName, scriptedEffectsPath, statements);
		var nodes = rootNode.Nodes.ToArray();
		
		// There is a difference in the effect names between WtWSMS and RoA.
		string[] heritageFamilyEffectNames = ["ccu_initialize_heritage_family_effect", "ccu_initialize_heritage_family"];
		var heritageFamilyNode = nodes.FirstOrDefault(n => heritageFamilyEffectNames.Contains(n.Key));
		if (heritageFamilyNode is null) {
			Logger.Warn("ccu_initialize_heritage_family_effect effect not found!");
			return;
		}
		// TODO: make sure this has correct format for RoA (which has set_variable instead of add_to_variable_list).
		string[] heritageFamilyEffectNodeStrings = heritageFamilyParameters.Select(param =>
			$$"""
			if = {
				limit = { has_cultural_parameter = {{param}} }
				add_to_variable_list = { name = heritage_family target = flag:{{param}} }
			}
			""").ToArray();
		AddChildrenToNode(heritageFamilyNode, scriptedEffectsPath, fileName, heritageFamilyEffectNodeStrings);
		
		string[] heritageGroupEffectNames = ["ccu_initialize_heritage_group_effect", "ccu_initialize_heritage_group"];
		var heritageGroupNode = nodes.FirstOrDefault(n => heritageGroupEffectNames.Contains(n.Key));
		if (heritageGroupNode is null) {
			Logger.Warn("ccu_initialize_heritage_group_effect effect not found!");
			return;
		}
		// TODO: make sure this has correct format for RoA (which has set_variable instead of add_to_variable_list).
		string[] heritageGroupEffectNodeStrings = heritageGroupParameters.Select(param =>
			$$"""
				if = {
					limit = { has_cultural_parameter = {{param}} }
					add_to_variable_list = { name = heritage_group target = flag:{{param}} }
				}
			""").ToArray();
		AddChildrenToNode(heritageGroupNode, scriptedEffectsPath, fileName, heritageGroupEffectNodeStrings);

		string[] languageFamilyEffectNames = ["ccu_initialize_language_family_effect", "ccu_initialize_language_family"];
		var familyEffectNode = nodes.FirstOrDefault(n => languageFamilyEffectNames.Contains(n.Key));
		if (familyEffectNode is null) {
			Logger.Warn("ccu_initialize_language_family_effect effect not found!");
			return;
		}
		string[] effectNodeStrings = languageFamilyParameters.Select(param =>
			$$"""
            else_if = {
                limit = { has_cultural_parameter = {{param}} }
                set_variable = { name = language_family value = flag:{{param}} }
            } 
            """).ToArray();
		AddChildrenToNode(familyEffectNode, scriptedEffectsPath, fileName, effectNodeStrings);

		string[] branchEffectNames = ["ccu_initialize_language_branch_effect", "ccu_initialize_language_branch"];
		var branchEffectNode = nodes.FirstOrDefault(n => branchEffectNames.Contains(n.Key));
		if (branchEffectNode is null) {
			Logger.Warn("ccu_initialize_language_branch_effect effect not found!");
			return;
		}
		string[] branchEffectNodeStrings = languageBranchParameters.Select(param =>
			$$"""
			  else_if = {
			  	limit = { has_cultural_parameter = {{param}} }
			  	set_variable = { name = language_branch value = flag:{{param}} }
			  } 
			""").ToArray();
		AddChildrenToNode(branchEffectNode, scriptedEffectsPath, fileName, branchEffectNodeStrings);
		
		string[] groupEffectNames = ["ccu_initialize_language_group_effect", "ccu_initialize_language_group"];
		var groupEffectNode = nodes.FirstOrDefault(n => groupEffectNames.Contains(n.Key));
		if (groupEffectNode is null) {
			Logger.Warn("ccu_initialize_language_group_effect effect not found!");
			return;
		}
		string[] groupEffectNodeStrings = languageGroupParameters.Select(param =>
			$$"""
			  else_if = {
			  	limit = { has_cultural_parameter = {{param}} }
			  	set_variable = { name = language_group value = flag:{{param}} }
			  } 
			""").ToArray();
		AddChildrenToNode(groupEffectNode, scriptedEffectsPath, fileName, groupEffectNodeStrings);
		
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

		// In the file, find the last line that contains: "if = { limit = { var:temp = flag:heritage_family_",
		// and output the same line for each heritage family parameter.
		// Do the same for other heritage and language parameter types.
		bool foundHeritageFamily = false;
		bool foundHeritageGroup = false;
		bool foundLanguageFamily = false;
		bool foundLanguageBranch = false;
		bool foundLanguageGroup = false;
		var errorSuppressionContent = File.ReadAllText(errorSuppressionPath);
		var contentLines = errorSuppressionContent.Split('\n');
		var newContent = new StringBuilder();
		foreach (var line in contentLines) {
			newContent.AppendLine(line.TrimEnd());
			if (line.Contains("if = { limit = { var:temp = flag:heritage_family_")) {
				foundHeritageFamily = true;
				foreach (var param in heritageFamilyParameters) {
					newContent.AppendLine(
						$$"""
						  		if = { limit = { var:temp = flag:{{param}} set_variable = { name = temp value = flag:{{param}} } }
						  """);
				}
			} else if (line.Contains("if = { limit = { var:temp = flag:heritage_group_")) {
				foundHeritageGroup = true;
				foreach (var param in heritageGroupParameters) {
					newContent.AppendLine(
						$$"""
						  		if = { limit = { var:temp = flag:{{param}} set_variable = { name = temp value = flag:{{param}} } }
						  """);
				}
			} else if (line.Contains("if = { limit = { var:temp = flag:language_family_")) {
				foundLanguageFamily = true;
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
				foundLanguageBranch = true;
				foreach (var branchParameter in languageBranchParameters) {
					newContent.AppendLine(
						$$"""
						  		if = {
						  			limit = { var:temp = flag:{{branchParameter}} }
						  			set_variable = { name = temp value = flag:{{branchParameter}} }
						  		}
						  """);
				}
			} else if (line.Contains("if = { limit = { var:temp = flag:language_group_")) {
				foundLanguageGroup = true;
				foreach (var groupParameter in languageGroupParameters) {
					newContent.AppendLine(
						$$"""
						  		if = {
						  			limit = { var:temp = flag:{{groupParameter}} }
						  			set_variable = { name = temp value = flag:{{groupParameter}} }
						  		}
						  """);
				}
			}
		}
		if (!foundHeritageFamily) {
			Logger.Warn("Could not find the line to add heritage family parameters to in ccu_error_suppression.txt.");
		}
		if (!foundHeritageGroup) {
			Logger.Warn("Could not find the line to add heritage group parameters to in ccu_error_suppression.txt.");
		}
		if (!foundLanguageFamily) {
			Logger.Warn("Could not find the line to add language family parameters to in ccu_error_suppression.txt.");
		}
		if (!foundLanguageBranch) {
			Logger.Warn("Could not find the line to add language branch parameters to in ccu_error_suppression.txt.");
		}
		if (!foundLanguageGroup) {
			Logger.Warn("Could not find the line to add language group parameters to in ccu_error_suppression.txt.");
		}
		outputFilePath = Path.Join(outputModPath, errorSuppressionRelativePath);
		File.WriteAllText(outputFilePath, newContent.ToString(), Encoding.UTF8);
	}
	
	private static void ReadParamsIntoSet(BufferedReader reader, OrderedSet<string> paramsSet, OrderedDictionary<string, bool> ck3ModFlags) {
		var paramsParser = new Parser();
		paramsParser.RegisterModDependentBloc(ck3ModFlags);
		paramsParser.RegisterRegex(CommonRegexes.Catchall, (_, parameter) => {
			paramsSet.Add(parameter);
		});
		paramsParser.ParseStream(reader);
	}

	private static void AddChildrenToNode(Node node, string filePath, string fileName, string[] childrenStrings) {
		List<Child> allChildren = node.AllChildren;
		foreach (var childStr in childrenStrings) {
			var statementsForFamily = CKParser.parseString(childStr, fileName).GetResult();
			
			var rootNodeForFamily = Parsers.ProcessStatements(fileName, filePath, statementsForFamily);
			allChildren.Add(Child.NewNodeC(rootNodeForFamily.Nodes.First()));
		}
		node.AllChildren = allChildren;
	}
}