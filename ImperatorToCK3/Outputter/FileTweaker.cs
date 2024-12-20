using commonItems;
using commonItems.Mods;
using ImperatorToCK3.CommonUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImperatorToCK3.CK3.Cleanup;

public static class FileTweaker {
	public static async Task RemoveUnneededPartsOfFiles(ModFilesystem ck3ModFS, string outputModPath, Configuration config) {
		if (config.FallenEagleEnabled) {
			Logger.Info("Removing unneeded parts of Fallen Eagle files...");
			await RemovePartsOfFilesFromConfigurable("configurables/removable_file_blocks_tfe.txt", ck3ModFS, outputModPath);
		} else if (!config.WhenTheWorldStoppedMakingSenseEnabled) { // vanilla
			Logger.Info("Removing unneeded parts of vanilla files...");
			await RemovePartsOfFilesFromConfigurable("configurables/removable_file_blocks.txt", ck3ModFS, outputModPath);
		}
	}
	
	private static async Task RemovePartsOfFilesFromConfigurable(string configurablePath, ModFilesystem ck3ModFS, string outputModPath) {
		// Load removable blocks from configurables.
		Dictionary<string, string[]> partsToRemovePerFile = [];
		var parser = new Parser();
		parser.RegisterRegex(CommonRegexes.String, (reader, fileName) => {
			var blocksToRemove = new BlobList(reader).Blobs.Select(b => b.Trim()).ToArray();
			partsToRemovePerFile[fileName] = blocksToRemove;
		});
		parser.RegisterRegex(CommonRegexes.QuotedString, (reader, fileNameInQuotes) => {
			var blocksToRemove = new BlobList(reader).Blobs.Select(b => b.Trim()).ToArray();
			partsToRemovePerFile[fileNameInQuotes.RemQuotes()] = blocksToRemove;
		});
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseFile(configurablePath);
		
		// Log count of blocks to remove for each file.
		foreach (var (relativePath, partsToRemove) in partsToRemovePerFile) {
			Logger.Debug($"Loaded {partsToRemove.Length} blocks to remove from {relativePath}.");
		}

		foreach (var (relativePath, partsToRemove) in partsToRemovePerFile) {
			var inputPath = ck3ModFS.GetActualFileLocation(relativePath);
			if (!File.Exists(inputPath)) {
				Logger.Debug($"{relativePath} not found.");
				return;
			}

			string lineEndings = GetLineEndingsInFile(inputPath);
			
			var fileContent = await File.ReadAllTextAsync(inputPath);

			foreach (var block in partsToRemove) {
				// If the file uses other line endings than CRLF, we need to modify the search string.
				string searchString;
				if (lineEndings == "LF") {
					searchString = block.Replace("\r\n", "\n");
				} else if (lineEndings == "CR") {
					searchString = block.Replace("\r\n", "\r");
				} else {
					searchString = block;
				}
				
				// Log if the block is not found.
				if (!fileContent.Contains(searchString)) {
					Logger.Warn($"Block not found in file {relativePath}: {searchString}");
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

	private static string GetLineEndingsInFile(string filePath) {
		using StreamReader sr = new StreamReader(filePath);
		bool returnSeen = false;
		while (sr.Peek() >= 0) {
			char c = (char)sr.Read();
			if (c == '\n') {
				return returnSeen ? "CRLF" : "LF";
			}
			else if (returnSeen) {
				return "CR";
			}

			returnSeen = c == '\r';
		}

		if (returnSeen) {
			return "CR";
		} else {
			return "LF";
		}
	}
}