using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3;
using ImperatorToCK3.CommonUtils;
using ImperatorToCK3.Outputter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class FileTweakerTests {
	[Fact]
	public void GetLineEndingsInFile_DetectsCRLF() {
		var tempDir = CreateTempDir();
		try {
			var filePath = Path.Combine(tempDir, "crlf.txt");
			File.WriteAllBytes(filePath, "a\r\nb\r\n"u8.ToArray());

			var lineEnding = InvokeGetLineEndingsInFile(filePath);
			Assert.Equal(FileTweaker.LineEnding.CRLF, lineEnding);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public void GetLineEndingsInFile_DetectsLF() {
		var tempDir = CreateTempDir();
		try {
			var filePath = Path.Combine(tempDir, "lf.txt");
			File.WriteAllBytes(filePath, "a\nb\n"u8.ToArray());

			var lineEnding = InvokeGetLineEndingsInFile(filePath);
			Assert.Equal(FileTweaker.LineEnding.LF, lineEnding);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public void GetLineEndingsInFile_DetectsCR() {
		var tempDir = CreateTempDir();
		try {
			var filePath = Path.Combine(tempDir, "cr.txt");
			File.WriteAllBytes(filePath, "a\rb\r"u8.ToArray());

			var lineEnding = InvokeGetLineEndingsInFile(filePath);
			Assert.Equal(FileTweaker.LineEnding.CR, lineEnding);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task ModifyPartsOfFiles_HandlesLineEndingMismatchInSearchString() {
		var tempDir = CreateTempDir();
		try {
			var inputRoot = Path.Combine(tempDir, "input");
			Directory.CreateDirectory(inputRoot);

			const string relativePath = "common/test.txt";
			var inputFilePath = Path.Combine(inputRoot, "common", "test.txt");
			Directory.CreateDirectory(Path.GetDirectoryName(inputFilePath)!);

			// Input file uses LF.
			File.WriteAllBytes(inputFilePath, "AA\nBB\nCC\n"u8.ToArray());

			// Search block uses CRLF, but should still match because FileTweaker
			// adjusts the search string to the file's line endings.
			var parts = new OrderedSet<PartOfFileToModify> {
				new PartOfFileToModify(textBefore: "BB\r\nCC\r\n", textAfter: "REPLACED\n")
			};

			var partsToModifyPerFile = new Dictionary<string, OrderedSet<PartOfFileToModify>> {
				[relativePath] = parts
			};

			var ck3ModFS = new ModFilesystem(inputRoot, Array.Empty<Mod>());

			var outputRoot = Path.Combine(tempDir, "output");
			await InvokeModifyPartsOfFiles(partsToModifyPerFile, ck3ModFS, outputRoot);

			var outputFilePath = Path.Combine(outputRoot, "common", "test.txt");
			Assert.True(File.Exists(outputFilePath));
			var output = await File.ReadAllTextAsync(outputFilePath, TestContext.Current.CancellationToken);
			Assert.Equal("AA\nREPLACED\n", output);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task ModifyPartsOfFiles_RemovesBlockDefinedAsCRLF_FromLFFile() {
		var tempDir = CreateTempDir();
		try {
			var inputRoot = Path.Combine(tempDir, "input");
			Directory.CreateDirectory(inputRoot);

			const string relativePath = "common/remove_crlf_from_lf.txt";
			var inputFilePath = Path.Combine(inputRoot, "common", "remove_crlf_from_lf.txt");
			Directory.CreateDirectory(Path.GetDirectoryName(inputFilePath)!);

			// Input file uses LF.
			File.WriteAllBytes(inputFilePath, "AA\nBB\nCC\nDD\n"u8.ToArray());

			// Removable block is defined with CRLF.
			var parts = new OrderedSet<PartOfFileToModify> {
				new PartOfFileToModify(textBefore: "BB\r\nCC\r\n", textAfter: string.Empty)
			};

			var partsToModifyPerFile = new Dictionary<string, OrderedSet<PartOfFileToModify>> {
				[relativePath] = parts
			};

			var ck3ModFS = new ModFilesystem(inputRoot, Array.Empty<Mod>());
			var outputRoot = Path.Combine(tempDir, "output");
			await InvokeModifyPartsOfFiles(partsToModifyPerFile, ck3ModFS, outputRoot);

			var outputFilePath = Path.Combine(outputRoot, "common", "remove_crlf_from_lf.txt");
			Assert.True(File.Exists(outputFilePath));
			var output = await File.ReadAllTextAsync(outputFilePath, TestContext.Current.CancellationToken);
			Assert.Equal("AA\nDD\n", output);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task ModifyPartsOfFiles_RemovesBlockDefinedAsLF_FromCRLFFile() {
		var tempDir = CreateTempDir();
		try {
			var inputRoot = Path.Combine(tempDir, "input");
			Directory.CreateDirectory(inputRoot);

			const string relativePath = "common/remove_lf_from_crlf.txt";
			var inputFilePath = Path.Combine(inputRoot, "common", "remove_lf_from_crlf.txt");
			Directory.CreateDirectory(Path.GetDirectoryName(inputFilePath)!);

			// Input file uses CRLF.
			File.WriteAllBytes(inputFilePath, "AA\r\nBB\r\nCC\r\nDD\r\n"u8.ToArray());

			// Removable block is defined with LF.
			var parts = new OrderedSet<PartOfFileToModify> {
				new PartOfFileToModify(textBefore: "BB\nCC\n", textAfter: string.Empty)
			};

			var partsToModifyPerFile = new Dictionary<string, OrderedSet<PartOfFileToModify>> {
				[relativePath] = parts
			};

			var ck3ModFS = new ModFilesystem(inputRoot, Array.Empty<Mod>());
			var outputRoot = Path.Combine(tempDir, "output");
			await InvokeModifyPartsOfFiles(partsToModifyPerFile, ck3ModFS, outputRoot);

			var outputFilePath = Path.Combine(outputRoot, "common", "remove_lf_from_crlf.txt");
			Assert.True(File.Exists(outputFilePath));
			var output = await File.ReadAllTextAsync(outputFilePath, TestContext.Current.CancellationToken);
			Assert.Equal("AA\r\nDD\r\n", output);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	private static FileTweaker.LineEnding InvokeGetLineEndingsInFile(string filePath) {
		var method = typeof(FileTweaker).GetMethod(
			"GetLineEndingsInFile",
			BindingFlags.NonPublic | BindingFlags.Static
		);
		Assert.NotNull(method);

		var result = method.Invoke(null, [filePath]);
		Assert.NotNull(result);
		return (FileTweaker.LineEnding)result!;
	}

	private static async Task InvokeModifyPartsOfFiles(
		Dictionary<string, OrderedSet<PartOfFileToModify>> partsToModifyPerFile,
		ModFilesystem ck3ModFS,
		string outputModPath
	) {
		var method = typeof(FileTweaker).GetMethod(
			"ModifyPartsOfFiles",
			BindingFlags.NonPublic | BindingFlags.Static
		);
		Assert.NotNull(method);

		var taskObj = method.Invoke(null, [partsToModifyPerFile, ck3ModFS, outputModPath]);
		Assert.NotNull(taskObj);
		await (Task)taskObj!;
	}

	[Fact]
	public async Task ModifyAndRemovePartsOfFiles_RemovesBlockFromVanillaFile() {
		var tempDir = CreateTempDir();
		var createdConfigs = new List<string>();
		try {
			var inputRoot = Path.Combine(tempDir, "input");
			const string relativePath = "common/tweak/vanilla_remove.txt";
			var inputFile = Path.Combine(inputRoot, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(inputFile)!);
			File.WriteAllText(inputFile, "foo = 1\nblock = {\ninner = yes\n}\nbaz = 3\n");

			WriteConfig(createdConfigs, "removable_file_blocks.txt", """
"common/tweak/vanilla_remove.txt" = {
{
block = {
inner = yes
}
}
}
"""
			);

			var ck3ModFS = new ModFilesystem(inputRoot, Array.Empty<Mod>());
			var outputRoot = Path.Combine(tempDir, "output");
			await FileTweaker.ModifyAndRemovePartsOfFiles(ck3ModFS, outputRoot, MakeConfig());

			var outputFile = Path.Combine(outputRoot, relativePath);
			Assert.True(File.Exists(outputFile));
			var output = await File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);
			Assert.Equal("foo = 1\n\nbaz = 3\n", output);
		} finally {
			DeleteConfigs(createdConfigs);
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task ModifyAndRemovePartsOfFiles_ReplacesBlockInVanillaFile() {
		var tempDir = CreateTempDir();
		var createdConfigs = new List<string>();
		try {
			var inputRoot = Path.Combine(tempDir, "input");
			const string relativePath = "common/tweak/vanilla_replace.txt";
			var inputFile = Path.Combine(inputRoot, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(inputFile)!);
			File.WriteAllText(inputFile, "foo = 1\nblock = {\ninner = yes\n}\nbaz = 3\n");

			WriteConfig(createdConfigs, "replaceable_file_blocks.txt", """
"common/tweak/vanilla_replace.txt" = {
replace = {
before = {
block = {
inner = yes
}
}
after = {
block = {
inner = no
}
}
}
}
"""
			);

			var ck3ModFS = new ModFilesystem(inputRoot, Array.Empty<Mod>());
			var outputRoot = Path.Combine(tempDir, "output");
			await FileTweaker.ModifyAndRemovePartsOfFiles(ck3ModFS, outputRoot, MakeConfig());

			var outputFile = Path.Combine(outputRoot, relativePath);
			Assert.True(File.Exists(outputFile));
			var output = await File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);
			Assert.Equal("foo = 1\nblock = {\ninner = no\n}\nbaz = 3\n", output);
		} finally {
			DeleteConfigs(createdConfigs);
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task ModifyAndRemovePartsOfFiles_RemovesBlockForActiveModFlag() {
		var tempDir = CreateTempDir();
		var createdConfigs = new List<string>();
		try {
			var inputRoot = Path.Combine(tempDir, "input");
			const string relativePath = "common/tweak/flag_remove.txt";
			var inputFile = Path.Combine(inputRoot, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(inputFile)!);
			File.WriteAllText(inputFile, "foo = 1\nblock = {\ninner = yes\n}\nbaz = 3\n");

			// Vanilla configurables exist but reference nothing for our file.
			WriteConfig(createdConfigs, "removable_file_blocks.txt", "");
			WriteConfig(createdConfigs, "replaceable_file_blocks.txt", "");
			WriteConfig(createdConfigs, "removable_file_blocks_tfe.txt", """
"common/tweak/flag_remove.txt" = {
{
block = {
inner = yes
}
}
}
"""
			);

			var ck3ModFS = new ModFilesystem(inputRoot, Array.Empty<Mod>());
			var outputRoot = Path.Combine(tempDir, "output");
			await FileTweaker.ModifyAndRemovePartsOfFiles(ck3ModFS, outputRoot, MakeConfig("tfe"));

			var outputFile = Path.Combine(outputRoot, relativePath);
			Assert.True(File.Exists(outputFile));
			var output = await File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);
			Assert.Equal("foo = 1\n\nbaz = 3\n", output);
		} finally {
			DeleteConfigs(createdConfigs);
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task ModifyAndRemovePartsOfFiles_ReplacesBlockForActiveModFlag() {
		var tempDir = CreateTempDir();
		var createdConfigs = new List<string>();
		try {
			var inputRoot = Path.Combine(tempDir, "input");
			const string relativePath = "common/tweak/flag_replace.txt";
			var inputFile = Path.Combine(inputRoot, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(inputFile)!);
			File.WriteAllText(inputFile, "foo = 1\nblock = {\ninner = yes\n}\nbaz = 3\n");

			WriteConfig(createdConfigs, "removable_file_blocks.txt", "");
			WriteConfig(createdConfigs, "replaceable_file_blocks.txt", "");
			WriteConfig(createdConfigs, "replaceable_file_blocks_tfe.txt", """
"common/tweak/flag_replace.txt" = {
replace = {
before = {
block = {
inner = yes
}
}
after = {
block = {
inner = no
}
}
}
}
"""
			);

			var ck3ModFS = new ModFilesystem(inputRoot, Array.Empty<Mod>());
			var outputRoot = Path.Combine(tempDir, "output");
			await FileTweaker.ModifyAndRemovePartsOfFiles(ck3ModFS, outputRoot, MakeConfig("tfe"));

			var outputFile = Path.Combine(outputRoot, relativePath);
			Assert.True(File.Exists(outputFile));
			var output = await File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);
			Assert.Equal("foo = 1\nblock = {\ninner = no\n}\nbaz = 3\n", output);
		} finally {
			DeleteConfigs(createdConfigs);
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task ModifyAndRemovePartsOfFiles_SkipsMissingModFlagConfigurableWithoutError() {
		var tempDir = CreateTempDir();
		var createdConfigs = new List<string>();
		try {
			var inputRoot = Path.Combine(tempDir, "input");
			const string relativePath = "common/tweak/skip_flag.txt";
			var inputFile = Path.Combine(inputRoot, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(inputFile)!);
			File.WriteAllText(inputFile, "foo = 1\nbar = 2\nbaz = 3\n");

			// Active flag but its configurable file is not present - should be skipped without error.
			// The vanilla config still references the file, so the file is processed and output is written.
			WriteConfig(createdConfigs, "removable_file_blocks.txt", """
"common/tweak/skip_flag.txt" = {
{
bar = 2
}
}
"""
			);
			WriteConfig(createdConfigs, "replaceable_file_blocks.txt", "");

			var ck3ModFS = new ModFilesystem(inputRoot, Array.Empty<Mod>());
			var outputRoot = Path.Combine(tempDir, "output");
			await FileTweaker.ModifyAndRemovePartsOfFiles(ck3ModFS, outputRoot, MakeConfig("tfe"));

			var outputFile = Path.Combine(outputRoot, relativePath);
			Assert.True(File.Exists(outputFile));
			var output = await File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);
			Assert.Equal("foo = 1\n\nbaz = 3\n", output);
		} finally {
			DeleteConfigs(createdConfigs);
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task ModifyAndRemovePartsOfFiles_DoesNotWriteOutputWhenSourceFileMissing() {
		var tempDir = CreateTempDir();
		var createdConfigs = new List<string>();
		try {
			var inputRoot = Path.Combine(tempDir, "input");
			Directory.CreateDirectory(inputRoot);

			// Reference a file that does not exist in the mod filesystem.
			WriteConfig(createdConfigs, "removable_file_blocks.txt", """
"common/tweak/does_not_exist.txt" = {
{
block = {
inner = yes
}
}
}
"""
			);

			var ck3ModFS = new ModFilesystem(inputRoot, Array.Empty<Mod>());
			var outputRoot = Path.Combine(tempDir, "output");
			await FileTweaker.ModifyAndRemovePartsOfFiles(ck3ModFS, outputRoot, MakeConfig());

			var outputFile = Path.Combine(outputRoot, "common/tweak/does_not_exist.txt");
			Assert.False(File.Exists(outputFile));
		} finally {
			DeleteConfigs(createdConfigs);
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task ModifyAndRemovePartsOfFiles_LeavesFileUnchangedWhenBlockNotFound() {
		var tempDir = CreateTempDir();
		var createdConfigs = new List<string>();
		try {
			var inputRoot = Path.Combine(tempDir, "input");
			const string relativePath = "common/tweak/not_found.txt";
			var inputFile = Path.Combine(inputRoot, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(inputFile)!);
			File.WriteAllText(inputFile, "foo = 1\nbar = 2\nbaz = 3\n");

			WriteConfig(createdConfigs, "removable_file_blocks.txt", """
"common/tweak/not_found.txt" = {
{
block = {
inner = yes
}
}
}
"""
			);

			var ck3ModFS = new ModFilesystem(inputRoot, Array.Empty<Mod>());
			var outputRoot = Path.Combine(tempDir, "output");
			await FileTweaker.ModifyAndRemovePartsOfFiles(ck3ModFS, outputRoot, MakeConfig());

			var outputFile = Path.Combine(outputRoot, relativePath);
			Assert.True(File.Exists(outputFile));
			var output = await File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);
			Assert.Equal("foo = 1\nbar = 2\nbaz = 3\n", output);
		} finally {
			DeleteConfigs(createdConfigs);
			TryDeleteDir(tempDir);
		}
	}

	[Fact]
	public async Task ModifyAndRemovePartsOfFiles_AppliesBothVanillaAndFlagBlocks() {
		var tempDir = CreateTempDir();
		var createdConfigs = new List<string>();
		try {
			var inputRoot = Path.Combine(tempDir, "input");
			const string relativePath = "common/tweak/combined.txt";
			var inputFile = Path.Combine(inputRoot, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(inputFile)!);
			File.WriteAllText(inputFile, "foo = 1\nvanilla_block = {\ninner = a\n}\nflag_block = {\ninner = b\n}\nbaz = 3\n");

			WriteConfig(createdConfigs, "removable_file_blocks.txt", """
"common/tweak/combined.txt" = {
{
vanilla_block = {
inner = a
}
}
}
"""
			);
			WriteConfig(createdConfigs, "removable_file_blocks_tfe.txt", """
"common/tweak/combined.txt" = {
{
flag_block = {
inner = b
}
}
}
"""
			);

			var ck3ModFS = new ModFilesystem(inputRoot, Array.Empty<Mod>());
			var outputRoot = Path.Combine(tempDir, "output");
			await FileTweaker.ModifyAndRemovePartsOfFiles(ck3ModFS, outputRoot, MakeConfig("tfe"));

			var outputFile = Path.Combine(outputRoot, relativePath);
			Assert.True(File.Exists(outputFile));
			var output = await File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);
			Assert.Equal("foo = 1\n\n\nbaz = 3\n", output);
			Assert.DoesNotContain("inner = a", output);
			Assert.DoesNotContain("inner = b", output);
		} finally {
			DeleteConfigs(createdConfigs);
			TryDeleteDir(tempDir);
		}
	}

	private static Configuration MakeConfig(params string[] activeFlags) {
		var config = new Configuration();
		var defs = activeFlags.Select(f => new ModDefinition(f, new List<Regex>(), new List<string>())).ToList();
		typeof(Configuration).GetField("ck3ModDefinitions", BindingFlags.NonPublic | BindingFlags.Instance)!
			.SetValue(config, defs);
		typeof(Configuration).GetField("activeCK3ModFlags", BindingFlags.NonPublic | BindingFlags.Instance)!
			.SetValue(config, new HashSet<string>(activeFlags));
		return config;
	}

	private static string ConfigPath(string name) {
		return Path.Combine(Directory.GetCurrentDirectory(), "configurables", name);
	}

	private static void WriteConfig(List<string> created, string name, string content) {
		var dir = Path.GetDirectoryName(ConfigPath(name))!;
		Directory.CreateDirectory(dir);
		File.WriteAllText(ConfigPath(name), content);
		created.Add(name);
	}

	private static void DeleteConfigs(List<string> created) {
		foreach (var name in created) {
			try {
				var path = ConfigPath(name);
				if (File.Exists(path)) {
					File.Delete(path);
				}
			} catch {
				// Best-effort cleanup only.
			}
		}
	}

	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "FileTweaker", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		return dir;
	}

	private static void TryDeleteDir(string dir) {
		try {
			if (Directory.Exists(dir)) {
				Directory.Delete(dir, recursive: true);
			}
		} catch {
			// Best-effort cleanup only.
		}
	}
}
