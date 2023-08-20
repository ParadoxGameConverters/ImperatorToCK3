using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using ImperatorToCK3.Exceptions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ImperatorToCK3.CK3.Cultures; 

public sealed class Culture : IIdentifiable<string> {
	public string Id { get; }
	public Color Color { get; private set; } = new(0, 0, 0);
	public Pillar Heritage { get; private set; }
	private SortedSet<string> traditionIds = new();
	public IReadOnlyCollection<string> TraditionIds => traditionIds;
	public OrderedSet<NameList> NameLists { get; }
	
	public Culture(string id, BufferedReader cultureReader, PillarCollection pillars, IdObjectCollection<string, NameList> nameLists, ColorFactory colorFactory) {
		Id = id;

		NameLists = new OrderedSet<NameList>();
		var parser = new Parser();
		parser.RegisterKeyword("color", reader => Color = colorFactory.GetColor(reader));
		parser.RegisterKeyword("heritage", reader => {
			var heritageId = reader.GetString();
			Heritage = pillars.Heritages.First(p => p.Id == heritageId);
		});
		parser.RegisterKeyword("traditions", reader => {
			traditionIds = new SortedSet<string>(reader.GetStrings());
		});
		parser.RegisterKeyword("name_list", reader => {
			var nameListId = reader.GetString();
			if (nameLists.TryGetValue(nameListId, out var nameList)) {
				NameLists.Add(nameList);
			} else {
				Logger.Warn($"Culture {id} has unrecognized name list: {nameListId}");
			}
		});
		parser.IgnoreUnregisteredItems();
		parser.ParseStream(cultureReader);
		
		if (Heritage is null) {
			throw new ConverterException($"Culture {id} has no heritage defined!");
		}
		if (NameLists.Count == 0) {
			throw new ConverterException($"Culture {id} has no name list defined!");
		}
	}

	public IEnumerable<string> MaleNames => NameLists.SelectMany(l => l.MaleNames);
	public IEnumerable<string> FemaleNames => NameLists.SelectMany(l => l.FemaleNames);
}