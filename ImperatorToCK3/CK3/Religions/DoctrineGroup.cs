using commonItems;
using commonItems.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ImperatorToCK3.CK3.Religions;

/// <summary>
/// A CK3 doctrine group. In 1.20+ a group holds many doctrines and a faith effectively uses only one of them
/// (rite doctrines take precedence over faith ones, faith over religion).
/// </summary>
internal sealed class DoctrineGroup : IIdentifiable<string> {
	public string Id { get; }
	public string? CategoryId { get; private set; }

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
		parser.RegisterKeyword("main_rite", reader => IsMainRiteOnly = reader.GetBool());
		parser.IgnoreAndLogUnregisteredItems();
		parser.ParseStream(groupReader);
	}

	public void AddDoctrineId(string doctrineId) {
		doctrineIds.Add(doctrineId);
	}
}