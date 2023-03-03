using commonItems;
using commonItems.Collections;
using ImperatorToCK3.Exceptions;
using System;

namespace ImperatorToCK3.CK3.Cultures; 

public class Pillar : IIdentifiable<string> {
	public string Id { get; }
	public string Type { get; private set; }

	public Pillar(string id, BufferedReader pillarReader) {
		Id = id;

		var parser = new Parser();
		parser.RegisterKeyword("type", reader => {
			Type = reader.GetString();
		});
		parser.IgnoreUnregisteredItems();
		parser.ParseStream(pillarReader);

		if (string.IsNullOrEmpty(Type)) {
			throw new ConverterException($"Cultural pillar {id} has no type defined!");
		}
	}
}