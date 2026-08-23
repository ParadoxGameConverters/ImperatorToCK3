using System.IO;

namespace ImperatorToCK3.CommonUtils;

internal static class PathHelper {
	internal static string RemoveTrailingSeparators(string path) {
		if (string.IsNullOrEmpty(path))
			return path;

		string root = Path.GetPathRoot(path) ?? string.Empty;
		string trimmed = path.TrimEnd(
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		);

		return trimmed.Length == 0 ? root : trimmed;
	}
}