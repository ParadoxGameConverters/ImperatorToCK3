using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using commonItems.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.CK3.Cultures; 

public sealed class Culture : IIdentifiable<string>, IPDXSerializable {
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
	
	public string Serialize(string indent, bool withBraces) {
		var contentIndent = indent;
		if (withBraces) {
			contentIndent += '\t';
		}

		var sb = new StringBuilder();
		if (withBraces) {
			sb.AppendLine("{");
		}

		sb.Append(contentIndent).AppendLine($"color={Color.OutputRgb()}");
		sb.Append(contentIndent).AppendLine($"heritage={Heritage.Id}");
		sb.Append(contentIndent).AppendLine($"traditions={PDXSerializer.Serialize(TraditionIds)}");
		foreach (var nameList in NameLists) {
			sb.Append(contentIndent).AppendLine($"name_list={nameList.Id}");
		}
		sb.AppendLine(PDXSerializer.Serialize(Attributes, indent: contentIndent, withBraces: false));

		if (withBraces) {
			sb.Append(indent).Append('}');
		}

		return sb.ToString();
	}

	public IEnumerable<string> MaleNames => NameLists.SelectMany(l => l.MaleNames);
	public IEnumerable<string> FemaleNames => NameLists.SelectMany(l => l.FemaleNames);
}