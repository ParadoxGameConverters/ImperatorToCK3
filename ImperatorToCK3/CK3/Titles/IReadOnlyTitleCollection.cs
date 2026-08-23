using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Titles;

internal interface IReadOnlyTitleCollection : IReadOnlyCollection<Title> {
	public bool ContainsKey(string key);
}