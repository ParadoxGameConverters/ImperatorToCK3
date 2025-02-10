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
}