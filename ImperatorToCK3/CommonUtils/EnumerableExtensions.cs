using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

public static class EnumerableExtensions {
	public static T? LastOrNull<T>(this IEnumerable<T> source, Func<T,bool> predicate) where T : struct {
		var enumerable = source as T[] ?? [.. source];

		if (enumerable.Length == 0) {
			return null;
		}

		foreach (var element in Enumerable.Reverse(enumerable)) {
			if (predicate(element)) {
				return element;
			}
		}

		return null;
	}
	public static KeyValuePair<TKey, TValue>? LastOrNull<TKey, TValue>(
		this IEnumerable<KeyValuePair<TKey, TValue>> source) {
		var keyValuePairs = source as KeyValuePair<TKey, TValue>[] ?? source.ToArray();
		return keyValuePairs.Length != 0
			? keyValuePairs[^1] : null;
	}
}