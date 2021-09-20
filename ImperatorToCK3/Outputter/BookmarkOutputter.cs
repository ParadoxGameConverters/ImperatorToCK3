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

			DrawBookmarkMap(config, playerTitles, titles);
		}

		private static void DrawBookmarkMap(Configuration config, SortedSet<Title> playerTitles, Dictionary<string, Title> titles) {
			Logger.Info("Drawing bookmark map.");

			var bookmarkMapPath = Path.Combine(config.Ck3Path, "game/gfx/map/terrain/flatmap.dds");
			var provincesMapPath = Path.Combine(config.Ck3Path, "game/map_data/provinces.png");

			using var bookmarkMapImage = new MagickImage(bookmarkMapPath);
			using var provincesImage = new MagickImage(provincesMapPath);

			var byzantionColor = MagickColor.FromRgb(42, 210, 48);
			var byzantionNeighborColor = MagickColor.FromRgb(5, 81, 210);
			var countryColors = new List<MagickColor> {
				byzantionColor,
				byzantionNeighborColor
			};
			
			foreach(var playerTitle in playerTitles) {
				var holderId = playerTitle.GetHolderId(config.Ck3BookmarkDate);
				var heldCounties = titles.Values.Where(t => t.GetHolderId(config.Ck3BookmarkDate) == holderId && t.Rank == TitleRank.county);
				var heldProvinces = new HashSet<ulong>();
				foreach(var county in heldCounties) {
					heldProvinces.UnionWith(county.CountyProvinces);
				}
			}

			foreach (var countryColor in countryColors) {
				using var copyImage = new MagickImage(provincesImage);
				copyImage.InverseTransparent(countryColor);
				copyImage.Opaque(countryColor, MagickColor.FromRgb(82, 71, 101));
				copyImage.Evaluate(Channels.Alpha, EvaluateOperator.Divide, 4);
				bookmarkMapImage.Composite(copyImage, Gravity.Center, CompositeOperator.Over);
			}
			bookmarkMapImage.Write("AWESOME.png");
			/*
			using (var images = new MagickImageCollection()) {
				// Add the first image
				var first = new MagickImage("Snakeware.png");
				images.Add(first);

				// Add the second image
				var second = new MagickImage("Snakeware.png");
				images.Add(second);

				// Create a mosaic from both images
				using (var result = images.) {
					// Save the result
					result.Write("Mosaic.png");
				}
			}
			*/
		}
	}
}
