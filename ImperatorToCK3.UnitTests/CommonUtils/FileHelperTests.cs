using System;
using System.IO;
using Xunit;
using ImperatorToCK3.CommonUtils;
using commonItems.Mods;
using ImperatorToCK3.Imperator;
using commonItems.Exceptions;

namespace ImperatorToCK3.UnitTests.CommonUtils {
	public class FileHelperTests : IDisposable {
		private readonly string tempRoot;

		public FileHelperTests() {
			tempRoot = Path.Combine(Path.GetTempPath(), "IRToCK3Tests", Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempRoot);
		}

		public void Dispose() {
			try {
				Directory.Delete(tempRoot, recursive: true);
			} catch { /* best effort cleanup */ }
		}

		[Fact]
		public void EnsureDirectoryExists_createsMissingPath() {
			var target = Path.Combine(tempRoot, "a", "b", "c");
			Assert.False(Directory.Exists(target));

			FileHelper.EnsureDirectoryExists(target);

			Assert.True(Directory.Exists(target));
		}

		[Fact]
		public void EnsureDirectoryExists_throwsWhenFileCollision() {
			var target = Path.Combine(tempRoot, "collisionDir");
			Directory.CreateDirectory(Path.GetDirectoryName(target)!);
			File.WriteAllText(target, "oops");
			Assert.True(File.Exists(target));

			var ex = Assert.Throws<UserErrorException>(() => FileHelper.EnsureDirectoryExists(target));
			Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);

			// original file must remain untouched
			Assert.True(File.Exists(target));
			Assert.False(Directory.Exists(target));
		}

		[Fact]
		public void OutputGuiContainer_handlesFileInPlaceOfGuiDirectory() {
			// prepare a fake Imperator installation with one GUI file
			var gameRoot = Path.Combine(tempRoot, "game");
			var guiDir = Path.Combine(gameRoot, "gui");
			Directory.CreateDirectory(guiDir);
			var topbar = Path.Combine(guiDir, "ingame_topbar.gui");
			File.WriteAllText(topbar, "foo");

			var modFS = new ModFilesystem(gameRoot, []);

			// configuration points to a separate doc path; create a collision file
			var docPath = Path.Combine(tempRoot, "docs");
			var config = new Configuration {
				ImperatorDocPath = docPath
			};
			Directory.CreateDirectory(docPath);

			var collisionFile = Path.Combine(docPath, "mod", "coa_export_mod", "gui");
			Directory.CreateDirectory(Path.GetDirectoryName(collisionFile)!);
			File.WriteAllText(collisionFile, "not a directory");

			// run the helper; it should not crash but will bail out early
			World.OutputGuiContainer(modFS, [], config);

			// collision file remains and no directory has been created
			var expectedGuiDir = Path.Combine(docPath, "mod", "coa_export_mod", "gui");
			Assert.False(Directory.Exists(expectedGuiDir));
			Assert.True(File.Exists(collisionFile));
		}
	}
}
