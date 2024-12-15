using commonItems;
using commonItems.Mods;
using CWTools.CSharp;
using CWTools.Parser;
using CWTools.Process;
using ImperatorToCK3.CK3.Titles;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

internal static class DecisionsOutputter {
	internal static async Task TweakERERestorationDecision(Title.LandedTitles titles, ModFilesystem ck3ModFS,
		string outputModPath) {
		if (!titles.ContainsKey("e_byzantium")) {
			return;
		}

		Logger.Info("Tweaking ERE restoration decision...");
		const string relativeDecisionsFilePath = "common/decisions/dlc_decisions/ep3_decisions.txt";

		// The file may already be in the output mod.
		string? decisionsFilePath;
		string fileInOutputPath = Path.Join(outputModPath, relativeDecisionsFilePath);
		if (File.Exists(fileInOutputPath)) {
			decisionsFilePath = fileInOutputPath;
		} else {
			decisionsFilePath = ck3ModFS.GetActualFileLocation(relativeDecisionsFilePath);
		}

		if (decisionsFilePath is null) {
			Logger.Warn($"Can't find {relativeDecisionsFilePath}!");
			return;
		}

		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		var fileName = Path.GetFileName(decisionsFilePath);

		var text = await File.ReadAllTextAsync(decisionsFilePath);
		var parsed = Parsers.ParseScriptFile(fileName, text);
		var decisionsFile = parsed.GetResult();

		var processed = Parsers.ProcessStatements(fileName, decisionsFilePath, decisionsFile);

		const string decisionName = "recreate_byzantine_empire_decision";
		var decisionNode = processed.Nodes.FirstOrDefault(n => n.Key == decisionName);
		if (decisionNode is null) {
			Logger.Warn($"Decision {decisionName} not found!");
			return;
		}

		var isShownNode = decisionNode.Nodes.FirstOrDefault(n => n.Key == "is_shown");
		if (isShownNode is null) {
			Logger.Warn($"is_shown node not found in decision {decisionName}!");
			return;
		}

		const string additionalCondition = "\t\texists = title:e_byzantium.previous_holder";
		var additionalStatements = CKParser.parseString(additionalCondition, fileName).GetResult();
		var rootNodeForStatements = Parsers.ProcessStatements(fileName, decisionsFilePath, additionalStatements);

		var newChild = Child.NewLeafC(rootNodeForStatements.Leaves.First());
		isShownNode.SetTag(newChild.leaf.Key, newChild);

		StringBuilder sb = new();
		foreach (var child in processed.Children) {
			sb.AppendLine(CKPrinter.api.prettyPrintStatement.Invoke(child.ToRaw));
		}

		// Output the modified file with UTF8-BOM encoding.
		var outputFilePath = Path.Join(outputModPath, relativeDecisionsFilePath);
		await File.WriteAllTextAsync(outputFilePath, sb.ToString(), Encoding.UTF8);
	}
}