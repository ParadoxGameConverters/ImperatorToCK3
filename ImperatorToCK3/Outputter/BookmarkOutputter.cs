using commonItems;
using ImperatorToCK3.CK3.Characters;
using ImperatorToCK3.CK3.Titles;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ImageMagick;
using System;

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

			var playerTitles = new List<Title>(titles.Values.Where(title => title.PlayerCountry));
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

		private static void DrawBookmarkMap(Configuration config, List<Title> playerTitles, Dictionary<string, Title> titles) {
			Logger.Info("Drawing bookmark map.");

			var bookmarkMapPath = Path.Combine(config.Ck3Path, "game/gfx/map/terrain/flatmap.dds");
			using var bookmarkMapImage = new MagickImage(bookmarkMapPath);
			bookmarkMapImage.FilterType = FilterType.Point;
			bookmarkMapImage.Resize(2160, 1080);
			bookmarkMapImage.Crop(1920, 1080);
			bookmarkMapImage.RePage();

			var provincesMapPath = Path.Combine(config.Ck3Path, "game/map_data/provinces.png");
			using var provincesImage = new MagickImage(provincesMapPath);
			provincesImage.FilterType = FilterType.Point;
			provincesImage.Resize(2160, 1080);
			provincesImage.Crop(1920, 1080);
			provincesImage.RePage();

			var provDefinitions = LoadProvinceDefinitions(config);

			foreach (var playerTitle in playerTitles) {
				var colorOnMap = playerTitle.Color1 ?? new Color(new[] { 0, 0, 0 });
				var magickColorOnMap = MagickColor.FromRgb((byte)colorOnMap.R, (byte)colorOnMap.G, (byte)colorOnMap.B);

				var holderId = playerTitle.GetHolderId(config.Ck3BookmarkDate);
				var heldCounties = new List<Title>(
					titles.Values.Where(t => t.GetHolderId(config.Ck3BookmarkDate) == holderId && t.Rank == TitleRank.county)
				);
				var heldProvinces = new HashSet<ulong>();
				foreach (var county in heldCounties) {
					heldProvinces.UnionWith(county.CountyProvinces);
				}

				using var copyImage = new MagickImage(provincesImage);
				foreach (var province in heldProvinces) {
					var provinceColor = provDefinitions[province].Color;
					// make pixels of the province black
					copyImage.Opaque(provinceColor, MagickColor.FromRgb(0, 0, 0));
				}
				// replace black with title color
				copyImage.Opaque(MagickColor.FromRgb(0, 0, 0), magickColorOnMap);
				// make pixels all colors but the country color transparent
				copyImage.InverseTransparent(magickColorOnMap);
				// make country on map semi-transparent
				copyImage.Evaluate(Channels.Alpha, EvaluateOperator.Divide, 3);
				// add the image on top of blank map image
				bookmarkMapImage.Composite(copyImage, Gravity.Center, CompositeOperator.Over);
			}
			var outputPath = Path.Combine("output", config.OutputModName, "gfx/interface/bookmarks/bm_converted.dds");
			bookmarkMapImage.Write(outputPath);
		}

		private class ProvinceDefinition {
			public ulong ID { get; }
			public MagickColor Color { get; }
			public ProvinceDefinition(ulong id, byte r, byte g, byte b) {
				ID = id;
				Color = MagickColor.FromRgb(r, g, b);
			}
		}

		private static Dictionary<ulong, ProvinceDefinition> LoadProvinceDefinitions(Configuration config) {
			var definitionsFilePath = Path.Combine(config.Ck3Path, "game/map_data/definition.csv");
			using var fileStream = File.OpenRead(definitionsFilePath);
			using var definitionFileReader = new StreamReader(fileStream);

			var definitions = new Dictionary<ulong, ProvinceDefinition>();

			definitionFileReader.ReadLine(); // discard first line

			while (!definitionFileReader.EndOfStream) {
				var line = definitionFileReader.ReadLine();
				if (line is null || line.Length < 4 || line[0] == '#' || line[1] == '#') {
					continue;
				}

				try {
					var columns = line.Split(';');
					var id = ulong.Parse(columns[0]);
					var r = byte.Parse(columns[1]);
					var g = byte.Parse(columns[2]);
					var b = byte.Parse(columns[3]);
					var definition = new ProvinceDefinition(id, r, g, b);
					definitions.Add(definition.ID, definition);
				} catch (Exception e) {
					throw new FormatException($"Line: |{line}| is unparseable! Breaking. ({e})");
				}
			}
			return definitions;
		}
	}
}
