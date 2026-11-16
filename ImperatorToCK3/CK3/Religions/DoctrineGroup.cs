using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ImperatorToCK3.CK3.Religions;

internal sealed class DoctrineGroup : IIdentifiable<string> {
	public string Id { get; }
	public string? CategoryId { get; private set; }
	public int NumberOfPicks { get; private set; } = 1;

	/// <summary>
	/// In CK3 1.20+, a doctrine group can be flagged as only applying to the main rite of a faith.
	/// </summary>
	public bool IsMainRiteOnly { get; private set; }

	private readonly OrderedSet<string> doctrineIds = [];
	public IReadOnlyCollection<string> DoctrineIds => doctrineIds.ToImmutableArray();

	public DoctrineGroup(string id, BufferedReader groupReader) {
		Id = id;

		var parser = new Parser(implicitVariableHandling: true);
		parser.RegisterKeyword("category", reader => CategoryId = reader.GetString());
		// Legacy keyword, no longer used by CK3, but still supported for older mods and tests.
		parser.RegisterKeyword("number_of_picks", reader => NumberOfPicks = reader.GetInt());
		// Legacy keyword, no longer used by CK3. In 1.20+ doctrines declare their group instead.
		parser.RegisterKeyword("doctrine_types", reader => doctrineIds.UnionWith(reader.GetStrings()));
		parser.RegisterKeyword("main_rite", reader => IsMainRiteOnly = reader.GetBool());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(groupReader);
	}

	public void AddDoctrineId(string doctrineId) {
		doctrineIds.Add(doctrineId);
	}
}