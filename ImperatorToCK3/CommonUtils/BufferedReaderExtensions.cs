using commonItems;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

internal static class BufferedReaderExtensions {
	internal static Dictionary<string, string> GetAssignmentsAsDict(this BufferedReader reader) {
		return reader.GetAssignments()
			.GroupBy(a => a.Key)
			.ToDictionary(g => g.Key, g => g.Last().Value);
	}

	internal static List<string> GetAndInternStrings(this BufferedReader reader) {
		var strings = new List<string>();
		foreach (var str in reader.GetStrings()) {
			string.Intern(str);
		}
		return strings;
	}
}