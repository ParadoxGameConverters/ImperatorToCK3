using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

internal enum LineEnding {
	CRLF,
	LF,
	CR
}

internal static class FileTweaker {
	public static async Task ModifyAndRemovePartsOfFiles(ModFilesystem ck3ModFS, string outputModPath, Configuration config) {
		// Load removable blocks from configurables.
		Dictionary<string, OrderedSet<PartOfFileToModify>> partsToModifyPerFile = [];
		
		if (config.FallenEagleEnabled) {
			Logger.Info("Reading unneeded parts of Fallen Eagle files...");
			ReadPartsOfFileToRemove(partsToModifyPerFile, "configurables/removable_file_blocks_tfe.txt", warnIfNotFound: true);
			
			Logger.Info("Reading parts of Fallen Eagle files to modify...");
			ReadPartsOfFileToReplace(partsToModifyPerFile, "configurables/replaceable_file_blocks_tfe.txt", warnIfNotFound: true);
		}

		if (config.RajasOfAsiaEnabled) {
			Logger.Info("Reading unneeded parts of Rajas of Asia files...");
			ReadPartsOfFileToRemove(partsToModifyPerFile, "configurables/removable_file_blocks_roa.txt", warnIfNotFound: true);
			
			Logger.Info("Reading parts of Rajas of Asia files to modify...");
			ReadPartsOfFileToReplace(partsToModifyPerFile, "configurables/replaceable_file_blocks_roa.txt", warnIfNotFound: true);
		}

		if (config.AsiaExpansionProjectEnabled) {
			Logger.Info("Reading unneeded parts of Rajas of Asia files...");
			ReadPartsOfFileToRemove(partsToModifyPerFile, "configurables/removable_file_blocks_aep.txt", warnIfNotFound: true);
			
			Logger.Info("Reading parts of Asia Expansion Project files to modify...");
			ReadPartsOfFileToReplace(partsToModifyPerFile, "configurables/replaceable_file_blocks_aep.txt", warnIfNotFound: true);
		}

		if (config.WhenTheWorldStoppedMakingSenseEnabled) {
			Logger.Info("Reading unneeded parts of When the World Stopped Making Sense files...");
			ReadPartsOfFileToRemove(partsToModifyPerFile, "configurables/removable_file_blocks_wtwsms.txt", warnIfNotFound: true);

			Logger.Info("Reading parts of When the World Stopped Making Sense files to modify...");
			ReadPartsOfFileToReplace(partsToModifyPerFile, "configurables/replaceable_file_blocks_wtwsms.txt", warnIfNotFound: true);
		}

		bool isVanilla = config.GetLiquidFlags()["vanilla"];
		Logger.Info("Reading unneeded parts of vanilla files...");
		ReadPartsOfFileToRemove(partsToModifyPerFile, "configurables/removable_file_blocks.txt", warnIfNotFound: isVanilla);
		Logger.Info("Reading parts of vanilla files to modify...");
		ReadPartsOfFileToReplace(partsToModifyPerFile, "configurables/replaceable_file_blocks.txt", warnIfNotFound: isVanilla);
		
		await ModifyPartsOfFiles(partsToModifyPerFile, ck3ModFS, outputModPath);
	}

	private static void ReadPartsOfFileToRemove(Dictionary<string, OrderedSet<PartOfFileToModify>> partsToModifyPerFile, string configurablePath, bool warnIfNotFound) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, fileName) => {
			ReadBlocksToRemoveForFile(fileName, reader, partsToModifyPerFile, warnIfNotFound);
		});
		parser.RegisterRegex(CommonRegexes.QuotedString, (reader, fileNameInQuotes) => {
			string fileName = fileNameInQuotes.RemQuotes();
			ReadBlocksToRemoveForFile(fileName, reader, partsToModifyPerFile, warnIfNotFound);
		});
		parser.IgnoreAndLogUnregisteredItems();

		parser.ParseFile(configurablePath);
	}

	private static void ReadBlocksToRemoveForFile(string fileName, BufferedReader reader, Dictionary<string, OrderedSet<PartOfFileToModify>> partsToModifyPerFile, bool warnIfNotFound) {
		var blocksToRemove = new BlobList(reader).Blobs.Select(b => b.Trim()).ToArray();
			
		if (partsToModifyPerFile.TryGetValue(fileName, out var existingBlocksToModify)) {
			var alreadyExistingBlocks = existingBlocksToModify.Select(b => b.TextBefore);
			var newBlocksToRemove = blocksToRemove
				.Except(alreadyExistingBlocks)
				.Select(b => new PartOfFileToModify(textBefore: b, textAfter: string.Empty, warnIfNotFound: warnIfNotFound));
			existingBlocksToModify.UnionWith(newBlocksToRemove);
		} else {
			partsToModifyPerFile[fileName] = blocksToRemove
				.Select(b => new PartOfFileToModify(textBefore: b, textAfter: string.Empty, warnIfNotFound: warnIfNotFound))
				.ToOrderedSet();
		}
	}
	
	private static void ReadPartsOfFileToReplace(Dictionary<string, OrderedSet<PartOfFileToModify>> partsToModifyPerFile, string configurablePath, bool warnIfNotFound) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, fileName) => {
			ReadBlocksToReplaceForFile(fileName, reader, partsToModifyPerFile, warnIfNotFound);
		});
		parser.RegisterRegex(CommonRegexes.QuotedString, (reader, fileNameInQuotes) => {
			string fileName = fileNameInQuotes.RemQuotes();
			ReadBlocksToReplaceForFile(fileName, reader, partsToModifyPerFile, warnIfNotFound);
		});
		parser.IgnoreAndLogUnregisteredItems();
		
		parser.ParseFile(configurablePath);
	}
	
	private static void ReadBlocksToReplaceForFile(string fileName, BufferedReader reader, Dictionary<string, OrderedSet<PartOfFileToModify>> partsToModifyPerFile, bool warnIfNotFound) {
		string? before = null;
		string? after = null;
		
		var parserForFile = new Parser();
		parserForFile.RegisterKeyword("replace", replaceBlockReader => {
			var replaceBlockParser = new Parser();
			replaceBlockParser.RegisterKeyword("before", beforeReader => {
				// Remove opening and closing braces.
				before = beforeReader.GetStringOfItem().ToString().Trim()[1..^1].Trim();
			});
			replaceBlockParser.RegisterKeyword("after", afterReader => {
				after = afterReader.GetStringOfItem().ToString().Trim()[1..^1].Trim();
			});
			replaceBlockParser.IgnoreAndLogUnregisteredItems();
			replaceBlockParser.ParseStream(replaceBlockReader);
			
			if (before is null || after is null) {
				Logger.Warn($"Invalid replace block for {fileName}.");
				return;
			} 
			
			// Add to partsToModifyPerFile.
			var partOfFileToModify = new PartOfFileToModify(textBefore: before, textAfter: after, warnIfNotFound: warnIfNotFound);
			if (partsToModifyPerFile.TryGetValue(fileName, out var existingBlocksToModify)) {
				var alreadyExistingBlocks = existingBlocksToModify.Select(b => b.TextBefore);
				if (alreadyExistingBlocks.Contains(before)) {
					Logger.Warn($"Duplicate replace block for {fileName}: {before}");
					return;
				}
				existingBlocksToModify.Add(partOfFileToModify);
			} else {
				partsToModifyPerFile[fileName] = [partOfFileToModify];
			}
		});
		parserForFile.IgnoreAndLogUnregisteredItems();
		parserForFile.ParseStream(reader);
	}

	private static async Task ModifyPartsOfFiles(Dictionary<string, OrderedSet<PartOfFileToModify>> partsToModifyPerFile, ModFilesystem ck3ModFS, string outputModPath) {
		// Log count of blocks to remove for each file.
		foreach (var (relativePath, partsToRemove) in partsToModifyPerFile) {
			Logger.Debug($"Loaded {partsToRemove.Count} blocks to remove from {relativePath}.");
		}

		foreach (var (relativePath, partsToRemove) in partsToModifyPerFile) {
			var inputPath = ck3ModFS.GetActualFileLocation(relativePath);
			if (inputPath is null) {
				Logger.Warn($"{relativePath} not found in mod filesystem.");
				continue;
			}
			if (!File.Exists(inputPath)) {
				Logger.Warn($"{relativePath} not found at {inputPath}.");
				continue;
			}

			LineEnding lineEndings = GetLineEndingsInFile(inputPath);
			
			var fileContent = await File.ReadAllTextAsync(inputPath);

			foreach (var (blockBefore, blockAfter, warnIfNotFound) in partsToRemove) {
				string searchString = NormalizeToLineEnding(blockBefore, lineEndings);
				string replacementString = NormalizeToLineEnding(blockAfter, lineEndings);
				
				if (!fileContent.Contains(searchString)) {
					if (warnIfNotFound) {
						string escapedSearchString = searchString.Replace("\r", "\\r").Replace("\n", "\\n");
						Logger.Warn($"Block not found in file {relativePath}: {escapedSearchString}");
					}
					continue;
				}
				
				fileContent = fileContent.Replace(searchString, replacementString);
			}

			string outputPath = $"{outputModPath}/{relativePath}";
			// Make sure the output directory exists.
			var outputDir = Path.GetDirectoryName(outputPath);
			if (!Directory.Exists(outputDir) && !string.IsNullOrEmpty(outputDir)) {
				Directory.CreateDirectory(outputDir);
			}
			
			await using var output = FileHelper.OpenWriteWithRetries(outputPath);
			await output.WriteAsync(fileContent);
		}
	}

	private static string NormalizeToLineEnding(string text, LineEnding targetLineEnding) {
		// Normalize any combination of CRLF/CR/LF to LF first.
		var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
		return targetLineEnding switch {
			LineEnding.CRLF => normalized.Replace("\n", "\r\n"),
			LineEnding.CR => normalized.Replace("\n", "\r"),
			_ => normalized,
		};
	}

	private static LineEnding GetLineEndingsInFile(string filePath) {
		using StreamReader sr = new StreamReader(filePath);
		bool returnSeen = false;
		while (sr.Peek() >= 0) {
			char c = (char)sr.Read();
			if (c == '\n') {
				return returnSeen ? LineEnding.CRLF : LineEnding.LF;
			}

			if (returnSeen) {
				return LineEnding.CR;
			}

			returnSeen = c == '\r';
		}

		return returnSeen ? LineEnding.CR : LineEnding.LF;
	}
}