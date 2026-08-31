using commonItems.Collections;
using System.Collections.Generic;
using System.Text;

namespace ImperatorToCK3.CK3.Modifiers; 

public class Modifier : IIdentifiable<string> {
	public string Id { get; }
	private readonly Dictionary<string, double> effects = new();

	public Modifier(string id, IDictionary<string, double> effects) {
		Id = id;
		this.effects = new Dictionary<string, double>(effects);
	}
	
	public override string ToString() {
		var output = new StringBuilder();
		output.AppendLine($"{Id} = {{");
		foreach (var effect in effects) {
			output.AppendLine($"\t{effect.Key} = {effect.Value}");
		}
		output.AppendLine("}");
		return output.ToString();
	}
}