using ImperatorToCK3.CommonUtils;
using System;
using System.IO;
using System.Security.Principal;
using Xunit;
using static ImperatorToCK3.CommonUtils.InstallationDiagnostics;

namespace ImperatorToCK3.UnitTests.CommonUtils;

public class InstallationDiagnosticsTests {
	[Fact]
	public void DescribeHResult_NamesAccessDenied() {
		// 0x80070005 is the HRESULT behind the UnauthorizedAccessException of the bug report.
		var description = DescribeHResult(unchecked((int)0x80070005));

		Assert.Contains("ERROR_ACCESS_DENIED", description);
		Assert.Contains("Win32 code 5", description);
		Assert.Contains("0x80070005", description);
	}

	[Theory]
	[InlineData(0x80070020, "ERROR_SHARING_VIOLATION")]
	[InlineData(0x80070021, "ERROR_LOCK_VIOLATION")]
	[InlineData(0x800704EC, "ERROR_ACCESS_DISABLED_BY_POLICY")]
	[InlineData(0x80070534, "ERROR_NO_MAPPING_USER_ACCOUNT")]
	public void DescribeHResult_NamesTheErrorCodeBehindTheFailure(uint hResult, string expectedName) {
		Assert.Contains(expectedName, DescribeHResult(unchecked((int)hResult)));
	}

	[Fact]
	public void DescribeHResult_ReportsUnrecognizedErrors() {
		var description = DescribeHResult(unchecked((int)0x80070000));

		Assert.Contains("unrecognized error", description);
		Assert.Contains("Win32 code 0", description);
	}

	[Fact]
	public void DescribeNonAsciiChars_ReportsNothingForAnAsciiPath() {
		Assert.Equal("none (pure ASCII path)",
			DescribeNonAsciiChars(@"S:\Games\ImperatorToCK3\configurables\version.txt"));
	}

	[Fact]
	public void DescribeNonAsciiChars_ReportsCodePointsOfNonAsciiCharacters() {
		// The Cyrillic folder name from the bug report, spelled out as code points so that the test
		// itself cannot be broken by a file encoding change. The point of the dump is that it survives
		// the encoding mismatch that garbles the path in the log.
		var path = "S:\\Games\\\u041A\u043E\u043D\u0432\u0435\u0440\u0442\u0435\u0440\u044B " +
			"\u041F\u0430\u0440\u0430\u0445\u043E\u0434\u043E\u0432\\ImperatorToCK3\\configurables\\version.txt";

		var description = DescribeNonAsciiChars(path);

		Assert.Contains("U+041A", description); // Cyrillic capital Ka, first letter of the folder name
		Assert.Contains("UppercaseLetter", description);
		Assert.Contains("U+0440", description); // Cyrillic small er
		Assert.Contains("LowercaseLetter", description);
	}

	[Fact]
	public void DescribeNonAsciiChars_ReportsEachDistinctCharacterOnce() {
		var path = "C:\\\u0430\u0431\u0430\u0431\u0430\u0431"; // "C:\" plus three repetitions of two Cyrillic letters

		var description = DescribeNonAsciiChars(path);

		Assert.Equal(2, description.Split(", ").Length);
	}

	[Fact]
	public void DescribePathShape_ReportsLengthAndLongestComponent() {
		var description = DescribePathShape(@"S:\Games\ImperatorToCK3\configurables\version.txt");

		Assert.Contains("length 49", description);
		Assert.Contains("longest 14 characters", description);
	}

	[Fact]
	public void DescribePathShape_ExcludesTheDriveLetterFromTheComponents() {
		// A drive letter ends in a colon, which is one of the characters Windows silently strips, so
		// counting it as a component would report every ordinary path as unsafe. Only Windows has a drive
		// to exclude: on Unix the same string is one relative name, and there is no root to remove.
		var description = DescribePathShape(@"S:\Games\ImperatorToCK3\configurables\version.txt");

		if (OperatingSystem.IsWindows()) {
			Assert.Contains("4 components below the root", description);
			Assert.Contains("a drive letter", description);
		}
		Assert.DoesNotContain("end in a space or a dot", description);
	}

	[Fact]
	public void DescribePathShape_FlagsPathsLongerThanMaxPath() {
		var longPath = @"C:\" + new string('a', 300);

		Assert.Contains("EXCEEDS the 260-character MAX_PATH limit", DescribePathShape(longPath));
	}

	[Fact]
	public void DescribePathShape_FlagsComponentsWindowsSilentlyTrims() {
		var description = DescribePathShape(@"C:\Games\ImperatorToCK3 \version.txt");

		Assert.Contains("end in a space or a dot", description);
	}

	[Fact]
	public void DescribePathShape_DoesNotFlagAnOrdinaryPath() {
		var description = DescribePathShape("/usr/local/share/ImperatorToCK3/configurables/version.txt");

		Assert.DoesNotContain("end in a space or a dot", description);
		Assert.DoesNotContain("EXCEEDS", description);
	}

	[Fact]
	public void Redact_LeavesUnrelatedTextAlone() {
		Assert.Equal(@"S:\Games\ImperatorToCK3\configurables\version.txt",
			Redact(@"S:\Games\ImperatorToCK3\configurables\version.txt"));
	}

	[Fact]
	public void Redact_RemovesTheUserProfilePath() {
		var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (string.IsNullOrEmpty(profile)) {
			return;
		}

		var redacted = Redact(Path.Combine(profile, "Documents", "Paradox Interactive"));

		Assert.DoesNotContain(profile, redacted, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("<redacted>", redacted);
		Assert.Contains("Documents", redacted);
	}

	[Fact]
	public void Redact_RemovesTheAccountName() {
		var userName = Environment.UserName;
		if (string.IsNullOrEmpty(userName) || userName.Length < 3) {
			return;
		}

		var redacted = Redact($@"C:\Users\{userName}\saves\game.rome");

		Assert.DoesNotContain(userName, redacted, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("saves", redacted);
	}

	[Fact]
	public void Redact_HandlesNullAndEmptyInput() {
		Assert.Equal(string.Empty, Redact(null));
		Assert.Equal(string.Empty, Redact(string.Empty));
	}

	[Fact]
	public void DescribeIdentity_NamesBuiltInPrincipals() {
		if (!OperatingSystem.IsWindows()) {
			return;
		}

		Assert.Equal("(unresolvable principal)", DescribeIdentity(null));
		Assert.Equal("Everyone", DescribeIdentity(new SecurityIdentifier("S-1-1-0")));
		Assert.Equal("Users", DescribeIdentity(new SecurityIdentifier("S-1-5-32-545")));
	}

	[Fact]
	public void DescribeIdentity_RedactsRealAccounts() {
		if (!OperatingSystem.IsWindows()) {
			return;
		}

		// Relative identifier 1001 is a real, per-machine account, so the identity has to stay hidden
		// while the rest of the rule remains useful.
		var someAccount = new SecurityIdentifier("S-1-5-21-1111111111-2222222222-3333333333-1001");

		var description = DescribeIdentity(someAccount);

		Assert.Equal("another account (redacted)", description);
		Assert.DoesNotContain("3333333333", description);
	}

	[Fact]
	public void BuildVerdict_BlamesSecuritySoftwareWhenTheFileIsLocked() {
		var verdict = BuildVerdict(unchecked((int)0x80070020), Probes(fileExists: true, couldRead: false));

		Assert.Contains("holds the file open", verdict);
	}

	[Fact]
	public void BuildVerdict_BlamesPolicyWhenAccessIsDisabledByPolicy() {
		var verdict = BuildVerdict(unchecked((int)0x800704EC), Probes(fileExists: true, couldRead: false));

		Assert.Contains("by policy", verdict);
	}

	[Fact]
	public void BuildVerdict_BlamesFolderPermissionsWhenNeitherFileNorDirectoryIsVisible() {
		var verdict = BuildVerdict(unchecked((int)0x80070005),
			Probes(fileExists: false, couldRead: false, couldListDirectory: false, directoryAccessDenied: true));

		Assert.Contains("no permission on its own folder", verdict);
	}

	[Fact]
	public void BuildVerdict_BlamesAnIncompleteInstallWhenTheFolderIsMissing() {
		var verdict = BuildVerdict(unchecked((int)0x80070003),
			Probes(fileExists: false, couldRead: false, couldListDirectory: false, directoryMissing: true));

		Assert.Contains("missing from the installation", verdict);
		Assert.DoesNotContain("no permission on its own folder", verdict);
	}

	[Fact]
	public void BuildVerdict_BlamesDenyRulesWhenAnExistingFileCannotBeRead() {
		var verdict = BuildVerdict(unchecked((int)0x80070005),
			Probes(fileExists: true, couldRead: false, hasDenyRules: true));

		Assert.Contains("folder permission problem", verdict);
	}

	[Fact]
	public void BuildVerdict_BlamesSecuritySoftwareWhenADenialHasNoDenyRule() {
		var verdict = BuildVerdict(unchecked((int)0x80070005),
			Probes(fileExists: true, couldRead: false));

		Assert.Contains("security software", verdict);
	}

	[Fact]
	public void BuildVerdict_BlamesTheDriveWhenItIsNotReady() {
		var verdict = BuildVerdict(unchecked((int)0x80070015), Probes(fileExists: false, couldRead: false));

		Assert.Contains("not available right now", verdict);
	}

	[Fact]
	public void BuildVerdict_BlamesAnUndownloadedFileWhenItIsOffline() {
		var verdict = BuildVerdict(unchecked((int)0x80070005),
			Probes(fileExists: true, couldRead: false, isOffline: true));

		Assert.Contains("not present on the volume", verdict);
		Assert.Contains("downloaded", verdict);
	}

	[Fact]
	public void BuildVerdict_BlamesEfsEncryption() {
		var verdict = BuildVerdict(unchecked((int)0x80070005),
			Probes(fileExists: true, couldRead: false, isEncrypted: true));

		Assert.Contains("EFS-encrypted", verdict);
	}

	[Fact]
	public void BuildVerdict_BlamesAPlaceholderFile() {
		var verdict = BuildVerdict(unchecked((int)0x80070005),
			Probes(fileExists: true, couldRead: false, isReparsePoint: true));

		Assert.Contains("reparse point", verdict);
	}

	[Fact]
	public void BuildVerdict_BlamesTheNetworkDrive() {
		var verdict = BuildVerdict(unchecked((int)0x80070005),
			Probes(fileExists: true, couldRead: false, isOnNetworkDrive: true));

		Assert.Contains("network drive", verdict);
	}

	[Fact]
	public void BuildVerdict_BlamesAMissingFile() {
		var verdict = BuildVerdict(unchecked((int)0x80070005),
			Probes(fileExists: false, couldRead: false));

		Assert.Contains("does not exist", verdict);
	}

	[Fact]
	public void BuildVerdict_BlamesAReadOnlyFolderWhenNothingIsDenied() {
		var verdict = BuildVerdict(unchecked((int)0x80070005),
			Probes(fileExists: true, couldRead: true, couldWriteToDirectory: false));

		Assert.Contains("write access", verdict);
	}

	[Fact]
	public void BuildVerdict_FallsBackToSecuritySoftware() {
		var verdict = BuildVerdict(unchecked((int)0x80070005),
			Probes(fileExists: true, couldRead: true));

		Assert.Contains("no probe explains the failure", verdict);
	}

	private static AccessProbeResults Probes(bool fileExists, bool couldRead, bool couldListDirectory = true,
		bool couldWriteToDirectory = true, bool hasDenyRules = false, bool isEncrypted = false,
		bool isReparsePoint = false, bool isOffline = false, bool isOnNetworkDrive = false,
		bool directoryAccessDenied = false, bool directoryMissing = false) {
		return new AccessProbeResults(fileExists, couldRead, couldListDirectory, couldWriteToDirectory,
			hasDenyRules, isEncrypted, isReparsePoint, isOffline, isOnNetworkDrive, directoryAccessDenied,
			directoryMissing);
	}
}

/// <summary>
/// The logging entry points run every probe against the real filesystem, so they must not throw
/// whatever the state of the machine. Capturing the log output is not attempted on purpose: the
/// content is covered by the pure tests above, and the log appender may well have been bound to
/// another test's console writer.
/// </summary>
[Collection("Sequential")]
public class InstallationDiagnosticsLoggingTests {
	[Fact]
	public void LogEnvironmentContext_DoesNotThrow() {
		Record.Exception(LogEnvironmentContext);
	}

	[Fact]
	public void LogExceptionDiagnostics_DoesNotThrow() {
		Record.Exception(() =>
			LogExceptionDiagnostics(new UnauthorizedAccessException("Access to the path is denied.")));
	}

	[Fact]
	public void LogFileAccessDiagnostics_DoesNotThrowForAMissingFile() {
		var missingPath = Path.Combine("configurables", "definitely_not_there.txt");

		Record.Exception(() => LogFileAccessDiagnostics(missingPath,
			new FileNotFoundException($"Could not find file \"{missingPath}\".", missingPath)));
	}

	[Fact]
	public void LogFileAccessDiagnostics_DoesNotThrowForARealAccessDenial() {
		// Opening a directory as a file is denied by the operating system, which reproduces the shape
		// of the reported failure without having to change any file permissions.
		var directoryAsFile = Path.Combine(Path.GetTempPath(), "converter_diagnostics_probe");
		Directory.CreateDirectory(directoryAsFile);
		try {
			Exception? denial = Record.Exception(() => {
				using var stream = new FileStream(directoryAsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			});
			Assert.NotNull(denial);

			Record.Exception(() => LogFileAccessDiagnostics(directoryAsFile, denial));
		} finally {
			Directory.Delete(directoryAsFile, true);
		}
	}
}
