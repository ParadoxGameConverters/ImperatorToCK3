using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Titles;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ImageMagick;

namespace ImperatorToCK3.Outputter {
	public static class BookmarkOutputter {
		public static void OutputBookmark(Dictionary<string, Character> characters, Dictionary<string, Title> titles, Configuration config) {
			var path = "output/" + config.OutputModName + "/common/bookmarks/00_bookmarks.txt";
			using var stream = File.OpenWrite(path);
			using var output = new StreamWriter(stream, Encoding.UTF8);

			output.WriteLine("bm_converted = {");

			output.WriteLine("\tdefault = yes");
			output.WriteLine($"\tstart_date = {config.Ck3BookmarkDate}");
			output.WriteLine("\tis_playable = yes");
			output.WriteLine("\trecommended = yes");

			var playerTitles = new SortedSet<Title>(titles.Values.Where(title => title.PlayerCountry));
			var xPos = 430;
			var yPos = 190;
			foreach (var title in playerTitles) {
				var holder = characters[title.GetHolderId(config.Ck3BookmarkDate)];

				output.WriteLine("\tcharacter = {");

				output.WriteLine($"\t\tname = {holder.Name}");
				output.WriteLine($"\t\tdynasty = {holder.DynastyID}");
				output.WriteLine("\t\tdynasty_splendor_level = 1");
				output.WriteLine($"\t\ttype = {holder.AgeSex}");
				output.WriteLine($"\t\thistory_id = {holder.ID}");
				output.WriteLine($"\t\tbirth = {holder.BirthDate}");
				output.WriteLine($"\t\ttitle = {title.Name}");
				var gov = title.GetGovernment(config.Ck3BookmarkDate);
				if (gov is not null) {
					output.WriteLine($"\t\tgovernment = {gov}");
				}
				output.WriteLine($"\t\tculture = {holder.Culture}");
				output.WriteLine($"\t\treligion = {holder.Religion}");
				output.WriteLine("\t\tdifficulty = \"BOOKMARK_CHARACTER_DIFFICULTY_EASY\"");
				output.WriteLine($"\t\tposition = {{ {xPos} {yPos} }}");
				output.WriteLine("\t\tanimation = personality_rational");

				output.WriteLine("\t}");

				xPos += 200;
				if (xPos > 1700) {
					xPos = 430;
					yPos += 200;
				}

				string templateText;
				string templatePath = holder.AgeSex switch {
					"female" => "blankMod/templates/common/bookmark_portraits/female.txt",
					"girl" => "blankMod/templates/common/bookmark_portraits/girl.txt",
					"boy" => "blankMod/templates/common/bookmark_portraits/boy.txt",
					_ => "blankMod/templates/common/bookmark_portraits/male.txt",
				};
				templateText = File.ReadAllText(templatePath);
				templateText = templateText.Replace("REPLACE_ME_NAME", holder.Name);
				templateText = templateText.Replace("REPLACE_ME_AGE", holder.Age.ToString());
				var outPortraitPath = "output/" + config.OutputModName + "/common/bookmark_portraits/" + $"{holder.Name}.txt";
				File.WriteAllText(outPortraitPath, templateText);
			}

			output.WriteLine("}");

			DrawBookmarkMap(config);
		}

		private static void DrawBookmarkMap(Configuration config) {
			Logger.Info("Drawing bookmark map.");

			var blankMapPath = Path.Combine(config.Ck3Path, "game/gfx/map/terrain/flatmap.dds");
			var provincesMapPath = Path.Combine(config.Ck3Path, "game/map_data/provinces.png");

			var image = new MagickImage(provincesMapPath);

			var byzantionColor = new MagickColor(42, 210, 48);
			var byzantionNeighborColor = new MagickColor(5, 81, 210);
			var countryColors = new List<MagickColor> {
				byzantionColor,
				byzantionNeighborColor
			};

			foreach (var color in image.Histogram().Keys) {
				Logger.Debug("Checking color " + color.ToShortString());
				var toTransparent = true;
				foreach(var countryColor in countryColors) {
					if (color.Equals(countryColor)) {
						// province belongs to player country, should not be made transparent
						Logger.Debug("HIT!");
						toTransparent = false;
					}
				}
				if (toTransparent) {
					image.Transparent(color);
				}
			}
			image.Write("AWESOME.png");
		}
	}
}
