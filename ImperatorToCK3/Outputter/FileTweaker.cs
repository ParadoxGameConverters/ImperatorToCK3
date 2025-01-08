using commonItems;
using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImperatorToCK3.Outputter;

internal readonly struct PartOfFileToRemove(string text, bool warnIfNotFound = true) {
	internal readonly string Text = text;
	internal readonly bool WarnIfNotFound = warnIfNotFound;

	public void Deconstruct(out string text, out bool warnIfNotFound) {
		text = Text;
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
		Dictionary<string, OrderedSet<PartOfFileToRemove>> partsToRemovePerFile = new();
		
		if (config.FallenEagleEnabled) {
			Logger.Info("Reading unneeded parts of Fallen Eagle files...");
			ReadPartsOfFileToRemove(partsToRemovePerFile, "configurables/removable_file_blocks_tfe.txt", warnIfNotFound: true);
		}

		if (config.RajasOfAsiaEnabled) {
			Logger.Info("Reading unneeded parts of Rajas of Asia files...");
			ReadPartsOfFileToRemove(partsToRemovePerFile, "configurables/removable_file_blocks_roa.txt", warnIfNotFound: true);
		}
		
		bool isVanilla = config.GetCK3ModFlags()["vanilla"];
		Logger.Info("Reading unneeded parts of vanilla files...");
		ReadPartsOfFileToRemove(partsToRemovePerFile, "configurables/removable_file_blocks.txt", warnIfNotFound: isVanilla);
		
		await RemovePartsOfFiles(partsToRemovePerFile, ck3ModFS, outputModPath);
	}

	private static void ReadPartsOfFileToRemove(Dictionary<string, OrderedSet<PartOfFileToRemove>> partsToRemovePerFile, string configurablePath, bool warnIfNotFound) {
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, fileName) => {
			var blocksToRemove = new BlobList(reader).Blobs.Select(b => b.Trim()).ToArray();
			
			if (partsToRemovePerFile.TryGetValue(fileName, out var existingBlocksToRemove)) {
				var alreadyExistingBlocks = existingBlocksToRemove.Select(b => b.Text);
				var newBlocksToRemove = blocksToRemove
					.Except(alreadyExistingBlocks)
					.Select(b => new PartOfFileToRemove(text: b, warnIfNotFound: warnIfNotFound));
				existingBlocksToRemove.UnionWith(newBlocksToRemove);
			} else {
				partsToRemovePerFile[fileName] = blocksToRemove
					.Select(b => new PartOfFileToRemove(text: b, warnIfNotFound: warnIfNotFound))
					.ToOrderedSet();
			}
		});
		parser.RegisterRegex(CommonRegexes.QuotedString, (reader, fileNameInQuotes) => {
			var blocksToRemove = new BlobList(reader).Blobs.Select(b => b.Trim()).ToArray();
			
			var fileName = fileNameInQuotes.RemQuotes();
			if (partsToRemovePerFile.TryGetValue(fileName, out var existingBlocksToRemove)) {
				var alreadyExistingBlocks = existingBlocksToRemove.Select(b => b.Text);
				var blocksToAdd = blocksToRemove
					.Except(alreadyExistingBlocks)
					.Select(b => new PartOfFileToRemove(text: b, warnIfNotFound: warnIfNotFound));
				existingBlocksToRemove.UnionWith(blocksToAdd);
			} else {
				partsToRemovePerFile[fileName] = blocksToRemove
					.Select(b => new PartOfFileToRemove(text: b, warnIfNotFound: warnIfNotFound))
					.ToOrderedSet();
			}
		});
		parser.IgnoreAndLogUnregisteredItems();
		
		parser.ParseFile(configurablePath);
	}
	
	private static async Task RemovePartsOfFiles(Dictionary<string, OrderedSet<PartOfFileToRemove>> partsToRemovePerFile, ModFilesystem ck3ModFS, string outputModPath) {
		// Log count of blocks to remove for each file.
		foreach (var (relativePath, partsToRemove) in partsToRemovePerFile) {
			Logger.Debug($"Loaded {partsToRemove.Count} blocks to remove from {relativePath}.");
		}

		foreach (var (relativePath, partsToRemove) in partsToRemovePerFile) {
			var inputPath = ck3ModFS.GetActualFileLocation(relativePath);
			if (!File.Exists(inputPath)) {
				Logger.Debug($"{relativePath} not found.");
				return;
			}

			LineEnding lineEndings = GetLineEndingsInFile(inputPath);
			
			var fileContent = await File.ReadAllTextAsync(inputPath);

			foreach (var (block, warnIfNotFound) in partsToRemove) {
				// If the file uses other line endings than CRLF, we need to modify the search string.
				string searchString;
				if (lineEndings == LineEnding.LF) {
					searchString = block.Replace("\r\n", "\n");
				} else if (lineEndings == LineEnding.CR) {
					searchString = block.Replace("\r\n", "\r");
				} else {
					searchString = block;
				}
				
				if (!fileContent.Contains(searchString)) {
					if (warnIfNotFound) {
						Logger.Warn($"Block not found in file {relativePath}: {searchString}");
					}
					continue;
				}
				
				fileContent = fileContent.Replace(searchString, "");
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