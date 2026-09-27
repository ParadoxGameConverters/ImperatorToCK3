namespace ImperatorToCK3.CommonUtils;

using commonItems;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

/// <summary>
/// Diagnostics for failures to read the converter's own installation files, such as
/// "configurables/version.txt" or "configuration.txt".
///
/// A denial on one of those files is always an environment problem (folder permissions, security
/// software, a network drive, a cloud file that has not been downloaded), never a conversion bug,
/// and the exception message on its own never says which. The path is also the single most
/// interesting piece of evidence, and it is exactly the part that tends to reach us mangled by a
/// text encoding mismatch. So all of it is dumped into the log: the native error code, the shape of
/// the path, the code points of its non-ASCII characters, the volume, the file metadata, a matrix of
/// access probes, the folder's access rules, and a verdict on the most likely cause.
///
/// Nothing that identifies the user (account name, SID, machine name, elevation, network share
/// target, volume label) is ever written to the log, because these diagnostics are meant to be
/// pasted into public bug reports. Everything logged goes through <see cref="Redact"/> first.
/// </summary>
internal static class InstallationDiagnostics {
	private const int MaxPathLength = 260;
	private const int MaxLoggedAccessRules = 20;
	private const string RedactedPlaceholder = "<redacted>";

	// Substrings that must never reach the log. Ordered longest-first by BuildSensitiveFragments()
	// so that "C:\Users\nicol" is replaced before the bare account name "nicol".
	private static readonly string[] sensitiveFragments = BuildSensitiveFragments();

	// Marks an exception as already described, so that the fallback in Program.Main does not log a
	// second, shorter block for an exception that a more specific caller has already described.
	private const string LoggedMarker = "ImperatorToCK3.InstallationDiagnosticsLogged";

	/// <summary>
	/// Where the converter thinks it is. The data files are loaded relative to the working directory
	/// rather than to the executable, so a mismatch between the two is worth knowing about before
	/// anything else goes wrong.
	/// </summary>
	internal static void LogEnvironmentContext() {
		var workingDirectory = SafeDirectoryPath();
		var baseDirectory = SafeEnv(() => AppContext.BaseDirectory);

		Logger.Debug("=== Converter environment ===");
		Logger.Debug($"  executable: {Redact(SafeEnv(() => Environment.ProcessPath))}");
		Logger.Debug($"  base directory: {Redact(baseDirectory)}");
		Logger.Debug($"  working directory: {Redact(workingDirectory)}");
		Logger.Debug($"  entry assembly: {Redact(SafeEnv(() => Assembly.GetEntryAssembly()?.Location))}");
		Logger.Debug($"  command line: {Redact(SafeEnv(() => Environment.CommandLine))}");
		Logger.Debug($"  OS: {Redact(SafeEnv(() => RuntimeInformation.OSDescription))}, " +
		             $"64-bit process: {Environment.Is64BitProcess}");

		if (!SameDirectory(workingDirectory, baseDirectory)) {
			Logger.Warn("The converter's working directory differs from the directory containing its " +
			            "executable. The converter loads its data files (configurables, blankMod) relative " +
			            "to the working directory, so it will not be able to find them.");
		}

		Logger.Debug("  The account name, SID, machine name and elevation state are deliberately not " +
		             "logged, to keep bug reports free of personal data.");
		Logger.Debug("=== End of converter environment ===");
	}

	/// <summary>
	/// Logs what can be learned about a failure without knowing which file it concerns. Used for any
	/// I/O exception that escapes the conversion.
	/// </summary>
	internal static void LogExceptionDiagnostics(Exception ex) {
		Logger.Debug($"=== Diagnostics for {ex.GetType().Name} ===");
		Logger.Debug($"  message: {Redact(ex.Message)}");
		Logger.Debug($"  HRESULT 0x{ex.HResult.ToString("X8", CultureInfo.InvariantCulture)} -> {DescribeHResult(ex.HResult)}");
		if (ex.StackTrace is not null) {
			Logger.Debug($"  thrown at: {Redact(FirstStackFrameWithSource(ex.StackTrace))}");
		}
		Logger.Debug("=== End of diagnostics ===");
		MarkAsLogged(ex);
	}

	/// <summary>Whether these diagnostics have already described this exception.</summary>
	internal static bool HasBeenLogged(Exception ex) => ex.Data.Contains(LoggedMarker);

	private static void MarkAsLogged(Exception ex) {
		try {
			ex.Data[LoggedMarker] = true;
		} catch (Exception markerException) {
			// Not worth failing over. The only consequence is a repeated diagnostics block.
			Logger.Debug($"  (could not mark the exception as described: {markerException.GetType().Name})");
		}
	}

	/// <summary>
	/// Logs everything known about a file the converter failed to open, and a verdict on the most
	/// likely cause. Must not throw: diagnostics are useless if they fail the same way the conversion
	/// did.
	/// </summary>
	internal static void LogFileAccessDiagnostics(string path, Exception ex) {
		LogExceptionDiagnostics(ex);

		string fullPath;
		try {
			fullPath = Path.GetFullPath(path);
		} catch (Exception resolveException) {
			Logger.Debug($"  path could not be resolved: {Redact(resolveException.Message)}");
			return;
		}

		Logger.Debug("--- File access diagnostics ---");
		Logger.Debug($"  path: {Redact(fullPath)}");
		Logger.Debug($"  path shape: {DescribePathShape(fullPath)}");
		Logger.Debug($"  non-ASCII characters in path: {DescribeNonAsciiChars(fullPath)}");

		var fileExists = File.Exists(fullPath);
		Logger.Debug($"  File.Exists: {fileExists}");

		LogDriveDiagnostics(fullPath);
		LogFileMetadata(fullPath, fileExists);

		var access = RunAccessProbes(fullPath, fileExists);
		Logger.Debug("  access rules on the file: " +
		             $"{DescribeAccessRules(access.FileRules, Path.GetFileName(fullPath) ?? fullPath)}");
		if (Path.GetDirectoryName(fullPath) is string directory) {
			Logger.Debug("  access rules on the containing directory: " +
			             $"{DescribeAccessRules(access.DirectoryRules, Path.GetFileName(directory) ?? directory)}");
		}

		Logger.Debug($"  could read the file: {access.Probes.CouldRead}, could list the directory: " +
		             $"{access.Probes.CouldListDirectory}, could write to the directory: " +
		             $"{access.Probes.CouldWriteToDirectory}, folder carries Deny rules: " +
		             $"{access.Probes.HasDenyRules}");
		Logger.Debug($"  likely cause: {BuildVerdict(ex.HResult, access.Probes)}");
		Logger.Debug("--- End of file access diagnostics ---");
		Logger.Debug(SuggestedFixes);
	}

	/// <summary>
	/// Describes a file the converter could not read, if it could not read it. Silent by design: the
	/// parser reports neither a missing file nor a folder it cannot list, so this is the only place
	/// where those two failures become visible. A missing file is covered too, because opening it
	/// fails with a FileNotFoundException.
	/// </summary>
	internal static void LogUnreadableFileDiagnostics(string path) {
		try {
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
			LogFileAccessDiagnostics(path, ex);
		}
	}

	/// <summary>
	/// Turns a Win32 error code into a name, so that "access denied" can be told apart from "locked
	/// by another process", "drive not ready" and "blocked by policy" without guessing.
	/// </summary>
	internal static string DescribeHResult(int hResult) {
		var win32Code = hResult & 0xFFFF;
		var name = win32Code switch {
			1 => "ERROR_INVALID_FUNCTION",
			2 => "ERROR_FILE_NOT_FOUND",
			3 => "ERROR_PATH_NOT_FOUND",
			5 => "ERROR_ACCESS_DENIED",
			6 => "ERROR_INVALID_HANDLE",
			19 => "ERROR_NO_MEDIA",
			21 => "ERROR_NOT_READY",
			32 => "ERROR_SHARING_VIOLATION",
			33 => "ERROR_LOCK_VIOLATION",
			50 => "ERROR_NOT_SUPPORTED",
			53 => "ERROR_BAD_NETPATH",
			55 => "ERROR_DEV_NOT_EXIST",
			67 => "ERROR_BAD_NET_NAME",
			80 => "ERROR_FILE_EXISTS",
			112 => "ERROR_DISK_FULL",
			123 => "ERROR_INVALID_NAME",
			161 => "ERROR_BAD_PATHNAME",
			995 => "ERROR_IO_INCOMPLETE",
			1231 => "ERROR_INVALID_REPARSE_DATA",
			1232 => "ERROR_REPARSE_TAG_INVALID",
			1260 => "ERROR_ACCESS_DISABLED_BY_POLICY",
			1312 => "ERROR_NO_SUCH_LOGON_SESSION",
			1314 => "ERROR_NO_SUCH_USER",
			1326 => "ERROR_LOGON_FAILURE",
			1332 => "ERROR_NO_MAPPING_USER_ACCOUNT",
			1450 => "ERROR_INSUFFICIENT_RESOURCES",
			1920 => "ERROR_CANT_ACCESS_FILE",
			1921 => "ERROR_CANT_RESOLVE_FILENAME",
			2200 => "ERROR_NOT_A_REPARSE_POINT",
			_ => null
		};

		var code = win32Code.ToString(CultureInfo.InvariantCulture);
		var hex = hResult.ToString("X8", CultureInfo.InvariantCulture);
		return name is null
			? $"unrecognized error (Win32 code {code}, HRESULT 0x{hex})"
			: $"{name} (Win32 code {code}, HRESULT 0x{hex})";
	}

	/// <summary>
	/// Lists the code points of every non-ASCII character in a path. Cyrillic or accented folder
	/// names are not a problem by themselves, but the log line naming the folder is routinely mangled
	/// on its way out of the process, and this survives that mangling.
	/// </summary>
	internal static string DescribeNonAsciiChars(string path) {
		var described = new List<string>();
		for (var i = 0; i < path.Length; i++) {
			var character = path[i];
			if (character < 128) {
				continue;
			}
			var description = $"U+{(int)character:X4} '{character}' ({char.GetUnicodeCategory(character)})";
			if (!described.Contains(description, StringComparer.Ordinal)) {
				described.Add(description);
			}
		}
		return described.Count == 0 ? "none (pure ASCII path)" : string.Join(", ", described);
	}

	/// <summary>
	/// Describes the length, segmentation and quoting hazards of a path. Windows silently trims
	/// trailing spaces and dots from path components, so a folder that looks fine in a file manager
	/// can be unreachable for a file API.
	/// </summary>
	internal static string DescribePathShape(string path) {
		var root = Path.GetPathRoot(path);
		// The root is a drive or a share, not a name, so it is excluded both from the length report and
		// from the scan below - "C:" ends in a colon, which would otherwise look like a stripped one.
		var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
		var belowRoot = string.IsNullOrEmpty(root) || !path.StartsWith(root, comparison) ? path : path[root.Length..];
		var segments = belowRoot.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
		var longestSegment = segments.OrderByDescending(segment => segment.Length).FirstOrDefault() ?? string.Empty;

		var description = new List<string> {
			$"length {path.Length.ToString(CultureInfo.InvariantCulture)}",
			$"root \"{root ?? "(none)"}\" ({DescribeRootKind(root)})",
			$"{segments.Length.ToString(CultureInfo.InvariantCulture)} components below the root, " +
			$"longest {longestSegment.Length.ToString(CultureInfo.InvariantCulture)} characters"
		};

		if (path.Length > MaxPathLength) {
			description.Add($"EXCEEDS the {MaxPathLength.ToString(CultureInfo.InvariantCulture)}-character MAX_PATH limit");
		}

		var unquotable = segments.Count(segment =>
			segment.EndsWith(' ') || segment.EndsWith('.') || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0);
		if (unquotable > 0) {
			description.Add($"{unquotable.ToString(CultureInfo.InvariantCulture)} component(s) end in a space or a dot, or " +
			                 "contain characters Windows silently strips");
		}

		return string.Join("; ", description);
	}

	/// <summary>
	/// Replaces every personal fragment (profile path, account name, domain name, machine name) with a
	/// placeholder, so that paths stay useful in bug reports without identifying anybody.
	/// </summary>
	internal static string Redact(string? text) {
		if (string.IsNullOrEmpty(text)) {
			return string.Empty;
		}

		var redacted = text;
		foreach (var fragment in sensitiveFragments) {
			redacted = redacted.Replace(fragment, RedactedPlaceholder, StringComparison.OrdinalIgnoreCase);
		}
		return redacted;
	}

	/// <summary>
	/// Names a security principal only when doing so is safe: the account running the converter, or a
	/// built-in account, which is the same on every machine. Any other account is redacted.
	/// </summary>
	[SupportedOSPlatform("windows")]
	internal static string DescribeIdentity(SecurityIdentifier? sid) {
		if (sid is null) {
			return "(unresolvable principal)";
		}
		if (IsCurrentIdentity(sid)) {
			return "the account running the converter";
		}

		return sid.Value switch {
			"S-1-1-0" => "Everyone",
			"S-1-3-0" => "the owner of the object (CREATOR OWNER)",
			"S-1-5-11" => "Authenticated Users",
			"S-1-5-18" => "SYSTEM",
			"S-1-5-19" => "Local Service",
			"S-1-5-20" => "Network Service",
			"S-1-5-32-544" => "Administrators",
			"S-1-5-32-545" => "Users",
			"S-1-5-32-546" => "Guests",
			// Real accounts have a relative identifier of 1000 or more, so anything below that is a
			// built-in account, identical on every machine, and safe to describe.
			_ when TryGetRelativeIdentifier(sid.Value, out var rid) && rid < 1000 =>
				$"a built-in account (relative identifier {rid.ToString(CultureInfo.InvariantCulture)})",
			_ => "another account (redacted)"
		};
	}

	/// <summary>
	/// Picks the single most likely explanation from the probe results. Deliberately a hypothesis,
	/// not a certainty: the point is to point the reporter at the next thing to check.
	/// </summary>
	internal static string BuildVerdict(int hResult, AccessProbeResults probes) {
		// The helpers are consulted in order of how conclusive each cause is, so the most specific
		// explanation wins over the more speculative ones.
		return VerdictFromErrorCode(hResult & 0xFFFF) ??
		       VerdictFromFileAttributes(probes) ??
		       VerdictFromFileState(probes) ??
		       VerdictFromFolderAccess(probes) ??
		       NoProbeExplainsIt;
	}

	private const string NoProbeExplainsIt =
		"no probe explains the failure. Security software is still the most likely cause; adding the " +
		"converter folder to the antivirus' exclusion list and reinstalling to a plain local folder is " +
		"the next step worth trying.";

	/// <summary>Causes the operating system names outright, which outrank anything inferred.</summary>
	private static string? VerdictFromErrorCode(int win32Code) {
		if (win32Code is 32 or 33) {
			return "another program holds the file open. An on-access virus scanner is the usual suspect, " +
			       "and adding the converter folder to the antivirus' exclusion list normally fixes it.";
		}
		if (win32Code == 1260) {
			return "Windows refused the access by policy. Group policy, ransomware protection or a hardened " +
			       "security product is blocking the converter.";
		}
		if (win32Code == 21) {
			return "the volume is not available right now: a disconnected or removable drive. Reconnect it, " +
			       "then retry.";
		}
		return null;
	}

	/// <summary>Causes the file's own attributes give away.</summary>
	private static string? VerdictFromFileAttributes(AccessProbeResults probes) {
		if (probes.IsOffline) {
			return "the file is not present on the volume, either because a cloud file has not been " +
			       "downloaded or because the volume went away. Download or reconnect it, then retry.";
		}
		if (probes.IsEncrypted) {
			return "the file is EFS-encrypted and the account running the converter has no matching " +
			       "certificate, so reading it is denied. Reinstall the converter to a folder that is not " +
			       "EFS-encrypted.";
		}
		if (probes.IsReparsePoint) {
			return "the file is a reparse point, meaning a symlink or a cloud/antivirus placeholder. Its link " +
			       "target is logged above; reinstalling to a plain local folder avoids the problem.";
		}
		if (probes.IsOnNetworkDrive) {
			return "the file sits on a network drive or a mapped share, where the share's permissions can " +
			       "allow the executable to run while denying access to the individual files next to it. Move " +
			       "the converter to a local folder.";
		}
		return null;
	}

	/// <summary>Causes deduced from whether the file and its folder could be reached at all.</summary>
	private static string? VerdictFromFileState(AccessProbeResults probes) {
		if (!probes.FileExists && !probes.CouldListDirectory) {
			return probes.DirectoryMissing
				? "neither the file nor the folder holding it exist, so the converter's own files are missing " +
				  "from the installation. Reinstall or re-extract the converter."
				: "the file could not be found and its directory could not be listed either, so the converter " +
				  "almost certainly has no permission on its own folder. Check that folder's properties -> " +
				  "security tab.";
		}
		if (probes.FileExists && !probes.CouldRead) {
			return probes.HasDenyRules
				? "the file exists and is listed, opening it is denied, and the folder does carry Deny access " +
				  "rules, so this is a folder permission problem. The rules are logged above."
				: "the file exists and is listed, but opening it is denied without a matching Deny rule, " +
				  "which points at security software intercepting the converter.";
		}
		if (!probes.FileExists) {
			return "the file does not exist. Either the converter's files are incomplete, or the path is " +
			       "resolved differently than expected; compare the logged path with the working directory.";
		}
		return null;
	}

	/// <summary>Causes that only the converter's own need for write access reveals.</summary>
	private static string? VerdictFromFolderAccess(AccessProbeResults probes) {
		if (!probes.CouldWriteToDirectory) {
			return "the file can be read but the converter cannot write next to it. The converter needs write " +
			       "access to its own folder for its temporary files.";
		}
		return null;
	}

	/// <summary>Outcome of every probe taken for one inaccessible file.</summary>
	internal readonly record struct AccessProbeResults(bool FileExists, bool CouldRead, bool CouldListDirectory,
		bool CouldWriteToDirectory, bool HasDenyRules, bool IsEncrypted, bool IsReparsePoint, bool IsOffline,
		bool IsOnNetworkDrive, bool DirectoryAccessDenied = false, bool DirectoryMissing = false);

	/// <summary>
	/// One access rule, flattened into a platform-neutral shape so that nothing outside this file has
	/// to touch the Windows-only access control types.
	/// </summary>
	private readonly record struct AccessRuleDescription(string Principal, bool IsDeny, string Rights, bool IsInherited);

	/// <summary>Everything a single pass over one inaccessible file turned up.</summary>
	private readonly record struct AccessReport(AccessProbeResults Probes, List<AccessRuleDescription> FileRules,
		List<AccessRuleDescription> DirectoryRules);

	private const string SuggestedFixes =
		"Suggested fixes, in the order worth trying: move the converter to a plain local folder such as " +
		"C:\\Games\\ImperatorToCK3, add that folder to the antivirus' exclusion list, and re-download the " +
		"converter rather than copying it out of an archive.";

	private static void LogDriveDiagnostics(string fullPath) {
		var root = Path.GetPathRoot(fullPath);
		if (root is null) {
			Logger.Debug("  volume: the path has no root, so it is not a drive-letter or a UNC path");
			return;
		}

		TryProbeText("inspect volume", () => {
			var drive = new DriveInfo(root);
			var description = new List<string> {
				$"type {drive.DriveType}",
				$"format {drive.DriveFormat}",
				$"ready {drive.IsReady}"
			};
			if (drive.IsReady) {
				description.Add($"{drive.AvailableFreeSpace.ToString(CultureInfo.InvariantCulture)} bytes free of " +
				                $"{drive.TotalSize.ToString(CultureInfo.InvariantCulture)}");
			}
			return string.Join(", ", description);
		});

		// A network mapping can let the executable run while denying access to the files next to it.
		// A substituted ("subst") or virtual drive reports itself as a fixed volume, so the type above
		// cannot rule that out - the log can only hint at it.
		Logger.Debug("  note: the volume type above cannot distinguish a plain volume from a substituted or " +
		             "virtual one, so also check that the converter really is installed in a normal folder");
	}

	private static void LogFileMetadata(string fullPath, bool fileExists) {
		if (!fileExists) {
			return;
		}

		TryProbeText("inspect file", () => {
			var file = new FileInfo(fullPath);
			var attributes = file.Attributes;
			var description = new List<string> {
				$"length {file.Length.ToString(CultureInfo.InvariantCulture)}",
				$"last write {file.LastWriteTimeUtc:u}",
				$"attributes {attributes}"
			};
			if (attributes.HasFlag(FileAttributes.ReadOnly)) {
				description.Add("read-only");
			}
			if (attributes.HasFlag(FileAttributes.Encrypted)) {
				description.Add("EFS-encrypted");
			}
			if (attributes.HasFlag(FileAttributes.Offline)) {
				description.Add("offline, i.e. not present on the volume");
			}
			if (attributes.HasFlag(FileAttributes.ReparsePoint)) {
				description.Add($"reparse point, target: {DescribeLinkTarget(fullPath)}");
			}
			return string.Join("; ", description);
		});
	}

	private static AccessReport RunAccessProbes(string fullPath, bool fileExists) {
		var directory = Path.GetDirectoryName(fullPath);

		var couldRead = fileExists && TryProbe("open file for reading", () => {
			using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			return stream.Length >= 0;
		});

		// Initialized because the short-circuit below skips the probe, and therefore the out parameters,
		// when the path has no directory component at all.
		var directoryAccessDenied = false;
		var directoryMissing = false;
		var couldListDirectory = directory is not null && TryProbe("list containing directory", () => {
			Directory.GetFileSystemEntries(directory);
			return true;
		}, out directoryAccessDenied, out directoryMissing);

		var couldWriteToDirectory = directory is not null && TryProbe("write to containing directory", () => {
			var probeFilePath = Path.Combine(directory, "converter_access_probe.tmp");
			try {
				using var probeStream = File.Create(probeFilePath);
				return true;
			} finally {
				DeleteQuietly(probeFilePath);
			}
		});

		var fileRules = ReadAccessRulesIfSupported(fullPath, isDirectory: false);
		var directoryRules = directory is null
			? new List<AccessRuleDescription>()
			: ReadAccessRulesIfSupported(directory, isDirectory: true);

		var probes = new AccessProbeResults(
			FileExists: fileExists,
			CouldRead: couldRead,
			CouldListDirectory: couldListDirectory,
			CouldWriteToDirectory: couldWriteToDirectory,
			HasDenyRules: fileRules.Concat(directoryRules).Any(rule => rule.IsDeny),
			IsEncrypted: fileExists && HasAttribute(fullPath, FileAttributes.Encrypted),
			IsReparsePoint: fileExists && HasAttribute(fullPath, FileAttributes.ReparsePoint),
			IsOffline: fileExists && HasAttribute(fullPath, FileAttributes.Offline),
			IsOnNetworkDrive: IsNetworkDrive(fullPath),
			DirectoryAccessDenied: directoryAccessDenied,
			DirectoryMissing: directoryMissing);

		return new AccessReport(probes, fileRules, directoryRules);
	}

	/// <summary>
	/// Runs a probe and reports its outcome, returning whether it succeeded. A probe that was denied
	/// is not the same as one that found nothing missing, so why it failed is reported separately.
	/// </summary>
	private static bool TryProbe(string description, Func<bool> probe, out bool accessDenied,
		out bool missing) {
		accessDenied = false;
		missing = false;
		try {
			var succeeded = probe();
			Logger.Debug($"  probe '{description}': {(succeeded ? "succeeded" : "returned false")}");
			return succeeded;
		} catch (Exception probeException) {
			accessDenied = probeException is UnauthorizedAccessException;
			missing = probeException is DirectoryNotFoundException or FileNotFoundException;
			LogProbeFailure(description, probeException);
			return false;
		}
	}

	private static bool TryProbe(string description, Func<bool> probe) =>
		TryProbe(description, probe, out _, out _);

	/// <summary>Runs a probe that reports what it saw, rather than whether it worked.</summary>
	private static void TryProbeText(string description, Func<string> probe) {
		try {
			Logger.Debug($"  probe '{description}': {Redact(probe())}");
		} catch (Exception probeException) {
			LogProbeFailure(description, probeException);
		}
	}

	private static void LogProbeFailure(string description, Exception probeException) {
		Logger.Debug($"  probe '{description}': threw {probeException.GetType().Name} " +
		             $"({DescribeHResult(probeException.HResult)}) - {Redact(probeException.Message)}");
	}

	private static void DeleteQuietly(string path) {
		try {
			File.Delete(path);
		} catch (Exception cleanupException) {
			Logger.Debug($"  (could not delete the probe file: {cleanupException.GetType().Name})");
		}
	}

	private static string DescribeLinkTarget(string path) {
		try {
			var target = File.ResolveLinkTarget(path, returnFinalTarget: true);
			return target is null ? "unresolvable" : Redact(target.FullName);
		} catch (Exception linkException) {
			return $"unresolvable ({linkException.GetType().Name})";
		}
	}

	private static bool HasAttribute(string path, FileAttributes attribute) {
		try {
			return new FileInfo(path).Attributes.HasFlag(attribute);
		} catch (Exception) {
			return false;
		}
	}

	private static List<AccessRuleDescription> ReadAccessRulesIfSupported(string path, bool isDirectory) {
		if (!OperatingSystem.IsWindows()) {
			Logger.Debug($"  access rules of \"{Redact(path)}\": only readable on Windows, and this is not Windows");
			return new List<AccessRuleDescription>();
		}
		return ReadAccessRules(path, isDirectory);
	}

	[SupportedOSPlatform("windows")]
	private static List<AccessRuleDescription> ReadAccessRules(string path, bool isDirectory) {
		const AccessControlSections sections = AccessControlSections.Access | AccessControlSections.Owner;
		try {
			var rules = isDirectory
				? new DirectorySecurity(path, sections).GetAccessRules(true, true, typeof(SecurityIdentifier))
				: new FileSecurity(path, sections).GetAccessRules(true, true, typeof(SecurityIdentifier));

			return rules.Cast<FileSystemAccessRule>()
				.Select(rule => new AccessRuleDescription(
					Principal: DescribeIdentity(rule.IdentityReference as SecurityIdentifier),
					IsDeny: rule.AccessControlType == AccessControlType.Deny,
					Rights: rule.FileSystemRights.ToString(),
					IsInherited: rule.IsInherited))
				.ToList();
		} catch (Exception securityException) {
			Logger.Debug($"  (could not read the access rules of \"{Redact(path)}\": " +
			             $"{securityException.GetType().Name})");
			return new List<AccessRuleDescription>();
		}
	}

	[SupportedOSPlatform("windows")]
	private static bool IsCurrentIdentity(SecurityIdentifier sid) {
		try {
			using var identity = WindowsIdentity.GetCurrent();
			return identity.User is not null &&
			       string.Equals(identity.User.Value, sid.Value, StringComparison.OrdinalIgnoreCase);
		} catch (Exception) {
			return false;
		}
	}

	private static string DescribeAccessRules(IList<AccessRuleDescription> rules, string target) {
		if (rules.Count == 0) {
			return $"none could be read for \"{Redact(target)}\"";
		}

		var denies = rules.Where(rule => rule.IsDeny).ToList();
		var parts = new List<string> {
			$"{rules.Count.ToString(CultureInfo.InvariantCulture)} rule(s), " +
			$"{denies.Count.ToString(CultureInfo.InvariantCulture)} of them deny"
		};

		// A deny rule beats every allow rule, so those are the only ones worth spelling out.
		if (denies.Count > 0) {
			var described = denies.Take(MaxLoggedAccessRules)
				.Select(rule => $"[{rule.Principal}] {rule.Rights}{(rule.IsInherited ? " (inherited)" : string.Empty)}");
			parts.Add("deny rules: " + string.Join("; ", described));
			if (denies.Count > MaxLoggedAccessRules) {
				parts.Add($"(only the first {MaxLoggedAccessRules.ToString(CultureInfo.InvariantCulture)} of " +
				          $"{denies.Count.ToString(CultureInfo.InvariantCulture)} are listed)");
			}
		} else {
			parts.Add("no deny rules");
		}

		return string.Join(", ", parts);
	}

	private static bool IsNetworkDrive(string fullPath) {
		var root = Path.GetPathRoot(fullPath);
		if (root is null) {
			return false;
		}
		if (root.StartsWith(@"\\", StringComparison.Ordinal)) {
			return true;
		}
		if (!OperatingSystem.IsWindows()) {
			return false;
		}
		try {
			return new DriveInfo(root).DriveType == DriveType.Network;
		} catch (Exception) {
			return false;
		}
	}

	/// <summary>
	/// Names what a path is rooted at. Windows tells a drive apart from a network share; Unix has only
	/// the filesystem root, and a relative path has no root at all.
	/// </summary>
	private static string DescribeRootKind(string? root) {
		if (string.IsNullOrEmpty(root)) {
			return "no root, so the path is relative";
		}
		if (root.StartsWith(@"\\", StringComparison.Ordinal)) {
			return "a network share";
		}
		return root is "/" or "\\" ? "the filesystem root" : "a drive letter";
	}

	private static bool TryGetRelativeIdentifier(string sid, out int relativeIdentifier) {
		var parts = sid.Split('-');
		var last = parts.Length > 0 ? parts[parts.Length - 1] : string.Empty;
		return int.TryParse(last, NumberStyles.None, CultureInfo.InvariantCulture, out relativeIdentifier);
	}

	/// <summary>
	/// Picks the first stack frame naming a source location, which is often the only place the
	/// offending path survives once the exception message has been mangled.
	/// </summary>
	private static string FirstStackFrameWithSource(string stackTrace) {
		foreach (var line in stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries)) {
			if (line.Contains(" in ", StringComparison.Ordinal)) {
				return line.Trim();
			}
		}
		return stackTrace.Split('\n')[0].Trim();
	}

	private static string? SafeDirectoryPath() => SafeEnv(Directory.GetCurrentDirectory);

	private static string? SafeEnv(Func<string?> getter) {
		try {
			return getter();
		} catch (Exception) {
			return null;
		}
	}

	private static bool SameDirectory(string? first, string? second) {
		if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second)) {
			return false;
		}
		var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
		return string.Equals(PathHelper.RemoveTrailingSeparators(first),
			PathHelper.RemoveTrailingSeparators(second), comparison);
	}

	private static string[] BuildSensitiveFragments() {
		var fragments = new List<string?> {
			SafeEnv(() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
			SafeEnv(() => Environment.UserName),
			SafeEnv(() => Environment.UserDomainName),
			SafeEnv(() => Environment.MachineName)
		};

		return fragments
			.Where(fragment => !string.IsNullOrWhiteSpace(fragment))
			.Select(fragment => fragment!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			.Where(fragment => fragment.Length > 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderByDescending(fragment => fragment.Length)
			.ToArray();
	}
}
