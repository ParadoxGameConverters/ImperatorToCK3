using commonItems.Mods;
using ImageMagick;
using ImperatorToCK3.Outputter;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ImperatorToCK3.UnitTests.Outputter;

public class CoatOfArmsEmblemsOutputterTests {
	[Fact]
	public async Task CopyEmblemsConvertsColoredCopiesTexturedAndIgnoresUnsupportedExtensions() {
		var tempRoot = CreateTempDir();
		try {
			var modRoot = Path.Combine(tempRoot, "imperator");
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			CreateEmblemDirectories(modRoot);
			CreateEmblemDirectories(outputModPath);

			var coloredInputPath = Path.Combine(modRoot, "gfx", "coat_of_arms", "colored_emblems", "colored_test.png");
			var texturedInputPath = Path.Combine(modRoot, "gfx", "coat_of_arms", "textured_emblems", "textured_test.png");
			CreateSolidPng(coloredInputPath, new MagickColor(0, 20, 30));
			CreateSolidPng(texturedInputPath, new MagickColor(40, 50, 60));
			await File.WriteAllTextAsync(Path.Combine(modRoot, "gfx", "coat_of_arms", "colored_emblems", "ignored.jpg"), "ignore", TestContext.Current.CancellationToken);
			await File.WriteAllTextAsync(Path.Combine(modRoot, "gfx", "coat_of_arms", "textured_emblems", "ignored.bmp"), "ignore", TestContext.Current.CancellationToken);

			await CoatOfArmsEmblemsOutputter.CopyEmblems(outputModPath, new ModFilesystem(modRoot, Array.Empty<Mod>()));

			var coloredOutputPath = Path.Combine(outputModPath, "gfx", "coat_of_arms", "colored_emblems", "colored_test.png");
			Assert.True(File.Exists(coloredOutputPath));
			using (var inputImage = new MagickImage(coloredInputPath))
			using (var outputImage = new MagickImage(coloredOutputPath)) {
				var inputPixel = inputImage.GetPixels().GetPixel(0, 0).ToColor()!;
				var outputPixel = outputImage.GetPixels().GetPixel(0, 0).ToColor()!;
				Assert.True(outputPixel.R > inputPixel.R);
				Assert.Equal(inputPixel.G, outputPixel.G);
				Assert.Equal(inputPixel.B, outputPixel.B);
			}

			var texturedOutputPath = Path.Combine(outputModPath, "gfx", "coat_of_arms", "textured_emblems", "textured_test.png");
			Assert.True(File.Exists(texturedOutputPath));
			Assert.Equal(await File.ReadAllBytesAsync(texturedInputPath, TestContext.Current.CancellationToken), await File.ReadAllBytesAsync(texturedOutputPath, TestContext.Current.CancellationToken));

			Assert.False(File.Exists(Path.Combine(outputModPath, "gfx", "coat_of_arms", "colored_emblems", "ignored.jpg")));
			Assert.False(File.Exists(Path.Combine(outputModPath, "gfx", "coat_of_arms", "textured_emblems", "ignored.bmp")));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	[Fact]
	public async Task MalformedColoredEmblemsAreSkipped() {
		var tempRoot = CreateTempDir();
		try {
			var modRoot = Path.Combine(tempRoot, "imperator");
			var outputModPath = Path.Combine(tempRoot, "outputMod");
			CreateEmblemDirectories(modRoot);
			CreateEmblemDirectories(outputModPath);

			var invalidColoredInputPath = Path.Combine(modRoot, "gfx", "coat_of_arms", "colored_emblems", "broken.png");
			await File.WriteAllBytesAsync(invalidColoredInputPath, [1, 2, 3, 4, 5], TestContext.Current.CancellationToken);

			await CoatOfArmsEmblemsOutputter.CopyEmblems(outputModPath, new ModFilesystem(modRoot, Array.Empty<Mod>()));

			Assert.False(File.Exists(Path.Combine(outputModPath, "gfx", "coat_of_arms", "colored_emblems", "broken.png")));
		} finally {
			TryDeleteDir(tempRoot);
		}
	}

	private static void CreateSolidPng(string path, MagickColor color) {
		using var image = new MagickImage(color, 1, 1);
		image.Write(path);
	}

	private static void CreateEmblemDirectories(string rootPath) {
		Directory.CreateDirectory(Path.Combine(rootPath, "gfx", "coat_of_arms", "colored_emblems"));
		Directory.CreateDirectory(Path.Combine(rootPath, "gfx", "coat_of_arms", "textured_emblems"));
	}

	private static string CreateTempDir() {
		var dir = Path.Combine(Path.GetTempPath(), "ImperatorToCK3_UnitTests", "CoatOfArmsEmblemsOutputter", Guid.NewGuid().ToString("N"));
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