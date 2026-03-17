using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

public static class EnumerableExtensions {
	public static T? LastOrNull<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : struct {
		if (source is null) {
			throw new ArgumentNullException(nameof(source));
		}
		if (predicate is null) {
			throw new ArgumentNullException(nameof(predicate));
		}

		T? last = null;
		foreach (var element in source) {
			if (predicate(element)) {
				last = element;
			}
		}
		return last;
	}

	public static KeyValuePair<TKey, TValue>? LastOrNull<TKey, TValue>(
		this IEnumerable<KeyValuePair<TKey, TValue>> source) {
		if (source is null) {
			throw new ArgumentNullException(nameof(source));
		}

		KeyValuePair<TKey, TValue>? last = null;
		foreach (var kvp in source) {
			last = kvp;
		}
		return last;
	}
}