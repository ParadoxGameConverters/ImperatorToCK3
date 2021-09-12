using commonItems;
using ImperatorToCK3.CK3.Titles;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImperatorToCK3.Outputter {
	public static class BookmarkOutputter {
		public static void OutputBookmark(string outputModName, Dictionary<string, Title> titles, Date ck3BookmarkDate) {
			var path = "output/" + outputModName + "/common/bookmarks/00_bookmarks.txt";
			using var stream = File.OpenWrite(path);
			using var output = new StreamWriter(stream, Encoding.UTF8);

			output.WriteLine("bm_converted = {");

			output.WriteLine("\tdefault = yes");
			output.WriteLine($"\tstart_date = {ck3BookmarkDate}");
			output.WriteLine("\tis_playable = yes");
			output.WriteLine("\trecommended = yes");

			var playerTitles = new SortedSet<Title>(titles.Values.Where(title => title.PlayerCountry));
			var xPos = 430;
			var yPos = 190;
			foreach (var title in playerTitles) {
				var holder = title.Holder;
				if (holder is not null) {
					output.WriteLine("\tcharacter = {");

					output.WriteLine($"\t\tname = {holder.Name}");
					output.WriteLine($"\t\tdynasty = {holder.DynastyID}");
					output.WriteLine("\t\tdynasty_splendor_level = 1");
					var sex = holder.Female ? "female" : "male";
					output.WriteLine($"\t\ttype = {sex}");
					output.WriteLine($"\t\thistory_id = {holder.ID}");
					output.WriteLine($"\t\tbirth = {holder.BirthDate}");
					output.WriteLine($"\t\ttitle = {title.Name}");
					output.WriteLine($"\t\tgovernment = {title.Government}");
					output.WriteLine($"\t\tculture = {holder.Culture}");
					output.WriteLine($"\t\treligion = {holder.Religion}");
					output.WriteLine("\t\tdifficulty = \"BOOKMARK_CHARACTER_DIFFICULTY_EASY\"");
					output.WriteLine($"\t\tposition = {{ {xPos} {yPos} }}");
					output.WriteLine("\t\tanimation = personality_rational");

					output.WriteLine("\t}");

					xPos += 200;
					if (xPos > 1700) {
						xPos = 430;
					}
					yPos += 200;
					if (yPos > 800) {
						yPos = 190;
					}
				}
			}

			output.WriteLine("}");
		}
	}
}
