using commonItems;
using CWTools.CSharp;
using CWTools.Parser;
using CWTools.Process;
using CWTools.Utilities;
using ImperatorToCK3.CK3;
using ImperatorToCK3.Exceptions;
using log4net.Core;
using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ImperatorToCK3;
public static class Program {
	public static int Main(string[] args) {
		try {
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
			
			OutputCCULanguageParameters(); // TODO: REMOVE THIS
			throw new NotImplementedException();

			var converterVersion = new ConverterVersion();
			converterVersion.LoadVersion("configurables/version.txt");
			Logger.Info(converterVersion.ToString());
			if (args.Length > 0) {
				Logger.Warn("ImperatorToCK3 takes no parameters.\n" +
				            "It uses configuration.txt, configured manually or by the frontend.");
			}
			Converter.ConvertImperatorToCK3(converterVersion);
			return 0;
		} catch (Exception e) {
			Logger.Log(Level.Fatal, e is UserErrorException ? e.Message : $"{e.GetType()}: {e.Message}");
			if (e.StackTrace is not null) {
				Logger.Debug(e.StackTrace);
			}

			// Return exit code 1 for user errors. They should not be reported to Sentry.
			if (e is UserErrorException) {
				return 1;
			}
			return -1;
		}
	}
	
	private static void OutputCCULanguageParameters() {
		Logger.Info("Outputting CCU language parameters for WtWSMS...");
		List<string> languageFamilyParameters = [];
		List<string> languageBranchParameters = [];

		var ck3ModFlags = new OrderedDictionary<string, bool>();
		ck3ModFlags["wtwsms"] = true;
		
		// Read from configurable.
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
		var scriptedEffectsPath = Path.Join("D:\\GitHub\\loup99\\BP\\WtWSMS\\", relativePath); // TODO: REPLACE THE HARDCODED PATH
		
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
