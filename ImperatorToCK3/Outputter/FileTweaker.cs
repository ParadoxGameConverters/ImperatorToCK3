using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

internal readonly struct PartOfFileToModify(string textBefore, string textAfter, bool warnIfNotFound = true) {
	internal readonly string TextBefore = textBefore;
	internal readonly string TextAfter = textAfter;
	internal readonly bool WarnIfNotFound = warnIfNotFound;

	public void Deconstruct(out string textBefore, out string textAfter, out bool warnIfNotFound) {
		textBefore = TextBefore;
		textAfter = TextAfter;
		warnIfNotFound = WarnIfNotFound;
	}
}

internal enum LineEnding {
	CRLF,
	LF,
	CR
}

public static class FileTweaker {
	public static async Task RemoveUnneededPartsOfFiles(ModFilesystem ck3ModFS, string outputModPath, Configuration config) {
		// Load removable blocks from configurables.
		Dictionary<string, OrderedSet<PartOfFileToModify>> partsToModifyPerFile = new();
		
		if (config.FallenEagleEnabled) {
			Logger.Info("Reading unneeded parts of Fallen Eagle files...");
			ReadPartsOfFileToRemove(partsToModifyPerFile, "configurables/removable_file_blocks_tfe.txt", warnIfNotFound: true);
		}

		if (config.RajasOfAsiaEnabled) {
			Logger.Info("Reading unneeded parts of Rajas of Asia files...");
			ReadPartsOfFileToRemove(partsToModifyPerFile, "configurables/removable_file_blocks_roa.txt", warnIfNotFound: true);
		}
		
		bool isVanilla = config.GetCK3ModFlags()["vanilla"];
		Logger.Info("Reading unneeded parts of vanilla files...");
		ReadPartsOfFileToRemove(partsToModifyPerFile, "configurables/removable_file_blocks.txt", warnIfNotFound: isVanilla);
		
		await RemovePartsOfFiles(partsToModifyPerFile, ck3ModFS, outputModPath);
	}

	private static void ReadPartsOfFileToRemove(Dictionary<string, OrderedSet<PartOfFileToModify>> partsToModifyPerFile, string configurablePath, bool warnIfNotFound) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, fileName) => {
			ReadBlocksToModifyForFile(fileName, reader, partsToModifyPerFile, warnIfNotFound);
		});
		parser.RegisterRegex(CommonRegexes.QuotedString, (reader, fileNameInQuotes) => {
			string fileName = fileNameInQuotes.RemQuotes();
			ReadBlocksToModifyForFile(fileName, reader, partsToModifyPerFile, warnIfNotFound);
		});
		parser.IgnoreAndLogUnregisteredItems();
		
		parser.ParseFile(configurablePath);
	}

	private static void ReadBlocksToModifyForFile(string fileName, BufferedReader reader, Dictionary<string, OrderedSet<PartOfFileToModify>> partsToModifyPerFile, bool warnIfNotFound) {
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
	
	private static async Task RemovePartsOfFiles(Dictionary<string, OrderedSet<PartOfFileToModify>> partsToModifyPerFile, ModFilesystem ck3ModFS, string outputModPath) {
		// Log count of blocks to remove for each file.
		foreach (var (relativePath, partsToRemove) in partsToModifyPerFile) {
			Logger.Debug($"Loaded {partsToRemove.Count} blocks to remove from {relativePath}.");
		}

		foreach (var (relativePath, partsToRemove) in partsToModifyPerFile) {
			var inputPath = ck3ModFS.GetActualFileLocation(relativePath);
			if (!File.Exists(inputPath)) {
				Logger.Debug($"{relativePath} not found.");
				return;
			}

			LineEnding lineEndings = GetLineEndingsInFile(inputPath);
			
			var fileContent = await File.ReadAllTextAsync(inputPath);

			foreach (var (blockBefore, blockAfter, warnIfNotFound) in partsToRemove) {
				// If the file uses other line endings than CRLF, we need to modify the search string.
				string searchString;
				if (lineEndings == LineEnding.LF) {
					searchString = blockBefore.Replace("\r\n", "\n");
				} else if (lineEndings == LineEnding.CR) {
					searchString = blockBefore.Replace("\r\n", "\r");
				} else {
					searchString = blockBefore;
				}
				
				if (!fileContent.Contains(searchString)) {
					if (warnIfNotFound) {
						Logger.Warn($"Block not found in file {relativePath}: {searchString}");
					}
					continue;
				}
				
				fileContent = fileContent.Replace(searchString, blockAfter);
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

	private static LineEnding GetLineEndingsInFile(string filePath) {
		using StreamReader sr = new StreamReader(filePath);
		bool returnSeen = false;
		while (sr.Peek() >= 0) {
			char c = (char)sr.Read();
			if (c == '\n') {
				return returnSeen ? LineEnding.CRLF : LineEnding.LF;
			}
			else if (returnSeen) {
				return LineEnding.CR;
			}

			returnSeen = c == '\r';
		}

		if (returnSeen) {
			return LineEnding.CR;
		} else {
			return LineEnding.LF;
		}
	}
}