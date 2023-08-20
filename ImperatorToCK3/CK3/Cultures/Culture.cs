using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Serialization;
using ImperatorToCK3.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Cultures; 

public sealed class Culture : IIdentifiable<string>, IPDXSerializable { // TODO: make it serializable
	public string Id { get; }
	public Color Color { get; }
	public Pillar Heritage { get; }
	private readonly OrderedSet<string> traditionIds;
	public IReadOnlyCollection<string> TraditionIds => traditionIds;
	private readonly OrderedSet<NameList> nameLists;
	public IReadOnlyCollection<NameList> NameLists => nameLists;
	private readonly List<KeyValuePair<string, StringOfItem>> attributes;
	public IReadOnlyCollection<KeyValuePair<string, StringOfItem>> Attributes => attributes;
	
	public Culture(string id, CultureData cultureData) {
		Id = id;

		Color = cultureData.Color!;
		Heritage = cultureData.Heritage!;
		traditionIds = cultureData.TraditionIds;
		nameLists = cultureData.NameLists;
		attributes = cultureData.Attributes;
	}
	
	// TODO: serialize method

	public IEnumerable<string> MaleNames => NameLists.SelectMany(l => l.MaleNames);
	public IEnumerable<string> FemaleNames => NameLists.SelectMany(l => l.FemaleNames);
}