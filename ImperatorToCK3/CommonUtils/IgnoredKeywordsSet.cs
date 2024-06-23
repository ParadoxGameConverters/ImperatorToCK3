using JoshuaKearney.Collections;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

public class IgnoredKeywordsSet : ConcurrentSet<string> {
	public override string ToString() {
		return string.Join(", ", this.Order());
	}
}