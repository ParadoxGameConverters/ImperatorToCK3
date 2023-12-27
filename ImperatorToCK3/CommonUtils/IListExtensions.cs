using System;
using System.Collections.Generic;

namespace ImperatorToCK3.CommonUtils;

public static class IListExtensions {
	public static int RemoveAll<T>(this IList<T> list, Predicate<T> match) {
		int count = 0;

		for (int i = list.Count - 1; i >= 0; i--) {
			if (match(list[i])) {
				++count;
				list.RemoveAt(i);
			}
		}

		return count;
	}   
}