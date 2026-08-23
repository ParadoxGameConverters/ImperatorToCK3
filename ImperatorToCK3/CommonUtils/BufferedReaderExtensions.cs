using commonItems;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils;

internal static class BufferedReaderExtensions {
	internal static Dictionary<string, string> GetAssignmentsAsDict(this BufferedReader reader) {
		var assignmentsDict = new Dictionary<string, string>();
		foreach (var assignment in reader.GetAssignments()) {
			assignmentsDict[assignment.Key] = assignment.Value;
		}

		return assignmentsDict;
	}

	internal static List<string> GetAndInternStrings(this BufferedReader reader) {
		var strings = new List<string>();
		foreach (var str in reader.GetStrings()) {
			strings.Add(string.Intern(str));
		}
		return strings;
	}
}