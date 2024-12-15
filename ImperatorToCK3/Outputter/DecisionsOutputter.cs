using commonItems;
using commonItems.Mods;
using CWTools.CSharp;
using CWTools.Parser;
using CWTools.Process;
using ImperatorToCK3.CK3.Titles;
using Microsoft.FSharp.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.Outputter;

internal static class DecisionsOutputter {
	internal static void TweakERERestorationDecision(Title.LandedTitles titles, ModFilesystem ck3ModFS, string outputModPath) {
		if (!titles.ContainsKey("e_byzantium")) {
			return;
		}
		
		Logger.Info("Tweaking ERE restoration decision...");
		string relativeDecisionsFilePath = "common/decisions/dlc_decisions/ep3_decisions.txt";
		string? decisionsFilePath = ck3ModFS.GetActualFileLocation(relativeDecisionsFilePath);
		if (decisionsFilePath is null) {
			Logger.Warn($"Can't find {relativeDecisionsFilePath}!");
			return;
		}
		
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		
		var fileName = Path.GetFileName(decisionsFilePath);
		var statements = CKParser.parseFile(decisionsFilePath).GetResult();
		var rootNode = Parsers.ProcessStatements(fileName, decisionsFilePath, statements);
		var nodes = rootNode.Nodes.ToArray();

		const string decisionName = "recreate_byzantine_empire_decision";
		var decisionNode = nodes.FirstOrDefault(n => n.Key == decisionName);
		if (decisionNode is null) {
			Logger.Warn($"Decision {decisionName} not found!");
			return;
		} 
		
		var isShownNode = decisionNode.Nodes.FirstOrDefault(n => n.Key == "is_shown");
		if (isShownNode is null) {
			Logger.Warn($"is_shown node not found in decision {decisionName}!");
			return;
		}
		
		List<Child> allChildren = isShownNode.AllChildren;
		const string additionalCondition = "\t\texists = title:e_byzantium.previous_holder";
		var additionalStatements = CKParser.parseString(additionalCondition, fileName).GetResult();
		var rootNodeForStatements = Parsers.ProcessStatements(fileName, decisionsFilePath, additionalStatements);
		allChildren.Add(Child.NewNodeC(rootNodeForStatements.Nodes.First()));
		isShownNode.AllChildren = allChildren;

		// Output the modified file with UTF8-BOM encoding.
		var output = rootNode.ToRaw;
		var fsharpListToOutput = ListModule.OfSeq([output]);

		var outputFilePath = Path.Join(outputModPath, relativeDecisionsFilePath);
		File.WriteAllText(outputFilePath, CKPrinter.printTopLevelKeyValueList(fsharpListToOutput), Encoding.UTF8);// TODO: check how this is outputted
	}
}