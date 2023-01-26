using commonItems.Collections;

namespace ImperatorToCK3.CommonUtils;

public class IgnoredKeywordsSet : OrderedSet<string> {
	public override string ToString() {
		return string.Join(", ", this);
	}
}