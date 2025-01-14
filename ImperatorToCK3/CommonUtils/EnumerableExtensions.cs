using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CommonUtils;

public static class EnumerableExtensions 
{
	public static KeyValuePair<TKey, TValue>? LastOrNull<TKey, TValue>(
		this IEnumerable<KeyValuePair<TKey, TValue>> source) {
		var keyValuePairs = source as KeyValuePair<TKey, TValue>[] ?? source.ToArray();
		return keyValuePairs.Length != 0
			? keyValuePairs.Last() 
			: null;
	}
}