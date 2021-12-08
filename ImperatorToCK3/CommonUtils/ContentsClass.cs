using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils {
	public class ContentsClass {
		public Dictionary<string, List<object>> SimpleFieldContents { get; } = new();
		public Dictionary<string, List<List<object>>> ContainerFieldContents { get; } = new();
	}
}