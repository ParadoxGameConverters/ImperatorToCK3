using JoshuaKearney.Collections;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

internal sealed class ConcurrentIgnoredKeywordsSet : ConcurrentSet<string> {
	public override string ToString() {
		return string.Join(", ", this.Order());
	}
}