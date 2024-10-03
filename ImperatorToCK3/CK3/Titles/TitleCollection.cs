using commonItems.Collections;
using System.Collections.Generic;

namespace ImperatorToCK3.CK3.Titles;

public interface IReadOnlyTitleCollection : IReadOnlyCollection<Title> {
	public bool ContainsKey(string key);
}

public class TitleCollection : IdObjectCollection<string, Title>, IReadOnlyTitleCollection;