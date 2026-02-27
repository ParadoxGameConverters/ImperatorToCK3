using commonItems.Collections;
using System.Text;

namespace ImperatorToCK3.CK3.Modifiers; 

public class ModifierCollection : IdObjectCollection<string, Modifier> {
	public override string ToString() {
		var output = new StringBuilder();
		foreach (var modifier in this) {
			output.AppendLine(modifier.ToString());
		}
		return output.ToString();
	}
}