using commonItems;
using commonItems.Collections;
using commonItems.Colors;
using ImperatorToCK3.Exceptions;
using System.Linq;

namespace ImperatorToCK3.CK3.Cultures; 

public sealed class Culture : IIdentifiable<string> {
	public string Id { get; }
	public Color Color { get; private set; } = new(0, 0, 0);
	public Pillar Heritage;
	
	public Culture(string id, BufferedReader cultureReader, PillarCollection pillars, ColorFactory colorFactory) {
		Id = id;
		
		var parser = new Parser();
		parser.RegisterKeyword("color", reader => Color = colorFactory.GetColor(reader));
		parser.RegisterKeyword("heritage", reader => {
			var heritageId = reader.GetString();
			Heritage = pillars.Heritages.First(p => p.Id == heritageId);
		});
		parser.IgnoreUnregisteredItems();
		parser.ParseStream(cultureReader);
		
		if (Heritage is null) {
			throw new ConverterException($"Culture {id} has no heritage defined!");
		}
	}
}