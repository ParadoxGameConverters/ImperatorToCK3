using commonItems.Collections;
using commonItems.Mods;
using ImperatorToCK3.Outputter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
			Assert.Equal(LineEnding.CRLF, lineEnding);
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
			Assert.Equal(LineEnding.LF, lineEnding);
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
			Assert.Equal(LineEnding.CR, lineEnding);
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
			var output = await File.ReadAllTextAsync(outputFilePath);
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
			var output = await File.ReadAllTextAsync(outputFilePath);
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
			var output = await File.ReadAllTextAsync(outputFilePath);
			Assert.Equal("AA\r\nDD\r\n", output);
		} finally {
			TryDeleteDir(tempDir);
		}
	}

	private static LineEnding InvokeGetLineEndingsInFile(string filePath) {
		var method = typeof(FileTweaker).GetMethod(
			"GetLineEndingsInFile",
			BindingFlags.NonPublic | BindingFlags.Static
		);
		Assert.NotNull(method);

		var result = method.Invoke(null, [filePath]);
		Assert.NotNull(result);
		return (LineEnding)result!;
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
