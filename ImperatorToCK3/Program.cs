using commonItems;
using CWTools.CSharp;
using CWTools.Parser;
using CWTools.Process;
using CWTools.Utilities;
using ImperatorToCK3.CK3;
using ImperatorToCK3.Exceptions;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
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
		});
		fileParser.RegisterKeyword("language_branches", reader => {
			var branchesParser = new Parser();
			branchesParser.RegisterModDependentBloc(ck3ModFlags);
			branchesParser.RegisterRegex(CommonRegexes.Catchall, (_, branchParameter) => {
				languageBranchParameters.Add(branchParameter);
			});
		});
		// fileParser.ParseFile("configurables/ccu_language_parameters.txt"); // TODO: REENABLE THIS
		
		// Modify the common\scripted_effects\ccu_scripted_effects.txt file.
		var scriptedEffectsPath = "D:\\GitHub\\loup99\\BP\\WtWSMS\\common\\scripted_effects\\ccu_scripted_effects.txt";
		
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		var parsed = CWTools.Parser.CKParser.parseFile(scriptedEffectsPath);
		var statements = parsed.GetResult();

		var toprocess = CWTools.Parser.CKParser.parseEventFile(scriptedEffectsPath).GetResult();
		var processed = CK2Process.processEventFile(toprocess);

		var nodes = processed.Nodes.ToArray();
		// Print all leaves.
		foreach (var node in nodes) {
			Logger.Notice($"NODE: : {node.Key}");
		}
		
		// Print all statements for debugging.
		// foreach (var statement in statements) {
		// 	statement.
		// 	if (statement.IsComment) {
		// 		continue;
		// 	}
		// 	
		// 	Logger.Notice(statement.ToString());
		// }
		

		var familyEffectNode = nodes.FirstOrDefault(n => n.Key == "ccu_initialize_language_family_effect");
		if (familyEffectNode is null) {
			Logger.Error("\n\n\n Desired effect not found!\n\n\n");
		} else {
			Logger.Notice("\n\n\n DESIRED EFFECT FOUND!\n\n\n");
			
			var nodesInside = familyEffectNode.Nodes.ToArray();
			foreach (var node in nodesInside) {
				Logger.Notice($"NODE IN EFFECT: : {node.Key}");
			}
			
			// Add another else_if node after the last node.
			var key = Types.Key.NewKey("test_key");
			var keyValueItem = Types.KeyValueItem.NewKeyValueItem(key, Types.Value.NewFloat((decimal)1.0f), Types.Operator.Equals);
			familyEffectNode.AllChildren.Add(Leaf.Create(keyValueItem, new Position.range()));
			
			
			// Print all nodes inside the effect again.
			Logger.Debug("\n\n\nAFTER CHANGE: ");
			foreach (var child in familyEffectNode.AllChildren) {
				if (child.IsLeafC) {
					Logger.Notice($"LEAF IN EFFECT: : {child.leaf.Key}");
				} else if (child.IsNodeC) {
					Logger.Notice($"NODE IN EFFECT: : {child.node.Key}");
				} else {
					Logger.Notice($"OTHER IN EFFECT: : {child}");
				}
			}

			// Try to use nodes iterator.
			foreach (var childNode in familyEffectNode.Children) {
				Logger.Notice($"CHILDNODE: {childNode.Key}");
			}
		}


	}
}
