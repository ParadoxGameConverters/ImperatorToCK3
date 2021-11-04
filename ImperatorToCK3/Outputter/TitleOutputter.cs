using commonItems.Serialization;
using ImperatorToCK3.CK3.Titles;
using System.IO;

namespace ImperatorToCK3.Outputter {
	public static class TitleOutputter {
		public static void OutputTitle(StreamWriter writer, Title title, string indent) {
			writer.WriteLine($"{indent}{title.Name} = {PDXSerializer.Serialize(title)}");
		}
	}
}
