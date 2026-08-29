using DotLiquid;
using ImperatorToCK3.Outputter;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class WorldOutputterTests {
	[Fact]
	public void CopyBlankModFilesToOutput_SkipsShortLiquidPathWithoutThrow() {
		string tempRoot = Path.Combine(Path.GetTempPath(), "WorldOutputterTest", Guid.NewGuid().ToString("N"));
		string outputPath = Path.Combine(tempRoot, "output");
		Directory.CreateDirectory(outputPath);
		string currentBlankMod = Path.Combine(Directory.GetCurrentDirectory(), "blankMod", "output");
		bool blankModExisted = Directory.Exists(currentBlankMod);
		if (!blankModExisted) {
			Directory.CreateDirectory(currentBlankMod);
		}
		try {
			// Create a liquid file with a short name that would cause [..^7] to throw if not guarded.
			File.WriteAllText(Path.Combine(outputPath, "a.liquid"), "Hello {{ vanilla_ck3 }}");
			// Also create a deeper file.
			Directory.CreateDirectory(Path.Combine(outputPath, "common"));
			File.WriteAllText(Path.Combine(outputPath, "common", "test.txt.liquid"), "Value: {{ tfe }}");

			Hash liquidVars = Hash.FromDictionary(new Dictionary<string, object> { ["vanilla_ck3"] = true });

			Exception? ex = null;
			try {
				WorldOutputter.CopyBlankModFilesToOutput(outputPath, liquidVars);
			} catch (Exception e) {
				ex = e;
			}
			Assert.Null(ex);

			// a.txt should exist (a.liquid without extension)
			Assert.True(File.Exists(Path.Combine(outputPath, "a")));
			Assert.False(File.Exists(Path.Combine(outputPath, "a.liquid")));
			Assert.True(File.Exists(Path.Combine(outputPath, "common", "test.txt")));
		} finally {
			try { Directory.Delete(tempRoot, recursive: true); } catch { }
			if (!blankModExisted) {
				try { Directory.Delete(currentBlankMod, recursive: true); } catch { }
				try { Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "blankMod"), recursive: true); } catch { }
			}
		}
	}
}
