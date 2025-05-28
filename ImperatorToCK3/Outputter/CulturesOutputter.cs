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

		var culturesList = cultures.ToList();
		
		// Make sure parent cultures are output before their children.
		// For every culture, check the last index of a parent culture in the list.
		// If the last parent's index is greater than the current culture's index, swap the two.
		for (var i = 0; i < culturesList.Count; ++i) {
			var culture = culturesList[i];
			
			if (culture.ParentCultureIds.Count == 0) {
				continue;
			}
			
			var lastParentIndex = culturesList.FindLastIndex(c => culture.ParentCultureIds.Contains(c.Id));
			if (lastParentIndex > i) {
				(culturesList[i], culturesList[lastParentIndex]) = (culturesList[lastParentIndex], culturesList[i]);
			}
		}
		
		// Output cultures grouped by heritage.
		foreach (var group in culturesList.GroupBy(c => c.Heritage)) {
			var sb = new StringBuilder();
			foreach (var culture in group) {
				sb.AppendLine($"{culture.Id} = {PDXSerializer.Serialize(culture)}");
			}
			
			var outputPath = Path.Combine(outputModPath, $"common/culture/cultures/{group.Key.Id}.txt");
			await using var output = FileHelper.OpenWriteWithRetries(outputPath, Encoding.UTF8);
			await output.WriteAsync(sb.ToString());
		}

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
		const string relativePath = "common/scripted_effects/ccu_scripted_effects.txt";
		// Modify the common\scripted_effects\ccu_scripted_effects.txt file.
		var scriptedEffectsPath = ck3ModFS.GetActualFileLocation(relativePath);
		if (scriptedEffectsPath is null) {
			Logger.Warn("Could not find ccu_scripted_effects.txt in the CK3 mod. Aborting the outputting of CCU parameters.");
			return;
		}
		Logger.Debug($"Found ccu_scripted_effects.txt at {scriptedEffectsPath}");

		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		
		string fileText = File.ReadAllText(scriptedEffectsPath, Encoding.UTF8);
		var fileName = Path.GetFileName(scriptedEffectsPath);
		var statements = CKParser.parseFile(scriptedEffectsPath).GetResult();
		var rootNode = Parsers.ProcessStatements(fileName, scriptedEffectsPath, statements);
		var nodes = rootNode.Nodes.ToArray();

		OutputHeritageFamilyParameters(ck3ModFlags, nodes, heritageFamilyParameters, scriptedEffectsPath, fileName, fileText);
		OutputHeritageGroupParameters(ck3ModFlags, nodes, heritageGroupParameters, scriptedEffectsPath, fileName, fileText);

		OutputLanguageFamilyParameters(ck3ModFlags, nodes, languageFamilyParameters, scriptedEffectsPath, fileName, fileText);
		// As of 2025-01-16, only WtWSMS uses the language_branch parameter type.
		if (ck3ModFlags["wtwsms"]) {
			OutputLanguageBranchParameters(nodes, languageBranchParameters, scriptedEffectsPath, fileName);
		}
		OutputLanguageGroupParameters(ck3ModFlags, nodes, languageGroupParameters, scriptedEffectsPath, fileName, fileText);

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
		
		// For WtWSMS, add the heritage and language parameters to common/scripted_guis/ccu_error_suppression.txt.
		// This is what WtWSMS does for the parameters it adds.
		if (ck3ModFlags["wtwsms"]) {
			OutputCCUErrorSuppression(outputModPath, ck3ModFS, heritageFamilyParameters, heritageGroupParameters, languageFamilyParameters, languageBranchParameters, languageGroupParameters);
		}
	}

	private static void OutputLanguageBranchParameters(Node[] nodes, OrderedSet<string> languageBranchParameters,
		string scriptedEffectsPath, string fileName)
	{
		var branchEffectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_language_branch_effect");
		if (branchEffectNode is null) {
			Logger.Warn("Failed to find the scripted effect for CCU language branch parameters!");
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
	}

	private static void OutputLanguageGroupParameters(OrderedDictionary<string, bool> ck3ModFlags, Node[] nodes,
		OrderedSet<string> languageGroupParameters, string scriptedEffectsPath, string fileName, string fileText)
	{
		Node? groupEffectNode = null;
		if (ck3ModFlags["wtwsms"]) {
			groupEffectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_language_group_effect");
		} else if (ck3ModFlags["roa"]) {
			groupEffectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_language_group");
		} else if (ck3ModFlags["tfe"]) {
			groupEffectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_culture");
		}
		
		if (groupEffectNode is null) {
			Logger.Warn("Failed to find the scripted effect for CCU language group parameters!");
			return;
		}
		string[] groupEffectNodeStrings;
		if (ck3ModFlags["wtwsms"] || ck3ModFlags["roa"]) {
			groupEffectNodeStrings = languageGroupParameters.Select(param =>
				$$"""
				    else_if = {
				    	limit = { has_cultural_parameter = {{param}} }
				    	set_variable = { name = language_group value = flag:{{param}} }
				    } 
				  """).ToArray();
		} else if (ck3ModFlags["tfe"]) {
			// Only start searching for available numbers from 100, because there are some existing entries in the file. 
			int newVariableValue = 100;
			while (fileText.Contains($"set_variable = {{ name = language_group value = {newVariableValue} }}")) {
				++newVariableValue;
			}
			groupEffectNodeStrings = languageGroupParameters.Select(param =>
				$$"""
				  else_if = {
				      limit = { has_cultural_parameter = {{param}} }
				      set_variable = { name = language_group value = {{newVariableValue++}} }
				  } 
				  """).ToArray();
		} else {
			groupEffectNodeStrings = [];
		}
		
		if (ck3ModFlags["tfe"]) {
			AddChildrenToNodeAfterLastChildContainingText(groupEffectNode, scriptedEffectsPath, fileName, groupEffectNodeStrings, "name = language_group");
		} else {
			AddChildrenToNode(groupEffectNode, scriptedEffectsPath, fileName, groupEffectNodeStrings);
		}
	}

	private static void OutputLanguageFamilyParameters(OrderedDictionary<string, bool> ck3ModFlags, Node[] nodes, OrderedSet<string> languageFamilyParameters,
		string scriptedEffectsPath, string fileName, string fileText) {
		Node? effectNode = null;
		if (ck3ModFlags["wtwsms"]) {
			effectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_language_family_effect");
		} else if (ck3ModFlags["roa"]) {
			effectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_language_family");
		} else if (ck3ModFlags["tfe"]) {
			effectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_culture");
		}
		
		if (effectNode is null) {
			Logger.Warn("Failed to find the scripted effect for CCU language family parameters!");
			return;
		}
		
		string[] effectNodeStrings;
		if (ck3ModFlags["wtwsms"] || ck3ModFlags["roa"]) {
			effectNodeStrings = languageFamilyParameters.Select(param =>
				$$"""
				  else_if = {
				      limit = { has_cultural_parameter = {{param}} }
				      set_variable = { name = language_family value = flag:{{param}} }
				  } 
				  """).ToArray();
		} else if (ck3ModFlags["tfe"]) {
			// Only start searching for available numbers from 100, because there are some existing entries in the file. 
			int newVariableValue = 100;
			while (fileText.Contains($"set_variable = {{ name = language_family value = {newVariableValue} }}")) {
				++newVariableValue;
			}
			effectNodeStrings = languageFamilyParameters.Select(param =>
				$$"""
				  else_if = {
				      limit = { has_cultural_parameter = {{param}} }
				      set_variable = { name = language_family value = {{newVariableValue++}} }
				  } 
				  """).ToArray();
		} else {
			effectNodeStrings = [];
		}
		
		if (ck3ModFlags["tfe"]) {
			AddChildrenToNodeAfterLastChildContainingText(effectNode, scriptedEffectsPath, fileName, effectNodeStrings, "name = language_family");
		} else {
			AddChildrenToNode(effectNode, scriptedEffectsPath, fileName, effectNodeStrings);
		}
	}

	private static void OutputHeritageGroupParameters(OrderedDictionary<string, bool> ck3ModFlags, Node[] nodes,
		OrderedSet<string> heritageGroupParameters, string scriptedEffectsPath, string fileName, string fileText)
	{
		Node? effectNode = null;
		if (ck3ModFlags["wtwsms"]) {
			effectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_heritage_group_effect");
		} else if (ck3ModFlags["roa"]) {
			effectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_heritage_group");
		} else if (ck3ModFlags["tfe"]) {
			effectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_culture");
		}
		
		if (effectNode is null) {
			Logger.Warn("Failed to find the scripted effect for CCU heritage group parameters!");
			return;
		}
		
		// There is a difference in the heritage group effect formats between WtWSMS and RoA.
		string[] heritageGroupEffectNodeStrings;
		if (ck3ModFlags["wtwsms"]) {
			heritageGroupEffectNodeStrings = heritageGroupParameters.Select(param =>
				$$"""
				   	if = {
				   		limit = { has_cultural_parameter = {{param}} }
				   		add_to_variable_list = { name = heritage_group target = flag:{{param}} }
				   	}
				  """).ToArray();
		} else if (ck3ModFlags["roa"]) {
			heritageGroupEffectNodeStrings = heritageGroupParameters.Select(param =>
				$$"""
				    	else_if = {
				    		limit = { has_cultural_parameter = {{param}} }
				    		set_variable = { name = heritage_group value = flag:{{param}} }
				    	}
				  """).ToArray();
		} else if (ck3ModFlags["tfe"]) {
			// Only start searching for available numbers from 100, because there are some existing entries in the file. 
			int newVariableValue = 100;
			while (fileText.Contains($"set_variable = {{ name = heritage_group value = {newVariableValue} }}")) {
				++newVariableValue;
			}
			heritageGroupEffectNodeStrings = heritageGroupParameters.Select(param =>
				$$"""
				    	else_if = {
				    		limit = { has_cultural_parameter = {{param}} }
				    		set_variable = { name = heritage_group value = {{newVariableValue++}} }
				    	}
				  """).ToArray();
		} else {
			heritageGroupEffectNodeStrings = [];
		}
		
		if (ck3ModFlags["tfe"]) {
			AddChildrenToNodeAfterLastChildContainingText(effectNode, scriptedEffectsPath, fileName, heritageGroupEffectNodeStrings, "name = heritage_group");
		} else {
			AddChildrenToNode(effectNode, scriptedEffectsPath, fileName, heritageGroupEffectNodeStrings);
		}
	}

	private static void OutputHeritageFamilyParameters(OrderedDictionary<string, bool> ck3ModFlags, Node[] nodes,
		OrderedSet<string> heritageFamilyParameters, string scriptedEffectsPath, string fileName, string fileText)
	{
		// There is a difference in the heritage group effect formats between WtWSMS and RoA/TFE.
		Node? effectNode = null;
		if (ck3ModFlags["wtwsms"]) {
			effectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_heritage_family_effect");
		} else if (ck3ModFlags["roa"]) {
			effectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_heritage_family");
		} else if (ck3ModFlags["tfe"]) {
			effectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_culture");
		}
		
		if (effectNode is null) {
			Logger.Warn("Failed to find the scripted effect for CCU heritage family parameters!");
			return;
		}

		string[] heritageFamilyEffectNodeStrings;
		if (ck3ModFlags["wtwsms"]) {
			heritageFamilyEffectNodeStrings = heritageFamilyParameters.Select(param =>
				$$"""
				    if = {
				    	limit = { has_cultural_parameter = {{param}} }
				    	add_to_variable_list = { name = heritage_family target = flag:{{param}} }
				    }
				  """).ToArray();
		} else if (ck3ModFlags["roa"]) {
			heritageFamilyEffectNodeStrings = heritageFamilyParameters.Select(param =>
				$$"""
				    	else_if = {
				    		limit = { has_cultural_parameter = {{param}} }
				    		set_variable = { name = heritage_family value = flag:{{param}} }
				    	}
				  """).ToArray();
		} else if (ck3ModFlags["tfe"]) {
			// Only start searching for available numbers from 100, because there are some existing entries in the file. 
			int newVariableValue = 100;
			while (fileText.Contains($"set_variable = {{ name = heritage_group value = {newVariableValue} }}")) {
				++newVariableValue;
			}
			heritageFamilyEffectNodeStrings = heritageFamilyParameters.Select(param =>
				$$"""
				    	else_if = {
				    		limit = { has_cultural_parameter = {{param}} }
				    		set_variable = { name = heritage_family value = {{newVariableValue++}} }
				    	}
				  """).ToArray();
		} else {
			heritageFamilyEffectNodeStrings = [];
		}
		
		if (ck3ModFlags["tfe"]) {
			AddChildrenToNodeAfterLastChildContainingText(effectNode, scriptedEffectsPath, fileName, heritageFamilyEffectNodeStrings, "name = heritage_family");
		} else {
			AddChildrenToNode(effectNode, scriptedEffectsPath, fileName, heritageFamilyEffectNodeStrings);
		}
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
			var statementsForFamilystatements = CKParser.parseString(childStr, fileName).GetResult();
			
			var rootNodeForStatements = Parsers.ProcessStatements(fileName, filePath, statementsForFamilystatements);
			allChildren.Add(Child.NewNodeC(rootNodeForStatements.Nodes.First()));
		}
		node.AllChildren = allChildren;
	}
	
	private static void AddChildrenToNodeAfterLastChildContainingText(Node node, string filePath, string fileName, string[] childrenStrings, string precedingChildText) {
		List<Child> allChildren = node.AllChildren;
		
		Child? precedingChild = allChildren.LastOrNull(c => CKPrinter.api.prettyPrintStatement.Invoke(c.node.ToRaw).Contains(precedingChildText));
		if (!precedingChild.HasValue) {
			Logger.Warn($"Failed to find the preceding child containing the text '{precedingChildText}'!");
			return;
		}
		
		int indexToUse = allChildren.IndexOf(precedingChild.Value) + 1;
		
		foreach (var childStr in childrenStrings) {
			var statements = CKParser.parseString(childStr, fileName).GetResult();
			
			var rootNodeForStatements = Parsers.ProcessStatements(fileName, filePath, statements);
			allChildren.Insert(indexToUse++, Child.NewNodeC(rootNodeForStatements.Nodes.First()));
		}
		node.AllChildren = allChildren;
	}

	private static void OutputCCUErrorSuppression(string outputModPath, ModFilesystem ck3ModFS,
		OrderedSet<string> heritageFamilyParameters, OrderedSet<string> heritageGroupParameters,
		OrderedSet<string> languageFamilyParameters, OrderedSet<string> languageBranchParameters,
		OrderedSet<string> languageGroupParameters) {
		const string errorSuppressionRelativePath = "common/scripted_guis/ccu_error_suppression.txt";
		var errorSuppressionPath = ck3ModFS.GetActualFileLocation(errorSuppressionRelativePath);
		if (errorSuppressionPath is null) {
			Logger.Warn("Could not find ccu_error_suppression.txt in the CK3 mod. " +
			            "Some harmless errors related to converter-added language parameters may appear in error.log.");
			return;
		}
		Logger.Debug($"Found ccu_error_suppression.txt at {errorSuppressionPath}");

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

		string outputFilePath = Path.Join(outputModPath, errorSuppressionRelativePath);
		File.WriteAllText(outputFilePath, newContent.ToString(), Encoding.UTF8);
	}
}