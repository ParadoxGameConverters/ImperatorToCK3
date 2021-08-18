using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils {
	public class ContentsClass {
		public Dictionary<string, List<string>> SimpleFieldContents { get; } = new();
		public Dictionary<string, List<List<string>>> ContainerFieldContents { get; } = new();
	}
}